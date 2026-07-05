using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class BotonInteractuable3D : MonoBehaviour
{
    [Header("Componentes Visuales a Hundir")]
    public Transform piezaMovil; // Arrastra aquí el objeto "Icono_Y_Texto"

    [Header("Configuración del Movimiento")]
    public Vector3 direccionHundimiento = new Vector3(0, 0, 0.05f); // Cuánto se hunde en el eje Z
    public float velocidadGesto = 15f;

    [Header("Evento del Botón")]
    public UnityEvent onClick; // Lo que pasará al hundirse

    private Vector3 posicionOriginal;
    private Vector3 posicionObjetivo;
    private bool estaPresionado = false;

    void Start()
    {
        if (piezaMovil != null)
        {
            posicionOriginal = piezaMovil.localPosition;
            posicionObjetivo = posicionOriginal;
        }
    }

    void Update()
    {
        // Suaviza el movimiento de hundimiento y regreso
        if (piezaMovil != null)
        {
            piezaMovil.localPosition = Vector3.Lerp(piezaMovil.localPosition, posicionObjetivo, Time.deltaTime * velocidadGesto);
        }
    }

    // Estos métodos serán llamados por el XR Ray Interactor de tus manos
    public void PresionarBoton()
    {
        if (!estaPresionado)
        {
            estaPresionado = true;
            posicionObjetivo = posicionOriginal + direccionHundimiento; // Se va hacia el fondo
            onClick.Invoke(); // Ejecuta la acción (ej. cambiar de escena)
        }
    }

    public void SoltarBoton()
    {
        if (estaPresionado)
        {
            estaPresionado = false;
            posicionObjetivo = posicionOriginal; // Regresa al frente
        }
    }
}