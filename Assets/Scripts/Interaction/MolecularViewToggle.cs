using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// RF-06: Permite alternar entre vista macroscópica y vista molecular.
/// Al agarrar el objeto con la mano, se activa automáticamente el modelo
/// molecular holográfico (barras y esferas) flotando sobre el objeto,
/// con un haz de luz en forma de cono invertido que conecta el objeto
/// con la molécula, rotación animada y etiquetas de átomos.
/// Al soltarlo, se desactiva.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class MolecularViewToggle : MonoBehaviour
{
    [Header("Vista Molecular")]
    public GameObject molecularViewRoot;
    public float rotationSpeed = 30f;
    public float heightMargin = 0.25f;
    public float moleculeScale = 0.15f;

    [Header("Efecto Holograma")]
    public Color hologramColor = new Color(0f, 0.8f, 1f, 0.6f);
    public float flickerSpeed = 2f;
    [Range(0f, 1f)] public float flickerIntensity = 0.15f;

    [Header("Haz de Luz")]
    [Tooltip("Ancho del haz en la base (sobre el objeto)")]
    public float beamBaseWidth = 0.02f;
    [Tooltip("Ancho del haz en la parte superior (junto a la molécula)")]
    public float beamTopWidth = 0.12f;
    [Tooltip("Color del haz de luz (celeste/azul holográfico)")]
    public Color beamColor = new Color(0f, 0.75f, 1f, 0.50f);
    [Tooltip("Separación entre el tope del objeto y el inicio del haz")]
    public float beamBottomGap = 0.03f;
    [Tooltip("Separación entre el final del haz y la molécula")]
    public float beamTopGap = 0.05f;

    [Header("Transición")]
    public float fadeDuration = 0.3f;

    [Header("Configuración de Molécula")]
    public MoleculeType moleculeType = MoleculeType.H2O;

    public enum MoleculeType { H2O, NaCl, HCl, Custom }

    private Grabbable _grabbable;
    private bool _isMolecularViewActive = false;
    private GameObject _moleculeInstance;
    private MoleculeBuilder _moleculeBuilder;
    private Renderer[] _fadeRenderers;
    private Color[] _originalColors;
    private float _flickerTimer;
    private float _currentFade = 0f;
    private float _targetFade = 0f;
    private Vector3 _calculatedOffset = Vector3.zero;
    private GameObject _beamObject;

    void Awake() { _grabbable = GetComponent<Grabbable>(); }

    void Start()
    {
        // Calcular offset UNA SOLA VEZ al inicio — posición fija relativa al objeto
        _calculatedOffset = CalculateTopCenterOffset();

        if (molecularViewRoot == null)
            molecularViewRoot = CreateMolecularViewRoot();

        BuildMolecule();
        CreateLightBeam();
        StripPhysicsFromMolecularView();
        molecularViewRoot.SetActive(false);
    }

    /// <summary>
    /// Calcula el centro superior del objeto usando los bounds combinados
    /// de todos sus renderers.
    /// </summary>
    private Vector3 CalculateTopCenterOffset()
    {
        var allRenderers = GetComponentsInChildren<Renderer>();
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (var r in allRenderers)
        {
            if (ShouldIgnoreRendererForObjectBounds(r)) continue;

            if (!hasBounds) { combinedBounds = r.bounds; hasBounds = true; }
            else { combinedBounds.Encapsulate(r.bounds); }
        }

        if (!hasBounds) return new Vector3(0, 0.1f, 0);

        Vector3 topCenterWorld = new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z);
        Vector3 localOffset = transform.InverseTransformPoint(topCenterWorld);
        localOffset.y += heightMargin;
        return localOffset;
    }

    private bool ShouldIgnoreRendererForObjectBounds(Renderer renderer)
    {
        if (renderer == null) return true;
        if (renderer.transform == transform) return true;
        if (renderer.GetComponentInParent<MoleculeBuilder>() != null) return true;
        if (renderer.GetComponentInParent<LiquidVisualController>() != null) return true;

        string rendererName = renderer.name;
        if (rendererName == "MolecularView") return true;
        if (rendererName == "LightBeam") return true;
        if (rendererName == "RF03_LiquidBody") return true;
        if (rendererName == "RF03_Meniscus") return true;
        if (rendererName.StartsWith("Label_")) return true;
        if (rendererName.StartsWith("LabelShadow_")) return true;

        Transform current = renderer.transform;
        while (current != null && current != transform)
        {
            string objectName = current.name;
            if (objectName == "Liquid_RF03") return true;
            if (objectName.StartsWith("RF03_")) return true;
            if (objectName == "MolecularView") return true;
            current = current.parent;
        }

        return false;
    }

    void Update()
    {
        bool isGrabbed = _grabbable.SelectingPointsCount > 0;

        if (isGrabbed && !_isMolecularViewActive) ToggleMolecularView(true);
        else if (!isGrabbed && _isMolecularViewActive) ToggleMolecularView(false);

        if (_isMolecularViewActive && _moleculeInstance != null)
        {
            _moleculeInstance.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            float yOffset = Mathf.Sin(Time.time * 1.5f) * 0.002f;
            Vector3 pos = molecularViewRoot.transform.localPosition;
            pos.x = _calculatedOffset.x;
            pos.y = _calculatedOffset.y + yOffset;
            pos.z = _calculatedOffset.z;
            molecularViewRoot.transform.localPosition = pos;

            _currentFade = Mathf.MoveTowards(_currentFade, _targetFade, Time.deltaTime / fadeDuration);
            ApplyFade();

            if (flickerIntensity > 0f && _fadeRenderers != null && _currentFade >= Mathf.Epsilon && _currentFade >= _targetFade * 0.95f)
            {
                _flickerTimer += Time.deltaTime * flickerSpeed;
                float flicker = 1f - flickerIntensity + Mathf.Sin(_flickerTimer * Mathf.PI * 2f) * flickerIntensity;
                for (int i = 0; i < _fadeRenderers.Length; i++)
                {
                    if (_fadeRenderers[i] != null && _fadeRenderers[i].material != null)
                    {
                        Color c = _originalColors[i];
                        c.a = _currentFade * Mathf.Lerp(0.8f, 1f, flicker);
                        _fadeRenderers[i].material.color = c;
                    }
                }
            }
        }
    }

    public void ToggleMolecularView(bool active)
    {
        _isMolecularViewActive = active;
        _targetFade = active ? 1f : 0f;
        if (active)
        {
            StripPhysicsFromMolecularView();
            molecularViewRoot.SetActive(true);
        }
        else StartCoroutine(DeactivateAfterFade());
    }

    private void StripPhysicsFromMolecularView()
    {
        if (molecularViewRoot == null) return;

        foreach (Rigidbody rb in molecularViewRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            DestroyComponentSafely(rb);
        }

        foreach (Collider collider in molecularViewRoot.GetComponentsInChildren<Collider>(true))
        {
            DestroyComponentSafely(collider);
        }
    }

    private void DestroyComponentSafely(Component component)
    {
        if (component == null) return;

        if (Application.isPlaying)
        {
            Destroy(component);
        }
        else
        {
            DestroyImmediate(component);
        }
    }

    private System.Collections.IEnumerator DeactivateAfterFade()
    {
        yield return new WaitForSeconds(fadeDuration);
        if (!_isMolecularViewActive) molecularViewRoot.SetActive(false);
    }

    private void ApplyFade()
    {
        if (_fadeRenderers == null) return;
        for (int i = 0; i < _fadeRenderers.Length; i++)
        {
            if (_fadeRenderers[i] != null && _fadeRenderers[i].material != null)
            {
                Color c = _originalColors[i];
                c.a = Mathf.Lerp(0f, _originalColors[i].a, _currentFade);
                _fadeRenderers[i].material.color = c;
            }
        }
    }

    private GameObject CreateMolecularViewRoot()
    {
        GameObject root = new GameObject("MolecularView");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = _calculatedOffset;
        return root;
    }

    /// <summary>
    /// Crea el haz de luz entre el tope del objeto y la molécula.
    /// - Empieza justo encima de la superficie del objeto (no desde dentro)
    /// - Termina justo antes de la molécula (no la intersecta)
    /// - Alpha: ~50% en la base → 0% en el tope
    /// </summary>
    private void CreateLightBeam()
    {
        // El MolecularViewRoot está en object_top + heightMargin (0.25 sobre el objeto)
        // El beam se posiciona RELATIVO al root.
        // Queremos que arranque justo encima del objeto:
        float beamBottomLocalY = -(heightMargin - beamBottomGap); // ej: -(0.25-0.03) = -0.22

        // Estimación del borde inferior de la molécula (el átomo más grande es Cl con radio ~0.55)
        // Con escala 0.15 y atomRadius 0.5: radio visual = 0.55 * 0.5 * 2 * 0.15 = 0.0825
        // Redondeamos a -0.09 para cubrir el caso más grande
        float moleculeBottomY = -0.09f;

        // El haz termina justo antes de moleculeBottom
        float beamTopLocalY = moleculeBottomY - beamTopGap;
        float beamHeight = beamTopLocalY - beamBottomLocalY;

        if (beamHeight < 0.02f) beamHeight = 0.08f;

        _beamObject = new GameObject("LightBeam");
        _beamObject.transform.SetParent(molecularViewRoot.transform, false);
        _beamObject.transform.localPosition = new Vector3(0, beamBottomLocalY, 0);

        int segments = 16;
        Mesh beamMesh = GenerateFrustumMesh(beamBaseWidth, beamTopWidth, beamHeight, segments);

        // Vertex colors: alpha 50% en base → 0% en tope
        // Shader multiplica: finalColor = _TintColor * vertexColor
        // Vertex WHITE con alpha variable (el tinte azul viene del material)
        Color[] vertexColors = new Color[beamMesh.vertexCount];
        Color bottomCol = new Color(1f, 1f, 1f, 0.50f);
        Color topCol    = new Color(1f, 1f, 1f, 0.00f);

        for (int i = 0; i < segments; i++) vertexColors[i] = bottomCol;
        for (int i = segments; i < 2 * segments; i++) vertexColors[i] = topCol;
        vertexColors[2 * segments] = bottomCol;
        vertexColors[2 * segments + 1] = topCol;

        beamMesh.colors = vertexColors;

        MeshFilter mf = _beamObject.AddComponent<MeshFilter>();
        mf.mesh = beamMesh;
        MeshRenderer mr = _beamObject.AddComponent<MeshRenderer>();
        mr.material = CreateBeamMaterial();
    }

    /// <summary>
    /// Genera un cono truncado (frustum) como mesh
    /// </summary>
    private Mesh GenerateFrustumMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        Mesh mesh = new Mesh();
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * bottomRadius, 0, Mathf.Sin(angle) * bottomRadius));
        }
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * topRadius, height, Mathf.Sin(angle) * topRadius));
        }
        int centerBottom = vertices.Count;
        vertices.Add(Vector3.zero);
        int centerTop = vertices.Count;
        vertices.Add(new Vector3(0, height, 0));

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int b0 = i, b1 = next, t0 = i + segments, t1 = next + segments;
            triangles.Add(b0); triangles.Add(b1); triangles.Add(t0);
            triangles.Add(t0); triangles.Add(b1); triangles.Add(t1);
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles.Add(centerBottom); triangles.Add(next); triangles.Add(i);
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles.Add(centerTop); triangles.Add(i + segments); triangles.Add(next + segments);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Material para el haz: Mobile/Particles/Alpha Blended.
    /// ¡Importante! Los shaders de partículas usan _TintColor, NO _Color.
    /// mat.color = X setea _Color (no leído), no _TintColor → por eso se veía blanco.
    /// </summary>
    private Material CreateBeamMaterial()
    {
        Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        if (shader.name == "Standard")
        {
            mat.SetFloat("_Mode", 3);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);
            // Standard usa _Color
            mat.color = beamColor;
        }
        else
        {
            // Shaders de partículas usan _TintColor
            mat.SetColor("_TintColor", beamColor);
        }

        mat.renderQueue = 3000;
        return mat;
    }

    private void BuildMolecule()
    {
        _moleculeBuilder = molecularViewRoot.GetComponent<MoleculeBuilder>();
        if (_moleculeBuilder == null)
            _moleculeBuilder = molecularViewRoot.AddComponent<MoleculeBuilder>();

        _moleculeBuilder.hologramColor = hologramColor;
        _moleculeBuilder.scale = moleculeScale;
        _moleculeBuilder.BuildMolecule(moleculeType);

        var allRenderers = molecularViewRoot.GetComponentsInChildren<Renderer>();
        var fadeList = new System.Collections.Generic.List<Renderer>();
        var colorList = new System.Collections.Generic.List<Color>();

        foreach (var r in allRenderers)
        {
            if (r.GetComponent<TextMesh>() != null) continue;
            if (r.name.StartsWith("Label_")) continue;
            if (r.name.StartsWith("LabelShadow_")) continue;
            fadeList.Add(r);
            colorList.Add(r.material != null ? r.material.color : Color.white);
        }

        _fadeRenderers = fadeList.ToArray();
        _originalColors = colorList.ToArray();
        _moleculeInstance = molecularViewRoot;
    }
}
