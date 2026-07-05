using UnityEngine;
using UnityEngine.Events;

public class VRMenuButton : MonoBehaviour
{
    [Header("Evento al presionar")]
    public UnityEvent onPress;

    [Header("Visual feedback")]
    public Renderer faceRenderer;
    public Color colorNormal = new Color(0.93f, 0.96f, 0.985f, 1f);
    public Color colorHover  = new Color(0.75f, 0.88f, 1f, 1f);
    public Color colorPress  = new Color(0.4f, 0.7f, 1f, 1f);

    private bool yaPresionado = false;

    void OnTriggerEnter(Collider other)
    {
        if (yaPresionado) return;

        // Detecta si es una mano de Meta
        if (other.CompareTag("Hand") || 
            other.GetComponentInParent<OVRHand>() != null ||
            other.name.ToLower().Contains("hand") ||
            other.name.ToLower().Contains("finger") ||
            other.name.ToLower().Contains("index"))
        {
            yaPresionado = true;
            if (faceRenderer)
                faceRenderer.material.color = colorPress;
            onPress.Invoke();
            Invoke(nameof(Reset), 0.5f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (faceRenderer)
            faceRenderer.material.color = colorNormal;
    }

    void Reset()
    {
        yaPresionado = false;
        if (faceRenderer)
            faceRenderer.material.color = colorNormal;
    }
}