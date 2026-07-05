using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VRMenuPremiumFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform frontLayer;
    public RectTransform glowLayer;
    public Graphic faceGraphic;
    public Graphic borderGraphic;
    public Graphic iconTile;
    public Graphic iconGraphic;
    public Graphic labelGraphic;

    public Color normalFace = new Color(0.94f, 0.97f, 1f, 1f);
    public Color hoverFace = new Color(1f, 1f, 1f, 1f);
    public Color pressedFace = new Color(0.82f, 0.91f, 1f, 1f);
    public Color normalBlue = new Color(0.0f, 0.29f, 0.72f, 1f);
    public Color hoverBlue = new Color(0.0f, 0.58f, 1f, 1f);
    public Color pressedBlue = new Color(0.0f, 0.2f, 0.55f, 1f);
    public Color labelNormal = new Color(0.04f, 0.11f, 0.22f, 1f);
    public Color labelHover = new Color(0.0f, 0.25f, 0.62f, 1f);

    public float hoverZ = -8f;
    public float pressZ = 22f;
    public float hoverScale = 1.025f;
    public float pressScale = 0.975f;
    public float speed = 18f;

    Vector3 basePos;
    Vector3 baseScale;
    Vector2 baseGlowSize;
    bool hovered;
    bool pressed;

    void Awake() { Cache(); }
    void OnEnable() { Cache(); }

    void Cache()
    {
        if (!frontLayer) frontLayer = transform as RectTransform;
        if (frontLayer)
        {
            basePos = frontLayer.localPosition;
            baseScale = frontLayer.localScale;
        }
        if (glowLayer) baseGlowSize = glowLayer.sizeDelta;
    }

    void Update()
    {
        if (!frontLayer) return;

        float z = pressed ? pressZ : (hovered ? hoverZ : 0f);
        float s = pressed ? pressScale : (hovered ? hoverScale : 1f);
        frontLayer.localPosition = Vector3.Lerp(frontLayer.localPosition, basePos + new Vector3(0f, 0f, z), Time.unscaledDeltaTime * speed);
        frontLayer.localScale = Vector3.Lerp(frontLayer.localScale, new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z), Time.unscaledDeltaTime * speed);

        Color face = pressed ? pressedFace : (hovered ? hoverFace : normalFace);
        Color blue = pressed ? pressedBlue : (hovered ? hoverBlue : normalBlue);
        Color label = hovered || pressed ? labelHover : labelNormal;

        if (faceGraphic) faceGraphic.color = Color.Lerp(faceGraphic.color, face, Time.unscaledDeltaTime * speed);
        if (borderGraphic) borderGraphic.color = Color.Lerp(borderGraphic.color, blue, Time.unscaledDeltaTime * speed);
        if (iconTile) iconTile.color = Color.Lerp(iconTile.color, blue, Time.unscaledDeltaTime * speed);
        if (iconGraphic) iconGraphic.color = Color.Lerp(iconGraphic.color, Color.white, Time.unscaledDeltaTime * speed);
        if (labelGraphic) labelGraphic.color = Color.Lerp(labelGraphic.color, label, Time.unscaledDeltaTime * speed);
        if (glowLayer)
        {
            float glow = hovered ? 1.18f : (pressed ? 0.98f : 1f);
            glowLayer.sizeDelta = Vector2.Lerp(glowLayer.sizeDelta, baseGlowSize * glow, Time.unscaledDeltaTime * speed);
            var g = glowLayer.GetComponent<Graphic>();
            if (g) g.color = Color.Lerp(g.color, hovered ? new Color(0f, 0.55f, 1f, 0.42f) : new Color(0f, 0.36f, 1f, 0.24f), Time.unscaledDeltaTime * speed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
}
