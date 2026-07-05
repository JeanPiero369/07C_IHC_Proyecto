using UnityEngine;

public class AbrirPuerta : MonoBehaviour
{
    public enum TipoPuerta
    {
        GiroHorizontal,
        GiroVertical,
        DeslizarDerecha,
        DeslizarIzquierda,
        DeslizarArriba,
        DeslizarAbajo
    }

    public enum LadoBisagra
    {
        Izquierda,
        Derecha
    }

    [Header("Tipo de apertura")]
    public TipoPuerta tipoPuerta = TipoPuerta.GiroHorizontal;

    [Header("Configuracion de Giro")]
    public LadoBisagra ladoBisagra = LadoBisagra.Izquierda;
    public float gradosGiro = 90f;
    public float velocidadGiro = 2f;

    [Header("Configuracion de Deslizamiento")]
    public float distanciaDesliz = 2f;
    public float velocidadDesliz = 2f;

    private bool jugadorCerca = false;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    void Update()
    {
        if (tipoPuerta == TipoPuerta.GiroHorizontal ||
            tipoPuerta == TipoPuerta.GiroVertical)
        {
            ManejarGiro();
        }
        else
        {
            ManejarDeslizamiento();
        }
    }

    void ManejarGiro()
    {
        Vector3 eje = tipoPuerta == TipoPuerta.GiroHorizontal
                      ? Vector3.up
                      : Vector3.right;

        // El lado de la bisagra define el signo del giro
        float signo = ladoBisagra == LadoBisagra.Izquierda ? 1f : -1f;

        Quaternion rotObjetivo = jugadorCerca
            ? rotacionInicial * Quaternion.AngleAxis(signo * gradosGiro, eje)
            : rotacionInicial;

        transform.rotation = Quaternion.Lerp(
            transform.rotation, rotObjetivo, Time.deltaTime * velocidadGiro);
    }

    void ManejarDeslizamiento()
    {
        Vector3 direccion = Vector3.zero;

        switch (tipoPuerta)
        {
            case TipoPuerta.DeslizarDerecha:   direccion = Vector3.right; break;
            case TipoPuerta.DeslizarIzquierda: direccion = Vector3.left;  break;
            case TipoPuerta.DeslizarArriba:    direccion = Vector3.up;    break;
            case TipoPuerta.DeslizarAbajo:     direccion = Vector3.down;  break;
        }

        Vector3 posObjetivo = jugadorCerca
            ? posicionInicial + direccion * distanciaDesliz
            : posicionInicial;

        transform.position = Vector3.Lerp(
            transform.position, posObjetivo, Time.deltaTime * velocidadDesliz);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            jugadorCerca = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            jugadorCerca = false;
    }
}