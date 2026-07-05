using TMPro;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// RF-04: simple acid-base titration state for one container.
/// Attach this to the same container GameObject that owns LiquidPourController,
/// or to the Liquid_RF03 child. Assign Liquid Visual to the RF-03 LiquidVisualController.
/// </summary>
[DisallowMultipleComponent]
public class PHSimulationManager : MonoBehaviour
{
    public enum SolutionKind
    {
        StrongAcid,
        StrongBase,
        Neutral
    }

    public enum IndicatorKind
    {
        InspectorProfile,
        Phenolphthalein,
        MethylOrange
    }

    public enum InitialValueMode
    {
        ConcentrationMolar,
        PH
    }

    [System.Serializable]
    public struct IndicatorProfile
    {
        [Tooltip("Color used for low pH. For this RF-04 test, keep it orange.")]
        public Color acidicColor;

        [Tooltip("Color used around pH 7.")]
        public Color neutralColor;

        [Tooltip("Color used for high pH. For this RF-04 test, keep it violet.")]
        public Color basicColor;

        [Range(0f, 7f)]
        public float acidicPH;

        [Range(7f, 14f)]
        public float basicPH;

        public static IndicatorProfile Default => new IndicatorProfile
        {
            acidicColor = new Color(1f, 0.32f, 0.02f, 0.58f),
            neutralColor = new Color(0.75f, 0.9f, 0.95f, 0.50f),
            basicColor = new Color(0.48f, 0.12f, 1f, 0.58f),
            acidicPH = 2f,
            basicPH = 12f
        };
    }

    public struct ReagentSample
    {
        public float volumeMl;
        public float acidMoles;
        public float baseMoles;
        public SolutionKind dominantKind;
    }

    [Header("References")]
    [Tooltip("RF-03 visual liquid controlled by this chemical state.")]
    [SerializeField] private LiquidVisualController liquidVisual;

    [Tooltip("Optional TMP text in the VR scene. If empty, RF-04 can create a small floating display at runtime.")]
    [SerializeField] private TMP_Text phText;

    [Tooltip("Object used as anchor for the runtime floating display. Defaults to this transform.")]
    [SerializeField] private Transform displayAnchor;

    [Header("Initial Solution")]
    [Tooltip("Display name shown in the floating table. If empty, the container GameObject name is used.")]
    [SerializeField] private string solutionName = "";

    [Tooltip("When enabled, the simulation starts from LiquidVisualController.Test PH. Useful when Color Mode is Manual but pH is configured in RF-03.")]
    [SerializeField] private bool initializePHFromLiquidVisual = true;

    [Tooltip("When enabled, initial volume is calculated from LiquidVisualController.Liquid Level and Visual Full Volume Ml.")]
    [SerializeField] private bool initializeVolumeFromLiquidLevel = true;

    [Tooltip("Initial solution type inside this recipient.")]
    [SerializeField] private SolutionKind initialSolutionKind = SolutionKind.StrongAcid;

    [Tooltip("Choose whether the initial strength is configured by molar concentration or by pH.")]
    [SerializeField] private InitialValueMode initialValueMode = InitialValueMode.ConcentrationMolar;

    [Tooltip("Initial volume of the solution in milliliters.")]
    [Min(0f)]
    [SerializeField] private float initialVolumeMl = 25f;

    [Tooltip("Initial molar concentration of the analyte, in mol/L.")]
    [Min(0f)]
    [SerializeField] private float initialConcentrationM = 0.1f;

    [Tooltip("Initial pH used when Initial Value Mode is PH.")]
    [Range(0f, 14f)]
    [SerializeField] private float initialPH = 1f;

    [Tooltip("Initial color shown before RF-04 recalculates the indicator color.")]
    [SerializeField] private Color initialVisualColor = new Color(1f, 0.32f, 0.02f, 0.58f);

    [Tooltip("Use Initial Visual Color until another liquid is poured into this recipient.")]
    [SerializeField] private bool useInitialVisualColorBeforeMix = true;

