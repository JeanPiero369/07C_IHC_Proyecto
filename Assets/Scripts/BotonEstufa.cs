using UnityEngine;
using Oculus.Interaction;

public class BotonEstufa : MonoBehaviour
{
    public GameObject fuegoEstufa;
    private bool encendido = false;

    void Start()
    {
        GetComponent<PokeInteractable>().WhenStateChanged += OnStateChanged;
        Debug.Log("Boton listo");
    }

    private void OnStateChanged(InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            encendido = !encendido;
            fuegoEstufa.SetActive(encendido);
            Debug.Log("Fuego: " + encendido);
        }
    }
}