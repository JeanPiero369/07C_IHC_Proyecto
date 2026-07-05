using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Controla la interacción bimanual de desenroscar una tapa de una botella.
/// Mantiene la tapa visualmente "pegada" a la boca de la botella mientras
/// no esté completamente desenroscada. Detecta cuando ambas (botella y tapa)
/// son agarradas simultáneamente y mide la rotación acumulada de la tapa
/// respecto a la botella en el eje Y local. Al superar un umbral, libera
/// la tapa para que pueda manipularse independientemente.
/// </summary>
public class CapUnscrewController : MonoBehaviour
{
    [Header("Referencias requeridas")]
    [SerializeField] private Grabbable bottleGrabbable;
    [SerializeField] private Grabbable lidGrabbable;
    [SerializeField] private Transform bottleTransform;
    [SerializeField] private Transform lidTransform;

    [Header("Posición de la tapa relativa a la botella")]
    [Tooltip("Offset local desde el origen de la botella hasta donde se ancla la tapa cuando está enroscada.")]
    [SerializeField] private Vector3 lidLocalOffset = Vector3.zero;

    [Header("Parámetros de desenrosque")]
    [Tooltip("Grados totales acumulados de rotación para considerar la tapa desenroscada.")]
    [SerializeField] private float requiredRotationDegrees = 540f;

    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = true;

    // Estado interno
    private readonly HashSet<int> bottleActivePointers = new HashSet<int>();
    private readonly HashSet<int> lidActivePointers = new HashSet<int>();
    private bool isUnscrewed = false;
    private float accumulatedRotation = 0f;
    private float previousRelativeYAngle = 0f;

    private bool BottleIsHeld => bottleActivePointers.Count > 0;
    private bool LidIsHeld => lidActivePointers.Count > 0;

    private void OnEnable()
    {
        if (bottleGrabbable != null)
            bottleGrabbable.WhenPointerEventRaised += OnBottlePointerEvent;
        if (lidGrabbable != null)
            lidGrabbable.WhenPointerEventRaised += OnLidPointerEvent;
    }

    private void OnDisable()
    {
        if (bottleGrabbable != null)
            bottleGrabbable.WhenPointerEventRaised -= OnBottlePointerEvent;
        if (lidGrabbable != null)
            lidGrabbable.WhenPointerEventRaised -= OnLidPointerEvent;
    }

    private void Start()
    {
        previousRelativeYAngle = GetRelativeYAngle();
    }

    private void LateUpdate()
    {
        if (isUnscrewed) return;

        // Mientras la tapa NO esté agarrada, la mantenemos pegada a la botella
        if (!LidIsHeld)
        {
            lidTransform.position = bottleTransform.TransformPoint(lidLocalOffset);
            lidTransform.rotation = bottleTransform.rotation;
        }

        // Si ambas están agarradas simultáneamente, medimos la rotación relativa
        if (BottleIsHeld && LidIsHeld)
        {
            float currentY = GetRelativeYAngle();
            float deltaY = Mathf.DeltaAngle(previousRelativeYAngle, currentY);

            accumulatedRotation += Mathf.Abs(deltaY);

            if (logDebugInfo && Mathf.Abs(deltaY) > 0.5f)
            {
                Debug.Log($"[Unscrew] Δ={deltaY:F2}°  Acumulado={accumulatedRotation:F2}° / {requiredRotationDegrees}°");
            }

            if (accumulatedRotation >= requiredRotationDegrees)
            {
                Unscrew();
            }

            previousRelativeYAngle = currentY;
        }
        else
        {
            previousRelativeYAngle = GetRelativeYAngle();
        }
    }

    private float GetRelativeYAngle()
    {
        Quaternion relativeRot = Quaternion.Inverse(bottleTransform.rotation) * lidTransform.rotation;
        return relativeRot.eulerAngles.y;
    }

    private void OnBottlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            bottleActivePointers.Add(evt.Identifier);
            if (logDebugInfo) Debug.Log($"[Unscrew] Botella agarrada (pointer {evt.Identifier})");
        }
        else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
        {
            bottleActivePointers.Remove(evt.Identifier);
            if (logDebugInfo) Debug.Log($"[Unscrew] Botella soltada (pointer {evt.Identifier})");
        }
    }

    private void OnLidPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            lidActivePointers.Add(evt.Identifier);
            if (logDebugInfo) Debug.Log($"[Unscrew] Tapa agarrada (pointer {evt.Identifier})");
        }
        else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
        {
            lidActivePointers.Remove(evt.Identifier);
            if (logDebugInfo) Debug.Log($"[Unscrew] Tapa soltada (pointer {evt.Identifier})");
        }
    }

    private void Unscrew()
    {
        isUnscrewed = true;
        if (logDebugInfo) Debug.Log("[Unscrew] ¡TAPA DESENROSCADA! 🎉");
    }

    [ContextMenu("Reset Cap State")]
    public void ResetCap()
    {
        isUnscrewed = false;
        accumulatedRotation = 0f;
        previousRelativeYAngle = GetRelativeYAngle();
        if (logDebugInfo) Debug.Log("[Unscrew] Estado reiniciado");
    }
}