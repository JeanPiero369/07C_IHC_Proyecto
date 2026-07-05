using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class VR3DMenuButtonInteractable : MonoBehaviour
{
    public Button linkedUIButton;
    public Transform movableVisual;
    public Renderer faceRenderer;
    public Renderer rimRenderer;
    public Renderer iconRenderer;
    public Renderer glowRenderer;

    public Color faceNormal = new Color(0.93f, 0.96f, 0.99f, 1f);
    public Color faceHover = Color.white;
    public Color facePressed = new Color(0.80f, 0.90f, 1f, 1f);
    public Color blueNormal = new Color(0.0f, 0.23f, 0.62f, 1f);
    public Color blueHover = new Color(0.0f, 0.58f, 1f, 1f);
    public Color glowNormal = new Color(0.0f, 0.42f, 1f, 0.22f);
    public Color glowHover = new Color(0.0f, 0.70f, 1f, 0.55f);

    public float hoverForward = -10f;
    public float pressBackward = 20f;
    public float hoverScale = 1.025f;
    public float pressScale = 0.975f;
    public float speed = 18f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    Vector3 baseLocalPosition;
    Vector3 baseScale;
    bool hovered;
    bool pressed;

    void Awake()
    {
        if (!movableVisual) movableVisual = transform;
        baseLocalPosition = movableVisual.localPosition;
        baseScale = movableVisual.localScale;
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (!interactable) interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    void OnEnable()
    {
        if (!movableVisual) movableVisual = transform;
        baseLocalPosition = movableVisual.localPosition;
        baseScale = movableVisual.localScale;
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (!interactable) interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);
    }

    void OnDisable()
    {
        if (!interactable) return;
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelectEnter);
        interactable.selectExited.RemoveListener(OnSelectExit);
    }

    void Update()
    {
        if (!movableVisual) return;
        float z = pressed ? pressBackward : (hovered ? hoverForward : 0f);
        float s = pressed ? pressScale : (hovered ? hoverScale : 1f);
        movableVisual.localPosition = Vector3.Lerp(movableVisual.localPosition, baseLocalPosition + new Vector3(0f, 0f, z), Time.unscaledDeltaTime * speed);
        movableVisual.localScale = Vector3.Lerp(movableVisual.localScale, new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z), Time.unscaledDeltaTime * speed);

        SetRendererColor(faceRenderer, pressed ? facePressed : (hovered ? faceHover : faceNormal));
        Color blue = hovered ? blueHover : blueNormal;
        SetRendererColor(rimRenderer, blue);
        SetRendererColor(iconRenderer, blue);
        SetRendererColor(glowRenderer, hovered ? glowHover : glowNormal);
    }

    void OnHoverEnter(HoverEnterEventArgs args) { hovered = true; }
    void OnHoverExit(HoverExitEventArgs args) { hovered = false; pressed = false; }
    void OnSelectEnter(SelectEnterEventArgs args) { pressed = true; }
    void OnSelectExit(SelectExitEventArgs args)
    {
        pressed = false;
        if (linkedUIButton) linkedUIButton.onClick.Invoke();
    }

    void SetRendererColor(Renderer r, Color c)
    {
        if (!r || !r.sharedMaterial) return;
        if (Application.isPlaying) r.material.color = c;
        else r.sharedMaterial.color = c;
    }
}
