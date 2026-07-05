using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Nombre de la escena del experimento guiado
    public string escenaExperimento = "EscenaExperimento";

    // Nombre de la escena del modo libre
    public string escenaModoLibre = "EscenaModoLibre";

    public void IniciarExperimento()
    {
        // Carga la escena del experimento
        SceneManager.LoadScene(escenaExperimento);
    }

    public void IniciarModoLibre()
    {
        // Carga la escena en modo libre
        SceneManager.LoadScene(escenaModoLibre);
    }

    public void AbrirAcercaDe()
    {
        // Aquí puedes activar un panel con la información del proyecto
        Debug.Log("Abriendo Acerca de...");
    }

    public void SalirDelJuego()
    {
        // Cierra la aplicación (solo funciona en el build final, no en el editor)
        Application.Quit();
        Debug.Log("El usuario ha salido del laboratorio.");
    }
}