using UnityEngine;

public class MenuLaboratorio : MonoBehaviour
{
    public GameObject estufaPrefab;
    private GameObject estufaInstancia;

    public void SpawnEstufa()
    {
        if (estufaInstancia == null)
        {
            // Aparece en la posición del prefab directamente
            estufaInstancia = Instantiate(estufaPrefab);
        }
        else
        {
            estufaInstancia.SetActive(!estufaInstancia.activeSelf);
        }
    }

    public void ResetearEscena()
    {
        if (estufaInstancia != null)
            Destroy(estufaInstancia);
    }
}