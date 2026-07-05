using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class VRMenuButtonHandler : MonoBehaviour
{
    public enum Accion
    {
        IrASeleccionExperimento,
        ModoLibre,
        AcercaDe,
        Salir
    }

    [Header("Configuración")]
    public Accion accion = Accion.ModoLibre;

    [Header("Feedback Visual")]
    public Transform visualRoot;
    public Renderer faceRenderer;
    public Renderer rimRenderer;
    public Renderer glowRenderer;
    public Renderer iconRenderer;

    public Color faceNormal = new Color(0.93f, 0.96f, 0.99f, 1f);
    public Color faceHover = Color.white;
    public Color facePressed = new Color(0.80f, 0.90f, 1f, 1f);
    public Color rimNormal = new Color(0.0f, 0.23f, 0.62f, 1f);
    public Color rimHover = new Color(0.0f, 0.58f, 1f, 1f);
    public Color glowNormal = new Color(0.0f, 0.42f, 1f, 0.22f);
    public Color glowHover = new Color(0.0f, 0.70f, 1f, 0.55f);

    public float hoverAtras = -10f;
    public float pressAtras = 20f;
    public float hoverScale = 1.025f;
    public float pressScale = 0.975f;
    public float velocidadAnim = 18f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    Vector3 basePos;
    Vector3 baseScale;
    bool hovered;
    bool pressed;

    void Awake()
    {
        if (!visualRoot) visualRoot = transform;
        basePos = visualRoot.localPosition;
        baseScale = visualRoot.localScale;
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (!interactable) interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    void OnEnable()
    {
        if (!interactable) interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);
    }

    void OnDisable()
    {
        if (interactable == null) return;
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelectEnter);
        interactable.selectExited.RemoveListener(OnSelectExit);
    }

    void Update()
    {
        if (!visualRoot) return;

        float z = pressed ? pressAtras : (hovered ? hoverAtras : 0f);
        float s = pressed ? pressScale : (hovered ? hoverScale : 1f);

        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            basePos + new Vector3(0f, 0f, z),
            Time.unscaledDeltaTime * velocidadAnim
        );

        visualRoot.localScale = Vector3.Lerp(
            visualRoot.localScale,
            new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z),
            Time.unscaledDeltaTime * velocidadAnim
        );

        Color face = pressed ? facePressed : (hovered ? faceHover : faceNormal);
        Color accent = hovered ? rimHover : rimNormal;
        SetColor(faceRenderer, face);
        SetColor(rimRenderer, accent);
        SetColor(iconRenderer, accent);
        SetColor(glowRenderer, hovered ? glowHover : glowNormal);
    }

    void OnHoverEnter(HoverEnterEventArgs args) { hovered = true; }
    void OnHoverExit(HoverExitEventArgs args) { hovered = false; pressed = false; }
    void OnSelectEnter(SelectEnterEventArgs args) { pressed = true; }
    void OnSelectExit(SelectExitEventArgs args)
    {
        pressed = false;
        EjecutarAccion();
    }

    void EjecutarAccion()
    {
        var flow = FindObjectOfType<FlowManager>(true);
        if (flow == null)
        {
            Debug.LogWarning("[VRMenuButtonHandler] FlowManager no encontrado");
            return;
        }

        switch (accion)
        {
            case Accion.IrASeleccionExperimento:
                flow.IrAModoLibre();
                break;
            case Accion.ModoLibre:
                flow.IrAModoLibre();
                break;
            case Accion.AcercaDe:
                Debug.Log("[VRMenuButtonHandler] Acerca de — sin implementar aún");
                break;
            case Accion.Salir:
                Debug.Log("[VRMenuButtonHandler] Saliendo de la aplicación...");
#if UNITY_ANDROID
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("finish");
                }
#else
                Application.Quit();
#endif
                break;
        }
    }

    void SetColor(Renderer r, Color c)
    {
        if (!r || !r.sharedMaterial) return;
        if (Application.isPlaying) r.material.color = c;
        else r.sharedMaterial.color = c;
    }
}