    [Header("Incoming Titrant")]
    [Tooltip("Fallback titrant type when the poured source has no PHSimulationManager.")]
    [SerializeField] private SolutionKind defaultTitrantKind = SolutionKind.StrongBase;

    [Tooltip("Fallback titrant concentration in mol/L when the poured source has no PHSimulationManager.")]
    [Min(0f)]
    [SerializeField] private float defaultTitrantConcentrationM = 0.1f;

    [Tooltip("Manual test volume added by Add Default Titrant Event, in milliliters.")]
    [Min(0f)]
    [SerializeField] private float volumeAddedPerEventMl = 1f;

    [Header("Visual Mapping")]
    [Tooltip("How many milliliters correspond to LiquidLevel = 1 in RF-03 for this recipient.")]
    [Min(0.001f)]
    [SerializeField] private float visualFullVolumeMl = 100f;

    [Tooltip("Indicator palette used to map calculated pH into RF-03 color.")]
    [SerializeField] private IndicatorKind indicator = IndicatorKind.InspectorProfile;

    [Tooltip("Editable pH-to-color profile. Orange acid and violet base are the default RF-04 proof colors.")]
    [SerializeField] private IndicatorProfile indicatorProfile = IndicatorProfile.Default;

    [Tooltip("When enabled, RF-04 drives RF-03 liquid level from the calculated total volume.")]
    [SerializeField] private bool driveLiquidLevelFromVolume = true;

    [Tooltip("When enabled, RF-04 creates a simple floating pH display if no TMP text is assigned.")]
    [SerializeField] private bool createRuntimeDisplayIfMissing = true;

    [Tooltip("Shows the floating table only while the container is grabbed, plus a short time after mixing.")]
    [SerializeField] private bool showDisplayOnlyWhenGrabbed = true;

    [Tooltip("Seconds the display remains visible after this liquid changes because of pouring/mixing.")]
    [SerializeField] private float displayVisibleAfterChangeSeconds = 3f;

    [Tooltip("Local offset for the runtime pH display. Lower Y keeps it near the reagent.")]
    [SerializeField] private Vector3 runtimeDisplayLocalOffset = new Vector3(0f, 0.36f, 0f);

    [Tooltip("Scale for the runtime pH display.")]
    [SerializeField] private float runtimeDisplayScale = 0.01f;

