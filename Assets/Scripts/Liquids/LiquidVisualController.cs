using UnityEngine;

/// <summary>
/// RF-03: Visual controller for simple, low-cost laboratory liquids.
/// Creates/updates a cylindrical liquid body plus a curved meniscus surface.
/// It is intentionally visual-only: no fluid physics and no real chemistry calculation.
/// Future systems such as RF-04 can drive it through SetPH, SetConcentration,
/// SetLiquidLevel and SetLiquidColor.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class LiquidVisualController : MonoBehaviour
{
    public enum AutoColorMode
    {
        Manual,
        ByPH,
        ByConcentration
    }

    [Header("Geometry")]
    [Tooltip("Bottom local Y position of the liquid inside the container.")]
    [SerializeField] private float bottomOffset = 0.02f;

    [Tooltip("Maximum visual height in local units when liquidLevel is 1.")]
    [SerializeField] private float maxLiquidHeight = 0.18f;

    [Tooltip("Radius in local units. Adjust per beaker/flask/tube.")]
    [SerializeField] private float liquidRadius = 0.055f;

    [Tooltip("0 = empty, 1 = full. Can be changed from Inspector or SetLiquidLevel().")]
    [Range(0f, 1f)]
    [SerializeField] private float liquidLevel = 0.65f;

    [Header("Visual")]
    [Tooltip("Base liquid color used in Manual mode, or blended with auto pH/concentration color.")]
    [SerializeField] private Color baseColor = new Color(0.2f, 0.65f, 1f, 0.45f);

    [Tooltip("Alpha/transparency of the liquid. Lower = more transparent.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float transparency = 0.45f;

    [Tooltip("Visual meniscus curvature/intensity. 0 = flat, 1 = stronger curved edge.")]
    [Range(0f, 1f)]
    [SerializeField] private float meniscusIntensity = 0.55f;

    [Tooltip("Optional shared material. If empty, a transparent URP/Lit material is created automatically.")]
    [SerializeField] private Material liquidMaterial;

    [Header("Color Mixing")]
    [Tooltip("When this container is empty, incoming poured liquid takes its color.")]
    [SerializeField] private bool useIncomingColorWhenEmpty = true;

    [Tooltip("When this container already has liquid, blend the current color with the incoming color.")]
    [SerializeField] private bool mixIncomingColorAutomatically = true;

    [Tooltip("Use a custom result color when two different liquids are mixed. Example: orange + cyan = green.")]
    [SerializeField] private bool useCustomMixedColor = false;

    [Tooltip("Manual visual result used when useCustomMixedColor is enabled.")]
    [SerializeField] private Color customMixedColor = new Color(0.1f, 0.85f, 0.25f, 0.45f);

    [Header("Movement / Slosh")]
    [Tooltip("Makes the liquid surface try to remain horizontal when the container is tilted.")]
    [SerializeField] private bool simulateSurfaceMovement = true;

    [Tooltip("How much the visible liquid body leans with movement/tilt. Keep low for VR performance and stability.")]
    [Range(0f, 1f)]
    [SerializeField] private float sloshAmount = 0.35f;

    [Tooltip("How quickly the liquid visual catches up after movement.")]
    [Range(1f, 20f)]
    [SerializeField] private float sloshResponsiveness = 8f;

    [Tooltip("Maximum visual tilt of the liquid surface in degrees.")]
    [Range(0f, 35f)]
    [SerializeField] private float maxSurfaceTilt = 18f;

    [Header("Container Fit")]
    [Tooltip("Prevents the visible liquid body from leaning outside narrow containers such as test tubes.")]
    [SerializeField] private bool preventVisualOverflow = true;

    [Tooltip("Extra safety margin used when clamping the liquid body tilt inside the container.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float overflowSafetyMargin = 0.55f;

    [Header("Test values for RF-03")]
    [Tooltip("Inspector test pH. RF-04 can later call SetPH().")]
    [Range(0f, 14f)]
    [SerializeField] private float testPH = 7f;

    [Tooltip("Inspector test concentration normalized 0..1. RF-04 can later call SetConcentration().")]
    [Range(0f, 1f)]
    [SerializeField] private float testConcentration = 0.25f;

    [Tooltip("Select how the visual color is calculated.")]
    [SerializeField] private AutoColorMode colorMode = AutoColorMode.ByPH;

    [Tooltip("If disabled, color remains baseColor but level/transparency/meniscus still update.")]
    [SerializeField] private bool updateColorAutomatically = true;

    private const string BodyName = "RF03_LiquidBody";
    private const string MeniscusName = "RF03_Meniscus";
    private const int MeniscusSegments = 48; // Good balance for Quest 2/3: smooth enough, low vertex count.
    private const float EmptyVisualLevelThreshold = 0.01f;

    private Transform bodyTransform;
    private MeshRenderer bodyRenderer;
    private Transform meniscusTransform;
    private MeshFilter meniscusMeshFilter;
    private MeshRenderer meniscusRenderer;
    private Material runtimeMaterial;
    private MaterialPropertyBlock materialPropertyBlock;
    private Mesh meniscusMesh;
    private Quaternion currentSurfaceLocalRotation = Quaternion.identity;
    private Vector3 lastWorldPosition;
    private Vector3 smoothedVelocity;

    public float LiquidLevel => liquidLevel;
    public float TestPH => testPH;
    public float TestConcentration => testConcentration;
    public float LiquidRadius => liquidRadius;
    public float MaxLiquidHeight => maxLiquidHeight;
    public Color CurrentVisualColor => GetResolvedColorWithAlpha();

    private void Awake()
    {
        EnsureVisualObjects();
        lastWorldPosition = transform.position;
        ApplyVisualState(true);
    }

    private void OnEnable()
    {
        EnsureVisualObjects();
        lastWorldPosition = transform.position;
        ApplyVisualState(true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            ApplyVisualState(true);
            return;
        }

        UpdateSloshMotion();
        ApplyVisualState(false);
    }

    private void OnValidate()
    {
        bottomOffset = Mathf.Max(0f, bottomOffset);
        maxLiquidHeight = Mathf.Max(0.001f, maxLiquidHeight);
        liquidRadius = Mathf.Max(0.001f, liquidRadius);
        liquidLevel = Mathf.Clamp01(liquidLevel);
        testPH = Mathf.Clamp(testPH, 0f, 14f);
        testConcentration = Mathf.Clamp01(testConcentration);
        transparency = Mathf.Clamp(transparency, 0.05f, 1f);
        meniscusIntensity = Mathf.Clamp01(meniscusIntensity);
        sloshAmount = Mathf.Clamp01(sloshAmount);
        sloshResponsiveness = Mathf.Max(1f, sloshResponsiveness);
        maxSurfaceTilt = Mathf.Clamp(maxSurfaceTilt, 0f, 35f);
        overflowSafetyMargin = Mathf.Clamp(overflowSafetyMargin, 0.1f, 1f);

        EnsureVisualObjects();
        ApplyVisualState(true);
    }

    /// <summary>Future RF-04 hook: sets a visual-only pH value.</summary>
    public void SetPH(float ph)
    {
        testPH = Mathf.Clamp(ph, 0f, 14f);
        colorMode = AutoColorMode.ByPH;
        updateColorAutomatically = true;
        ApplyVisualState(false);
    }

    /// <summary>Future RF-04 hook: sets a visual-only concentration value normalized 0..1.</summary>
    public void SetConcentration(float concentration)
    {
        testConcentration = Mathf.Clamp01(concentration);
        colorMode = AutoColorMode.ByConcentration;
        updateColorAutomatically = true;
        ApplyVisualState(false);
    }

    /// <summary>Sets liquid level normalized 0..1.</summary>
    public void SetLiquidLevel(float level)
    {
        liquidLevel = Mathf.Clamp01(level);
        if (liquidLevel <= EmptyVisualLevelThreshold)
        {
            liquidLevel = 0f;
        }
        ApplyVisualState(false);
    }

    /// <summary>Adds or removes visual liquid amount. Positive fills, negative empties.</summary>
    public float AddLiquidLevel(float delta)
    {
        float previousLevel = liquidLevel;
        SetLiquidLevel(liquidLevel + delta);
        return liquidLevel - previousLevel;
    }

    /// <summary>Sets a manual liquid color while preserving current transparency.</summary>
    public void SetLiquidColor(Color color)
    {
        baseColor = color;
        colorMode = AutoColorMode.Manual;
        updateColorAutomatically = false;
        ApplyVisualState(false);
    }

    /// <summary>
    /// Receives poured visual liquid. If empty, the incoming color is adopted.
    /// If it already contains liquid, it can blend or use a custom result color.
    /// Returns the accepted level delta.
    /// </summary>
    public float ReceiveLiquid(float deltaLevel, Color incomingColor)
    {
        if (deltaLevel <= 0f)
        {
            return 0f;
        }

        float previousLevel = liquidLevel;
        float acceptedDelta = AddLiquidLevel(deltaLevel);
        if (acceptedDelta <= 0f)
        {
            return 0f;
        }

        incomingColor.a = transparency;

        if (previousLevel <= 0.001f && useIncomingColorWhenEmpty)
        {
            SetLiquidColor(incomingColor);
        }
        else if (mixIncomingColorAutomatically)
        {
            Color mixedColor = useCustomMixedColor
                ? customMixedColor
                : Color.Lerp(CurrentVisualColor, incomingColor, acceptedDelta / Mathf.Max(previousLevel + acceptedDelta, 0.001f));
            mixedColor.a = transparency;
            SetLiquidColor(mixedColor);
        }

        return acceptedDelta;
    }

    /// <summary>Approximate world position of the liquid surface, useful for visual pouring.</summary>
    public Vector3 GetSurfaceWorldPosition()
    {
        float height = Mathf.Max(0.001f, maxLiquidHeight * liquidLevel);
        return transform.TransformPoint(new Vector3(0f, bottomOffset + height, 0f));
    }

    /// <summary>Approximate world position at the rim/edge of the liquid surface.</summary>
    public Vector3 GetSurfaceEdgeWorldPosition(Vector3 worldDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        localDirection.y = 0f;
        if (localDirection.sqrMagnitude < 0.0001f)
        {
            localDirection = Vector3.forward;
        }
        localDirection.Normalize();

        float height = Mathf.Max(0.001f, maxLiquidHeight * liquidLevel);
        Vector3 localPoint = new Vector3(localDirection.x * liquidRadius, bottomOffset + height, localDirection.z * liquidRadius);
        return transform.TransformPoint(localPoint);
    }

    private void EnsureVisualObjects()
    {
        EnsureBodyObject();
        EnsureMeniscusObject();
        EnsureMaterial();
    }

    private void EnsureBodyObject()
    {
        bodyTransform = transform.Find(BodyName);

        if (bodyTransform == null)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = BodyName;
            body.transform.SetParent(transform, false);

            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(bodyCollider);
                }
                else
#endif
                {
                    Destroy(bodyCollider);
                }
            }

            bodyTransform = body.transform;
        }

        bodyRenderer = bodyTransform.GetComponent<MeshRenderer>();
    }

    private void EnsureMeniscusObject()
    {
        meniscusTransform = transform.Find(MeniscusName);

        if (meniscusTransform == null)
        {
            GameObject meniscus = new GameObject(MeniscusName);
            meniscus.transform.SetParent(transform, false);
            meniscusMeshFilter = meniscus.AddComponent<MeshFilter>();
            meniscusRenderer = meniscus.AddComponent<MeshRenderer>();
            meniscusTransform = meniscus.transform;
        }
        else
        {
            meniscusMeshFilter = meniscusTransform.GetComponent<MeshFilter>();
            if (meniscusMeshFilter == null)
            {
                meniscusMeshFilter = meniscusTransform.gameObject.AddComponent<MeshFilter>();
            }

            meniscusRenderer = meniscusTransform.GetComponent<MeshRenderer>();
            if (meniscusRenderer == null)
            {
                meniscusRenderer = meniscusTransform.gameObject.AddComponent<MeshRenderer>();
            }
        }
    }

    private void EnsureMaterial()
    {
        if (runtimeMaterial == null)
        {
            Material source = liquidMaterial != null ? liquidMaterial : CreateDefaultLiquidMaterial();
            runtimeMaterial = source;
        }

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.sharedMaterial = runtimeMaterial;
        }

        if (meniscusRenderer != null)
        {
            meniscusRenderer.sharedMaterial = runtimeMaterial;
        }
    }

    private Material CreateDefaultLiquidMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = "RF03_Runtime_Transparent_Liquid",
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        ConfigureMaterialForTransparency(material);
        return material;
    }

    private void ConfigureMaterialForTransparency(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.78f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void UpdateSloshMotion()
    {
        if (!simulateSurfaceMovement)
        {
            currentSurfaceLocalRotation = Quaternion.Slerp(currentSurfaceLocalRotation, Quaternion.identity, Time.deltaTime * sloshResponsiveness);
            lastWorldPosition = transform.position;
            return;
        }

        float safeDeltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = (transform.position - lastWorldPosition) / safeDeltaTime;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, Time.deltaTime * sloshResponsiveness);
        lastWorldPosition = transform.position;

        // The liquid surface mostly wants to stay horizontal in world space.
        Quaternion worldHorizontal = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized == Vector3.zero ? Vector3.forward : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
        Quaternion targetLocal = Quaternion.Inverse(transform.rotation) * worldHorizontal;

        // Add a small inertial counter-tilt when the container moves quickly.
        Vector3 localVelocity = transform.InverseTransformDirection(smoothedVelocity);
        Quaternion inertialTilt = Quaternion.Euler(
            Mathf.Clamp(localVelocity.z * -sloshAmount, -maxSurfaceTilt, maxSurfaceTilt),
            0f,
            Mathf.Clamp(localVelocity.x * sloshAmount, -maxSurfaceTilt, maxSurfaceTilt));

        targetLocal *= inertialTilt;
        currentSurfaceLocalRotation = Quaternion.Slerp(currentSurfaceLocalRotation, targetLocal, Time.deltaTime * sloshResponsiveness);
    }

    private void ApplyVisualState(bool immediate)
    {
        if (bodyTransform == null || meniscusTransform == null)
        {
            return;
        }

        bool hasVisibleLiquid = liquidLevel > EmptyVisualLevelThreshold;
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = hasVisibleLiquid;
        }
        if (meniscusRenderer != null)
        {
            meniscusRenderer.enabled = hasVisibleLiquid;
        }
        if (!hasVisibleLiquid)
        {
            return;
        }

        float height = Mathf.Max(0.001f, maxLiquidHeight * liquidLevel);
        float topY = bottomOffset + height;

        bodyTransform.localPosition = new Vector3(0f, bottomOffset + height * 0.5f, 0f);
        bodyTransform.localRotation = GetContainedBodyRotation(height);
        bodyTransform.localScale = new Vector3(liquidRadius * 2f, height * 0.5f, liquidRadius * 2f);

        meniscusTransform.localPosition = new Vector3(0f, topY, 0f);
        meniscusTransform.localRotation = immediate ? currentSurfaceLocalRotation : Quaternion.Slerp(meniscusTransform.localRotation, currentSurfaceLocalRotation, Time.deltaTime * sloshResponsiveness);
        meniscusTransform.localScale = Vector3.one;

        BuildMeniscusMesh();
        ApplyMaterialColor();
    }

    private Quaternion GetContainedBodyRotation(float height)
    {
        if (!simulateSurfaceMovement)
        {
            return Quaternion.identity;
        }

        Quaternion bodyRotation = Quaternion.Slerp(Quaternion.identity, currentSurfaceLocalRotation, sloshAmount * 0.45f);
        if (!preventVisualOverflow)
        {
            return bodyRotation;
        }

        float halfHeight = Mathf.Max(height * 0.5f, 0.001f);
        float allowedTilt = Mathf.Atan2(liquidRadius * overflowSafetyMargin, halfHeight) * Mathf.Rad2Deg;
        allowedTilt = Mathf.Clamp(allowedTilt, 0f, maxSurfaceTilt);

        return Quaternion.RotateTowards(Quaternion.identity, bodyRotation, allowedTilt);
    }

    private void BuildMeniscusMesh()
    {
        if (meniscusMeshFilter == null)
        {
            return;
        }

        if (meniscusMesh == null)
        {
            meniscusMesh = new Mesh { name = "RF03_MeniscusMesh" };
            meniscusMeshFilter.sharedMesh = meniscusMesh;
        }

        int vertexCount = MeniscusSegments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[MeniscusSegments * 3];

        float edgeLift = Mathf.Lerp(0f, 0.018f, meniscusIntensity);
        vertices[0] = new Vector3(0f, -edgeLift * 0.35f, 0f);
        normals[0] = Vector3.up;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i <= MeniscusSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / MeniscusSegments;
            float x = Mathf.Cos(angle) * liquidRadius;
            float z = Mathf.Sin(angle) * liquidRadius;
            vertices[i + 1] = new Vector3(x, edgeLift, z);
            normals[i + 1] = Vector3.up;
            uvs[i + 1] = new Vector2((x / liquidRadius + 1f) * 0.5f, (z / liquidRadius + 1f) * 0.5f);
        }

        for (int i = 0; i < MeniscusSegments; i++)
        {
            int index = i * 3;
            triangles[index] = 0;
            triangles[index + 1] = i + 1;
            triangles[index + 2] = i + 2;
        }

        meniscusMesh.Clear();
        meniscusMesh.vertices = vertices;
        meniscusMesh.normals = normals;
        meniscusMesh.uv = uvs;
        meniscusMesh.triangles = triangles;
        meniscusMesh.RecalculateBounds();
    }

    private void ApplyMaterialColor()
    {
        EnsureMaterial();
        if (runtimeMaterial == null || materialPropertyBlock == null)
        {
            return;
        }

        Color finalColor = GetResolvedColorWithAlpha();

        materialPropertyBlock.Clear();
        materialPropertyBlock.SetColor("_BaseColor", finalColor);
        materialPropertyBlock.SetColor("_Color", finalColor);
        materialPropertyBlock.SetColor("_SpecColor", Color.Lerp(Color.white, finalColor, 0.25f));

        if (bodyRenderer != null)
        {
            bodyRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        if (meniscusRenderer != null)
        {
            meniscusRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    private Color GetResolvedColorWithAlpha()
    {
        Color finalColor = GetResolvedColor();
        finalColor.a = transparency;
        return finalColor;
    }

    private Color GetResolvedColor()
    {
        if (!updateColorAutomatically || colorMode == AutoColorMode.Manual)
        {
            return baseColor;
        }

        if (colorMode == AutoColorMode.ByConcentration)
        {
            return ColorByConcentration(testConcentration);
        }

        return ColorByPH(testPH);
    }

    private Color ColorByPH(float ph)
    {
        Color acidic = new Color(1f, 0.18f, 0.08f, transparency);
        Color neutral = new Color(0.70f, 0.95f, 1f, transparency);
        Color basic = new Color(0.55f, 0.10f, 1f, transparency);

        if (ph < 7f)
        {
            return Color.Lerp(acidic, neutral, Mathf.InverseLerp(0f, 7f, ph));
        }

        return Color.Lerp(neutral, basic, Mathf.InverseLerp(7f, 14f, ph));
    }

    private Color ColorByConcentration(float concentration)
    {
        Color dilute = new Color(baseColor.r, baseColor.g, baseColor.b, transparency);
        Color concentrated = Color.Lerp(baseColor, new Color(baseColor.r * 0.65f, baseColor.g * 0.65f, baseColor.b * 0.65f, transparency), 0.55f);
        return Color.Lerp(dilute, concentrated, Mathf.Clamp01(concentration));
    }
}
