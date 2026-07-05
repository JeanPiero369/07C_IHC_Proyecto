using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VRButton3DFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Visual Targets")]
    public RectTransform movingRoot;
    public Graphic faceGraphic;
    public Graphic labelGraphic;
    public Transform depthBody;

    [Header("Motion")]
    public float hoverLiftZ = -10f;
    public float pressDepthZ = 26f;
    public float hoverScale = 1.035f;
    public float pressScale = 0.985f;
    public float animationSpeed = 16f;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.98f);
    public Color hoverColor = new Color(0.88f, 0.97f, 1f, 1f);
    public Color pressedColor = new Color(0.68f, 0.9f, 1f, 1f);
    public Color labelNormal = new Color(0.02f, 0.09f, 0.17f, 1f);
    public Color labelHover = new Color(0.0f, 0.42f, 0.86f, 1f);

    Vector3 _baseLocalPosition;
    Vector3 _baseScale;
    bool _hovered;
    bool _pressed;

    void Awake()
    {
        CacheBase();
    }

    void OnEnable()
    {
        CacheBase();
        ApplyImmediate();
    }

    void CacheBase()
    {
        if (!movingRoot)
            movingRoot = transform as RectTransform;
        if (!faceGraphic)
            faceGraphic = GetComponent<Graphic>();
        if (movingRoot)
        {
            _baseLocalPosition = movingRoot.localPosition;
            _baseScale = movingRoot.localScale;
        }
    }

    void Update()
    {
        if (!movingRoot)
            return;

        float zOffset = _pressed ? pressDepthZ : (_hovered ? hoverLiftZ : 0f);
        float scale = _pressed ? pressScale : (_hovered ? hoverScale : 1f);
        Vector3 targetPos = _baseLocalPosition + new Vector3(0f, 0f, zOffset);
        Vector3 targetScale = new Vector3(_baseScale.x * scale, _baseScale.y * scale, _baseScale.z);

        movingRoot.localPosition = Vector3.Lerp(movingRoot.localPosition, targetPos, Time.unscaledDeltaTime * animationSpeed);
        movingRoot.localScale = Vector3.Lerp(movingRoot.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        Color targetFace = _pressed ? pressedColor : (_hovered ? hoverColor : normalColor);
        Color targetLabel = _hovered || _pressed ? labelHover : labelNormal;
        if (faceGraphic)
            faceGraphic.color = Color.Lerp(faceGraphic.color, targetFace, Time.unscaledDeltaTime * animationSpeed);
        if (labelGraphic)
            labelGraphic.color = Color.Lerp(labelGraphic.color, targetLabel, Time.unscaledDeltaTime * animationSpeed);

        if (depthBody)
        {
            float depthScale = _pressed ? 0.62f : (_hovered ? 1.1f : 1f);
            depthBody.localScale = Vector3.Lerp(depthBody.localScale, new Vector3(depthBody.localScale.x, depthBody.localScale.y, Mathf.Max(1f, 16f * depthScale)), Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { _hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { _pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { _pressed = false; }

    public void ApplyImmediate()
    {
        if (!movingRoot)
            return;
        movingRoot.localPosition = _baseLocalPosition;
        movingRoot.localScale = _baseScale;
        if (faceGraphic)
            faceGraphic.color = normalColor;
        if (labelGraphic)
            labelGraphic.color = labelNormal;
    }
}
