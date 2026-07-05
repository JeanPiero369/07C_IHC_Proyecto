using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class VRExperiment3DButton : MonoBehaviour
{
    public enum ActionKind { SelectExperiment, StartSelected, Back }

    public ActionKind actionKind;
    public int experimentIndex;
    public VRExperimentSelectionController controller;
    public Transform movableRoot;
    public Renderer faceRenderer;
    public Renderer rimRenderer;
    public Renderer glowRenderer;
    public Renderer iconRenderer;
    public UnityEvent onPressed;

    public Color faceNormal = new Color(0.94f, 0.965f, 0.99f, 1f);
    public Color faceHover = Color.white;
    public Color facePressed = new Color(0.80f, 0.90f, 1f, 1f);
    public Color rimNormal = new Color(0.0f, 0.33f, 0.86f, 1f);
    public Color rimHover = new Color(0.0f, 0.65f, 1f, 1f);
    public Color selectedColor = new Color(0.0f, 0.70f, 1f, 1f);

    public float hoverZ = -8f;
    public float pressZ = 18f;
    public float hoverScale = 1.025f;
    public float pressScale = 0.975f;
    public float speed = 18f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    Vector3 basePos;
    Vector3 baseScale;
    bool hovered;
    bool pressed;
    bool selected;

    void Awake()
    {
        if (!movableRoot) movableRoot = transform;
        basePos = movableRoot.localPosition;
        baseScale = movableRoot.localScale;
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    void OnEnable()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
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
        if (!movableRoot) return;
        float z = pressed ? pressZ : (hovered ? hoverZ : 0f);
        float s = pressed ? pressScale : (hovered ? hoverScale : 1f);
        movableRoot.localPosition = Vector3.Lerp(movableRoot.localPosition, basePos + new Vector3(0, 0, z), Time.unscaledDeltaTime * speed);
        movableRoot.localScale = Vector3.Lerp(movableRoot.localScale, new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z), Time.unscaledDeltaTime * speed);

        Color face = pressed ? facePressed : (hovered ? faceHover : faceNormal);
        Color accent = selected ? selectedColor : (hovered ? rimHover : rimNormal);
        SetColor(faceRenderer, face);
        SetColor(rimRenderer, accent);
        SetColor(iconRenderer, accent);
        SetColor(glowRenderer, new Color(accent.r, accent.g, accent.b, selected ? 0.45f : (hovered ? 0.35f : 0.16f)));
    }

    public void SetSelected(bool value) { selected = value; }

    void OnHoverEnter(HoverEnterEventArgs args) { hovered = true; }
    void OnHoverExit(HoverExitEventArgs args) { hovered = false; pressed = false; }
    void OnSelectEnter(SelectEnterEventArgs args) { pressed = true; }
    void OnSelectExit(SelectExitEventArgs args)
    {
        pressed = false;
        if (controller)
        {
            if (actionKind == ActionKind.SelectExperiment) controller.SelectExperiment(experimentIndex);
            else if (actionKind == ActionKind.StartSelected) controller.StartSelectedExperiment();
            else if (actionKind == ActionKind.Back) controller.BackToMainMenu();
        }
        onPressed?.Invoke();
    }

    void SetColor(Renderer r, Color color)
    {
        if (!r || !r.sharedMaterial) return;
        if (Application.isPlaying) r.material.color = color;
        else r.sharedMaterial.color = color;
    }
}