    [Tooltip("Maximum RF-03 visual fill level. Chemistry volume can keep accumulating, but the mesh stays inside the container.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVisualLiquidLevel = 0.98f;

    [Header("Runtime State")]
    [SerializeField] private float totalVolumeMl;
    [SerializeField] private float titrantAddedMl;
    [SerializeField] private float acidVolumeAddedMl;
    [SerializeField] private float baseVolumeAddedMl;
    [SerializeField] private float neutralVolumeAddedMl;
    [SerializeField] private float acidMoles;
    [SerializeField] private float baseMoles;
    [Range(0f, 14f)]
    [SerializeField] private float currentPH = 7f;
    [SerializeField] private string solutionState = "Neutral";

    private const float MinimumVolumeLiters = 0.000001f;
    private Transform runtimeDisplayTransform;
    private MeshRenderer runtimeDisplayBackground;
    private MeshRenderer runtimeDisplayAccent;
    private Grabbable containerGrabbable;
    private float displayVisibleUntilTime;
    private float lastSyncedLiquidPH = -1f;
    private float lastSyncedLiquidLevel = -1f;

    public float TotalVolumeMl => totalVolumeMl;
    public float TitrantAddedMl => titrantAddedMl;
    public float AcidVolumeAddedMl => acidVolumeAddedMl;
    public float BaseVolumeAddedMl => baseVolumeAddedMl;
    public float NeutralVolumeAddedMl => neutralVolumeAddedMl;
    public float AcidMoles => acidMoles;
    public float BaseMoles => baseMoles;
    public float CurrentPH => currentPH;
    public string SolutionState => solutionState;
    public string SolutionName => string.IsNullOrWhiteSpace(solutionName) ? gameObject.name : solutionName;
    public SolutionKind CurrentSolutionKind => GetDominantSolutionKind();
    public float ResultingConcentrationM => CalculateDominantConcentrationM();

    public static PHSimulationManager EnsureForLiquid(LiquidVisualController liquid)
    {
        if (liquid == null)
        {
            return null;
        }

        PHSimulationManager manager = liquid.GetComponentInParent<PHSimulationManager>();
        if (manager != null)
        {
            return manager;
        }

        Transform host = liquid.transform.parent != null ? liquid.transform.parent : liquid.transform;
        manager = host.gameObject.AddComponent<PHSimulationManager>();
        return manager;
    }

    private void Awake()
    {
        EnsureReferences();
        ResetSimulation();
    }

    private void OnEnable()
    {
        EnsureReferences();
        UpdateVisualsAndDisplay();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            SyncUnmixedInspectorStateFromLiquidVisual();
            UpdateDisplayVisibility();
        }
    }

    private void LateUpdate()
    {
        FaceDisplayToCamera();
    }

    private void OnValidate()
    {
        initialVolumeMl = Mathf.Max(0f, initialVolumeMl);
        initialConcentrationM = Mathf.Max(0f, initialConcentrationM);
        initialPH = Mathf.Clamp(initialPH, 0f, 14f);
        defaultTitrantConcentrationM = Mathf.Max(0f, defaultTitrantConcentrationM);
        volumeAddedPerEventMl = Mathf.Max(0f, volumeAddedPerEventMl);
        visualFullVolumeMl = Mathf.Max(0.001f, visualFullVolumeMl);
        runtimeDisplayScale = Mathf.Max(0.001f, runtimeDisplayScale);
        displayVisibleAfterChangeSeconds = Mathf.Max(0f, displayVisibleAfterChangeSeconds);
        maxVisualLiquidLevel = Mathf.Clamp01(maxVisualLiquidLevel);
        indicatorProfile.acidicPH = Mathf.Clamp(indicatorProfile.acidicPH, 0f, 7f);
        indicatorProfile.basicPH = Mathf.Clamp(indicatorProfile.basicPH, 7f, 14f);

        if (!Application.isPlaying)
        {
            EnsureReferences();
            ResetSimulation();
        }
    }

    [ContextMenu("RF-04/Reset Simulation")]
    public void ResetSimulation()
    {
        EnsureReferences();

        float startingPH = GetConfiguredInitialPH();
        float startingVolumeMl = GetConfiguredInitialVolumeMl();
        SolutionKind startingKind = initializePHFromLiquidVisual && liquidVisual != null
            ? GetSolutionKindFromPH(startingPH)
            : initialSolutionKind;

        totalVolumeMl = startingVolumeMl;
        titrantAddedMl = 0f;
        acidVolumeAddedMl = 0f;
        baseVolumeAddedMl = 0f;
        neutralVolumeAddedMl = 0f;
        acidMoles = 0f;
        baseMoles = 0f;

        // Initial H+/OH- moles come from either molarity or pH, configured per reagent in the Inspector.
        AddMoles(startingKind, CalculateInitialMoles(startingPH, startingKind, startingVolumeMl));
        RecalculatePH();
        RememberSyncedLiquidState();
        UpdateVisualsAndDisplay();
    }

    [ContextMenu("RF-04/Add Default Titrant Event")]
    public void AddDefaultTitrantEvent()
    {
        ReagentSample sample = CreateFallbackSample(volumeAddedPerEventMl);
        ReceiveSample(sample);
    }

    /// <summary>
    /// Called by LiquidPourController after RF-03 accepts visual liquid.
    /// deltaLevel is converted into milliliters with visualFullVolumeMl.
    /// </summary>
    public void ReceiveTitrantLevel(float deltaLevel, PHSimulationManager sourceSolution)
    {
        if (deltaLevel <= 0f)
        {
            return;
        }

        float addedVolumeMl = Mathf.Max(0f, deltaLevel * visualFullVolumeMl);
        ReagentSample sample = sourceSolution != null
            ? sourceSolution.CreateOutgoingSample(addedVolumeMl)
            : CreateFallbackSample(addedVolumeMl);

        ReceiveSample(sample);
    }

    /// <summary>
    /// Removes volume from the source container during pouring. This keeps source state coherent,
    /// but it does not remove a specific fraction of acid/base unless the source actually has volume.
    /// </summary>
    public void RemoveTransferredLevel(float deltaLevel)
    {
        if (deltaLevel <= 0f || totalVolumeMl <= 0f)
        {
            return;
        }

        float removedVolumeMl = Mathf.Min(totalVolumeMl, deltaLevel * visualFullVolumeMl);
        float remainingFraction = Mathf.Clamp01((totalVolumeMl - removedVolumeMl) / Mathf.Max(totalVolumeMl, 0.001f));

        totalVolumeMl -= removedVolumeMl;
        acidMoles *= remainingFraction;
        baseMoles *= remainingFraction;

        RecalculatePH();
        ShowDisplayForAWhile();
        UpdateVisualsAndDisplay();
    }

    /// <summary>
    /// Adds titrant volume cumulatively, updates total volume, adds moles, then recalculates pH.
    /// </summary>
    public void AddTitrantVolume(float volumeMl, float concentrationM, SolutionKind titrantKind)
    {
        ReceiveSample(CreateSample(volumeMl, concentrationM, titrantKind));
    }

    /// <summary>
    /// Adds one reagent sample to this mixture. This is where RF-04 accumulates total
    /// volume and H+/OH- moles from every liquid poured into the recipient.
    /// </summary>
    public void ReceiveSample(ReagentSample sample)
    {
        sample.volumeMl = Mathf.Max(0f, sample.volumeMl);
        sample.acidMoles = Mathf.Max(0f, sample.acidMoles);
        sample.baseMoles = Mathf.Max(0f, sample.baseMoles);
        if (sample.volumeMl <= 0f)
        {
            return;
        }

        totalVolumeMl += sample.volumeMl;
        titrantAddedMl += sample.volumeMl;
        acidMoles += sample.acidMoles;
        baseMoles += sample.baseMoles;
        TrackAddedVolume(sample.dominantKind, sample.volumeMl);

        // After each pour, H+ and OH- are neutralized mathematically by RecalculatePH()
        // using the accumulated acidMoles/baseMoles balance, not just the last reagent.
        RecalculatePH();
        ShowDisplayForAWhile();
        UpdateVisualsAndDisplay();
    }

    public ReagentSample CreateOutgoingSample(float requestedVolumeMl)
    {
        requestedVolumeMl = Mathf.Max(0f, requestedVolumeMl);
        if (totalVolumeMl <= 0f || requestedVolumeMl <= 0f)
        {
            return new ReagentSample { dominantKind = CurrentSolutionKind };
        }

        float actualVolumeMl = Mathf.Min(requestedVolumeMl, totalVolumeMl);
        float fraction = actualVolumeMl / Mathf.Max(totalVolumeMl, 0.001f);

        return new ReagentSample
        {
            volumeMl = actualVolumeMl,
            acidMoles = acidMoles * fraction,
            baseMoles = baseMoles * fraction,
            dominantKind = CurrentSolutionKind
        };
    }

    private void AddMoles(SolutionKind kind, float moles)
    {
        moles = Mathf.Max(0f, moles);
        if (kind == SolutionKind.StrongAcid)
        {
            acidMoles += moles;
        }
        else if (kind == SolutionKind.StrongBase)
        {
            baseMoles += moles;
        }
    }

    private float CalculateInitialMoles(float configuredPH, SolutionKind configuredKind, float configuredVolumeMl)
    {
        float concentrationM = initialValueMode == InitialValueMode.PH || initializePHFromLiquidVisual
            ? ConcentrationFromPH(configuredPH, configuredKind)
            : initialConcentrationM;

        return concentrationM * MlToLiters(configuredVolumeMl);
    }

    private float GetConfiguredInitialPH()
    {
        if (initializePHFromLiquidVisual && liquidVisual != null)
        {
            return Mathf.Clamp(liquidVisual.TestPH, 0f, 14f);
        }

        return Mathf.Clamp(initialPH, 0f, 14f);
    }

    private float GetConfiguredInitialVolumeMl()
    {
        if (initializeVolumeFromLiquidLevel && liquidVisual != null)
        {
            return Mathf.Max(0f, liquidVisual.LiquidLevel * visualFullVolumeMl);
        }

        return Mathf.Max(0f, initialVolumeMl);
    }

    private SolutionKind GetSolutionKindFromPH(float ph)
    {
        const float neutralTolerance = 0.05f;
        if (ph < 7f - neutralTolerance) return SolutionKind.StrongAcid;
        if (ph > 7f + neutralTolerance) return SolutionKind.StrongBase;
        return SolutionKind.Neutral;
    }

    private void SyncUnmixedInspectorStateFromLiquidVisual()
    {
        if (!initializePHFromLiquidVisual || liquidVisual == null || titrantAddedMl > 0.0001f)
        {
            return;
        }

        float liquidPH = Mathf.Clamp(liquidVisual.TestPH, 0f, 14f);
        float liquidLevel = Mathf.Clamp01(liquidVisual.LiquidLevel);
        bool phChanged = Mathf.Abs(liquidPH - lastSyncedLiquidPH) > 0.001f;
        bool levelChanged = Mathf.Abs(liquidLevel - lastSyncedLiquidLevel) > 0.001f;

        if (!phChanged && !levelChanged)
        {
            return;
        }

        float startingVolumeMl = initializeVolumeFromLiquidLevel
            ? liquidLevel * visualFullVolumeMl
            : totalVolumeMl;
        SolutionKind startingKind = GetSolutionKindFromPH(liquidPH);

        totalVolumeMl = startingVolumeMl;
        acidMoles = 0f;
        baseMoles = 0f;
        AddMoles(startingKind, CalculateInitialMoles(liquidPH, startingKind, startingVolumeMl));
        RecalculatePH();
        RememberSyncedLiquidState();
        UpdateVisualsAndDisplay();
    }

    private void RememberSyncedLiquidState()
    {
        if (liquidVisual == null)
        {
            lastSyncedLiquidPH = currentPH;
            lastSyncedLiquidLevel = totalVolumeMl / Mathf.Max(visualFullVolumeMl, 0.001f);
            return;
        }

        lastSyncedLiquidPH = liquidVisual.TestPH;
        lastSyncedLiquidLevel = liquidVisual.LiquidLevel;
    }

    private ReagentSample CreateFallbackSample(float volumeMl)
    {
        return CreateSample(volumeMl, defaultTitrantConcentrationM, defaultTitrantKind);
    }

    private ReagentSample CreateSample(float volumeMl, float concentrationM, SolutionKind kind)
    {
        float moles = Mathf.Max(0f, concentrationM) * MlToLiters(volumeMl);
        return new ReagentSample
        {
            volumeMl = Mathf.Max(0f, volumeMl),
            acidMoles = kind == SolutionKind.StrongAcid ? moles : 0f,
            baseMoles = kind == SolutionKind.StrongBase ? moles : 0f,
            dominantKind = kind
        };
    }

    private void TrackAddedVolume(SolutionKind kind, float volumeMl)
    {
        if (kind == SolutionKind.StrongAcid)
        {
            acidVolumeAddedMl += volumeMl;
        }
        else if (kind == SolutionKind.StrongBase)
        {
            baseVolumeAddedMl += volumeMl;
        }
        else
        {
            neutralVolumeAddedMl += volumeMl;
        }
    }

    private void RecalculatePH()
    {
        float volumeLiters = Mathf.Max(MlToLiters(totalVolumeMl), MinimumVolumeLiters);
        float excessAcid = acidMoles - baseMoles;
        float excessBase = baseMoles - acidMoles;
        const float equivalenceToleranceMoles = 0.0000001f;

        // Simple strong acid/base titration model:
        // before equivalence use excess H+, at equivalence stay near neutral,
        // after equivalence use excess OH- and convert pOH to pH.
        if (excessAcid > equivalenceToleranceMoles)
        {
            float hConcentration = Mathf.Max(excessAcid / volumeLiters, 0.00000000000001f);
            currentPH = -Mathf.Log10(hConcentration);
            solutionState = "Acida";
        }
        else if (excessBase > equivalenceToleranceMoles)
        {
            float ohConcentration = Mathf.Max(excessBase / volumeLiters, 0.00000000000001f);
            float pOH = -Mathf.Log10(ohConcentration);
            currentPH = 14f - pOH;
            solutionState = "Basica";
        }
        else
        {
            currentPH = 7f;
            solutionState = "Equivalencia / neutra";
        }

        currentPH = Mathf.Clamp(currentPH, 0f, 14f);
    }

    private void UpdateVisualsAndDisplay()
    {
        EnsureReferences();

        if (liquidVisual != null)
        {
            if (totalVolumeMl <= 0.001f)
            {
                liquidVisual.SetLiquidLevel(0f);
                liquidVisual.SetLiquidColor(new Color(1f, 1f, 1f, 0f));
                UpdatePHText();
                return;
            }

            if (driveLiquidLevelFromVolume)
            {
                float visualLevel = Mathf.Min(totalVolumeMl / visualFullVolumeMl, maxVisualLiquidLevel);
                liquidVisual.SetLiquidLevel(visualLevel);
            }

            // RF-04 connects chemistry to RF-03 here: before mixing it can show the
            // configured reagent color; after mixing, pH selects the indicator color.
            if (titrantAddedMl > 0.0001f)
            {
                liquidVisual.SetLiquidColor(GetIndicatorColor(currentPH));
            }
            else if (!initializePHFromLiquidVisual && useInitialVisualColorBeforeMix)
            {
                liquidVisual.SetLiquidColor(initialVisualColor);
            }
        }

        UpdatePHText();
    }

    private void UpdatePHText()
    {
        if (phText == null && Application.isPlaying && createRuntimeDisplayIfMissing)
        {
            phText = CreateRuntimeDisplay();
        }

        if (phText == null)
        {
            return;
        }

        UpdateDisplayColors();

        phText.text =
            $"<align=\"center\"><size=5.8><b>{SolutionName}</b></size></align>\n" +
            $"<align=\"center\"><size=8.4><b>pH {currentPH:0.00}</b></size></align>\n" +
            $"Volumen  {totalVolumeMl:0.0} mL\n" +
            $"Conc.    {ResultingConcentrationM:0.000} M\n" +
            $"Estado   {solutionState}";
    }

    private TMP_Text CreateRuntimeDisplay()
    {
        Transform anchor = displayAnchor != null ? displayAnchor : transform;
        GameObject displayObject = new GameObject("RF04_pH_Display");
        displayObject.transform.SetParent(anchor, false);
        displayObject.transform.localPosition = runtimeDisplayLocalOffset;
        displayObject.transform.localRotation = Quaternion.identity;
        displayObject.transform.localScale = Vector3.one * runtimeDisplayScale;
        runtimeDisplayTransform = displayObject.transform;

        GameObject background = new GameObject("RF04_pH_Display_Background");
        background.name = "RF04_pH_Display_Background";
        background.transform.SetParent(displayObject.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.035f);
        background.transform.localRotation = Quaternion.identity;
        background.transform.localScale = new Vector3(18f, 10.5f, 1f);
        MeshFilter backgroundMeshFilter = background.AddComponent<MeshFilter>();
        backgroundMeshFilter.sharedMesh = CreateDisplayBackgroundMesh();
        runtimeDisplayBackground = background.AddComponent<MeshRenderer>();
        runtimeDisplayBackground.sortingOrder = 0;
        runtimeDisplayBackground.sharedMaterial = CreateDisplayBackgroundMaterial();

        GameObject accent = new GameObject("RF04_pH_Display_Accent");
        accent.transform.SetParent(displayObject.transform, false);
        accent.transform.localPosition = new Vector3(0f, 4.8f, 0.032f);
        accent.transform.localRotation = Quaternion.identity;
        accent.transform.localScale = new Vector3(17f, 0.55f, 1f);
        MeshFilter accentMeshFilter = accent.AddComponent<MeshFilter>();
        accentMeshFilter.sharedMesh = backgroundMeshFilter.sharedMesh;
        runtimeDisplayAccent = accent.AddComponent<MeshRenderer>();
        runtimeDisplayAccent.sortingOrder = 1;
        runtimeDisplayAccent.sharedMaterial = CreateDisplayAccentMaterial();

        TextMeshPro text = displayObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 4.5f;
        text.color = Color.black;
        text.enableWordWrapping = false;
        text.richText = true;
        text.margin = new Vector4(1.2f, 0.9f, 1.2f, 0.8f);
        text.renderer.sortingOrder = 10;
        UpdateDisplayVisibility();
        return text;
    }

    private Mesh CreateDisplayBackgroundMesh()
    {
        Mesh mesh = new Mesh { name = "RF04_PH_Table_Background_Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateDisplayBackgroundMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader)
        {
            name = "RF04_Runtime_PH_Table_Background",
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
        };

        Color panelColor = new Color(0.92f, 0.98f, 1f, 0.88f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", panelColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", panelColor);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        return material;
    }

    private Material CreateDisplayAccentMaterial()
    {
        Material material = CreateDisplayBackgroundMaterial();
        material.name = "RF04_Runtime_PH_Table_Accent";
        SetMaterialColor(material, GetPHAccentColor());
        return material;
    }

    private void UpdateDisplayColors()
    {
        Color backgroundColor = new Color(0.94f, 0.98f, 1f, 0.90f);
        Color accentColor = GetPHAccentColor();

        if (runtimeDisplayBackground != null && runtimeDisplayBackground.sharedMaterial != null)
        {
            SetMaterialColor(runtimeDisplayBackground.sharedMaterial, backgroundColor);
        }

        if (runtimeDisplayAccent != null && runtimeDisplayAccent.sharedMaterial != null)
        {
            SetMaterialColor(runtimeDisplayAccent.sharedMaterial, accentColor);
        }
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private Color GetPHAccentColor()
    {
        if (currentPH < 6.5f)
        {
            return new Color(1f, 0.28f, 0.12f, 0.96f);
        }

        if (currentPH > 7.5f)
        {
            return new Color(0.45f, 0.22f, 1f, 0.96f);
        }

        return new Color(0.12f, 0.72f, 0.52f, 0.96f);
    }

    private string FitTableValue(string value, int width)
    {
        if (string.IsNullOrEmpty(value))
        {
            value = "-";
        }

        value = value.Replace('\n', ' ').Replace('\r', ' ');
        if (value.Length > width)
        {
            value = value.Substring(0, Mathf.Max(0, width - 1)) + ".";
        }

        return value.PadRight(width);
    }

    private void ShowDisplayForAWhile()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        displayVisibleUntilTime = Time.time + displayVisibleAfterChangeSeconds;
        UpdateDisplayVisibility();
    }

    private void UpdateDisplayVisibility()
    {
        Transform displayTransform = runtimeDisplayTransform != null
            ? runtimeDisplayTransform
            : phText != null ? phText.transform : null;

        if (displayTransform == null)
        {
            return;
        }

        bool shouldShow = !showDisplayOnlyWhenGrabbed || IsContainerGrabbed() || Time.time <= displayVisibleUntilTime;
        if (displayTransform.gameObject.activeSelf != shouldShow)
        {
            displayTransform.gameObject.SetActive(shouldShow);
        }
    }

    private bool IsContainerGrabbed()
    {
        EnsureReferences();
        return containerGrabbable != null && containerGrabbable.SelectingPointsCount > 0;
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

    private void FaceDisplayToCamera()
    {
        Transform displayTransform = runtimeDisplayTransform != null
            ? runtimeDisplayTransform
            : phText != null ? phText.transform : null;
        Camera cameraToFace = Camera.main;

        if (displayTransform == null || cameraToFace == null)
        {
            return;
        }

        Vector3 direction = displayTransform.position - cameraToFace.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            displayTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private Color GetIndicatorColor(float ph)
    {
        ph = Mathf.Clamp(ph, 0f, 14f);

        if (indicator == IndicatorKind.Phenolphthalein)
        {
            Color clearAcid = new Color(0.92f, 0.97f, 1f, 0.42f);
            Color pinkBase = new Color(1f, 0.12f, 0.62f, 0.58f);
            return Color.Lerp(clearAcid, pinkBase, Mathf.InverseLerp(8.2f, 10f, ph));
        }

        if (indicator == IndicatorKind.MethylOrange)
        {
            Color acidRed = new Color(1f, 0.1f, 0.02f, 0.56f);
            Color baseYellow = new Color(1f, 0.82f, 0.05f, 0.50f);
            return Color.Lerp(acidRed, baseYellow, Mathf.InverseLerp(3.1f, 4.4f, ph));
        }

        IndicatorProfile profile = indicatorProfile;
        Color acidic = profile.acidicColor;
        Color neutral = profile.neutralColor;
        Color basic = profile.basicColor;
        float acidicPH = Mathf.Clamp(profile.acidicPH, 0f, 7f);
        float basicPH = Mathf.Clamp(profile.basicPH, 7f, 14f);

        return ph < 7f
            ? Color.Lerp(acidic, neutral, Mathf.InverseLerp(acidicPH, 7f, ph))
            : Color.Lerp(neutral, basic, Mathf.InverseLerp(7f, basicPH, ph));
    }

    private SolutionKind GetDominantSolutionKind()
    {
        if (acidMoles > baseMoles)
        {
            return SolutionKind.StrongAcid;
        }

        if (baseMoles > acidMoles)
        {
            return SolutionKind.StrongBase;
        }

        return SolutionKind.Neutral;
    }

    private float CalculateDominantConcentrationM()
    {
        float volumeLiters = Mathf.Max(MlToLiters(totalVolumeMl), MinimumVolumeLiters);
        float excessMoles = Mathf.Abs(acidMoles - baseMoles);
        return Mathf.Max(0f, excessMoles / volumeLiters);
    }

    private void EnsureReferences()
    {
        if (liquidVisual == null)
        {
            liquidVisual = GetComponentInChildren<LiquidVisualController>();
        }

        if (liquidVisual == null)
        {
            liquidVisual = GetComponentInParent<LiquidVisualController>();
        }

        if (displayAnchor == null)
        {
            displayAnchor = transform;
        }

        if (containerGrabbable == null)
        {
            containerGrabbable = GetComponentInParent<Grabbable>();
        }

        if (containerGrabbable == null && liquidVisual != null)
        {
            containerGrabbable = liquidVisual.GetComponentInParent<Grabbable>();
        }
    }

    private static float MlToLiters(float milliliters)
    {
        return Mathf.Max(0f, milliliters) * 0.001f;
    }

    private static float ConcentrationFromPH(float ph, SolutionKind kind)
    {
        ph = Mathf.Clamp(ph, 0f, 14f);
        if (kind == SolutionKind.StrongAcid)
        {
            return Mathf.Pow(10f, -ph);
        }

        if (kind == SolutionKind.StrongBase)
        {
            float pOH = 14f - ph;
            return Mathf.Pow(10f, -pOH);
        }

        return 0f;
    }
}
