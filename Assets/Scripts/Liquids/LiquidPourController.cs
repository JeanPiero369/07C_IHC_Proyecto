using UnityEngine;

/// <summary>
/// RF-03 visual pouring helper.
/// Transfers visual liquid level from one LiquidVisualController to another while drawing
/// a lightweight stream with LineRenderer. This is not fluid physics and does not calculate chemistry.
/// </summary>
[DisallowMultipleComponent]
public class LiquidPourController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Liquid visual controlled by this container. If empty, it searches in children/parent.")]
    [SerializeField] private LiquidVisualController sourceLiquid;

    [Tooltip("Point where the stream starts. Place it near the lip/spout of the container.")]
    [SerializeField] private Transform pourOrigin;

    [Tooltip("Optional fixed target. If empty and autoDetectTarget is enabled, the nearest liquid below the stream is used.")]
    [SerializeField] private LiquidVisualController targetLiquid;

    [Header("Pour Behaviour")]
    [Tooltip("Enable simple automatic target search below the pour origin.")]
    [SerializeField] private bool autoDetectTarget = true;

    [Tooltip("Container tilt angle required before liquid starts pouring.")]
    [Range(15f, 130f)]
    [SerializeField] private float pourStartAngle = 65f;

    [Tooltip("How fast the visual liquid level moves from source to target per second at full pour.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float transferRate = 0.18f;

    [Tooltip("Maximum distance from source lip to target liquid surface.")]
    [SerializeField] private float maxPourDistance = 1.25f;

    [Tooltip("Horizontal search radius used for automatic target detection.")]
    [SerializeField] private float targetDetectionRadius = 0.35f;

    [Tooltip("Stop receiving liquid when target reaches this level.")]
    [Range(0f, 1f)]
    [SerializeField] private float targetMaxLevel = 0.98f;

    [Header("Stream Visual")]
    [Tooltip("Width of the falling liquid stream.")]
    [SerializeField] private float streamWidth = 0.012f;

    [Tooltip("Adds a slight curve to the falling stream so it does not look like a rigid laser line.")]
    [SerializeField] private float streamCurve = 0.055f;

    [Tooltip("Optional stream material. If empty, a transparent material is created at runtime.")]
    [SerializeField] private Material streamMaterial;

    private const string StreamName = "RF03_PourStream";
    private LineRenderer streamRenderer;
    private Material runtimeStreamMaterial;
    private PHSimulationManager sourceSimulation;
    private PHSimulationManager targetSimulation;

    public bool IsPouring { get; private set; }

    private void Awake()
    {
        EnsureReferences();
        EnsureStreamRenderer();
        SetStreamVisible(false);
    }

    private void OnEnable()
    {
        EnsureReferences();
        EnsureStreamRenderer();
        SetStreamVisible(false);
    }

    private void Update()
    {
        EnsureReferences();
        EnsureStreamRenderer();

        if (sourceLiquid == null || sourceLiquid.LiquidLevel <= 0.001f || pourOrigin == null)
        {
            StopPouring();
            return;
        }

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle < pourStartAngle)
        {
            StopPouring();
            return;
        }

        LiquidVisualController receiver = targetLiquid;
        if (receiver == null && autoDetectTarget)
        {
            receiver = FindBestTargetBelowPourOrigin();
        }

        if (receiver == null || receiver == sourceLiquid || receiver.LiquidLevel >= targetMaxLevel)
        {
            StopPouring();
            return;
        }

        Vector3 start = pourOrigin.position;
        Vector3 end = receiver.GetSurfaceWorldPosition();
        float distance = Vector3.Distance(start, end);
        if (distance > maxPourDistance || start.y < end.y)
        {
            StopPouring();
            return;
        }

        float pourStrength = Mathf.InverseLerp(pourStartAngle, 120f, tiltAngle);
        float requestedDelta = transferRate * pourStrength * Time.deltaTime;
        requestedDelta = Mathf.Min(requestedDelta, sourceLiquid.LiquidLevel);
        requestedDelta = Mathf.Min(requestedDelta, targetMaxLevel - receiver.LiquidLevel);

        if (requestedDelta <= 0f)
        {
            StopPouring();
            return;
        }

        sourceSimulation = PHSimulationManager.EnsureForLiquid(sourceLiquid);
        targetSimulation = PHSimulationManager.EnsureForLiquid(receiver);

        Color pouredColor = sourceLiquid.CurrentVisualColor;
        float acceptedDelta = receiver.ReceiveLiquid(requestedDelta, pouredColor);
        if (acceptedDelta <= 0f)
        {
            StopPouring();
            return;
        }

        sourceLiquid.AddLiquidLevel(-acceptedDelta);
        NotifyPHSimulation(receiver, acceptedDelta);
        DrawStream(start, end, pourStrength, pouredColor);
        IsPouring = true;
    }

    private void EnsureReferences()
    {
        if (sourceLiquid == null)
        {
            sourceLiquid = GetComponentInChildren<LiquidVisualController>();
        }

        if (sourceLiquid == null)
        {
            sourceLiquid = GetComponentInParent<LiquidVisualController>();
        }

        if (sourceSimulation == null)
        {
            sourceSimulation = GetComponent<PHSimulationManager>();
        }

        if (sourceSimulation == null && sourceLiquid != null)
        {
            sourceSimulation = PHSimulationManager.EnsureForLiquid(sourceLiquid);
        }

        if (pourOrigin == null)
        {
            Transform existing = transform.Find("PourOrigin_RF03");
            if (existing != null)
            {
                pourOrigin = existing;
            }
        }
    }

    private void EnsureStreamRenderer()
    {
        if (streamRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(StreamName);
        GameObject streamObject;
        if (existing == null)
        {
            streamObject = new GameObject(StreamName);
            streamObject.transform.SetParent(transform, false);
        }
        else
        {
            streamObject = existing.gameObject;
        }

        streamRenderer = streamObject.GetComponent<LineRenderer>();
        if (streamRenderer == null)
        {
            streamRenderer = streamObject.AddComponent<LineRenderer>();
        }

        streamRenderer.useWorldSpace = true;
        streamRenderer.positionCount = 3;
        streamRenderer.numCapVertices = 4;
        streamRenderer.numCornerVertices = 2;
        streamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        streamRenderer.receiveShadows = false;
        streamRenderer.textureMode = LineTextureMode.Stretch;

        if (runtimeStreamMaterial == null)
        {
            runtimeStreamMaterial = streamMaterial != null ? new Material(streamMaterial) : CreateDefaultStreamMaterial();
        }

        streamRenderer.sharedMaterial = runtimeStreamMaterial;
    }

    private Material CreateDefaultStreamMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader)
        {
            name = "RF03_Runtime_PourStream",
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        return material;
    }

    private LiquidVisualController FindBestTargetBelowPourOrigin()
    {
        LiquidVisualController[] liquids = FindObjectsOfType<LiquidVisualController>();
        LiquidVisualController best = null;
        float bestScore = float.MaxValue;
        Vector3 origin = pourOrigin.position;

        for (int i = 0; i < liquids.Length; i++)
        {
            LiquidVisualController candidate = liquids[i];
            if (candidate == null || candidate == sourceLiquid || candidate.LiquidLevel >= targetMaxLevel)
            {
                continue;
            }

            Vector3 surface = candidate.GetSurfaceWorldPosition();
            Vector3 horizontalDelta = Vector3.ProjectOnPlane(surface - origin, Vector3.up);
            float horizontalDistance = horizontalDelta.magnitude;
            float verticalDistance = origin.y - surface.y;

            if (verticalDistance <= 0f || verticalDistance > maxPourDistance || horizontalDistance > targetDetectionRadius)
            {
                continue;
            }

            float score = horizontalDistance + verticalDistance * 0.25f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private void NotifyPHSimulation(LiquidVisualController receiver, float acceptedDelta)
    {
        if (receiver == null || acceptedDelta <= 0f)
        {
            return;
        }

        targetSimulation = PHSimulationManager.EnsureForLiquid(receiver);
        if (targetSimulation != null)
        {
            // RF-04 chemistry hook: the visual RF-03 level delta becomes real added volume
            // through PHSimulationManager.Visual Full Volume Ml on the receiving container.
            targetSimulation.ReceiveTitrantLevel(acceptedDelta, sourceSimulation);
        }

        if (sourceSimulation != null)
        {
            sourceSimulation.RemoveTransferredLevel(acceptedDelta);
        }
    }

    private void DrawStream(Vector3 start, Vector3 end, float strength, Color color)
    {
        if (streamRenderer == null)
        {
            return;
        }

        SetStreamVisible(true);
        float width = Mathf.Lerp(streamWidth * 0.45f, streamWidth, strength);
        streamRenderer.startWidth = width;
        streamRenderer.endWidth = width * 0.65f;

        Vector3 mid = Vector3.Lerp(start, end, 0.5f);
        Vector3 sideways = Vector3.Cross(Vector3.up, (end - start).normalized);
        if (sideways.sqrMagnitude < 0.001f)
        {
            sideways = transform.right;
        }
        mid += sideways.normalized * streamCurve * strength;
        mid += Vector3.down * streamCurve * 0.5f;

        streamRenderer.SetPosition(0, start);
        streamRenderer.SetPosition(1, mid);
        streamRenderer.SetPosition(2, end);

        color.a = Mathf.Clamp(color.a + 0.2f, 0.25f, 0.85f);
        if (runtimeStreamMaterial != null)
        {
            if (runtimeStreamMaterial.HasProperty("_BaseColor")) runtimeStreamMaterial.SetColor("_BaseColor", color);
            if (runtimeStreamMaterial.HasProperty("_Color")) runtimeStreamMaterial.SetColor("_Color", color);
        }
    }

    private void StopPouring()
    {
        IsPouring = false;
        SetStreamVisible(false);
    }

    private void SetStreamVisible(bool visible)
    {
        if (streamRenderer != null && streamRenderer.enabled != visible)
        {
            streamRenderer.enabled = visible;
        }
    }
}
