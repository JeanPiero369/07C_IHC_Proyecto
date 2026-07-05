using UnityEngine;
using System.Collections;

public class FlowManager : MonoBehaviour
{
    [Header("Menus")]
    public GameObject menuPrincipal;
    public GameObject pantallaSeleccion;

    [Header("Laboratorio")]
    public GameObject laboratorioAssets;
    public GameObject laboratorioInteractables;
    public GameObject laboratorioStatics;

    [Header("Jugador y Spawn")]
    public Transform ovrCameraRig;
    public Transform spawnFrenteMesa;

    [Header("Menu frente al jugador")]
    public Transform menuCanvasTransform;
    public float distanciaAlFrente = 2.0f;

    private Transform _cameraTransform;

    void Start()
    {
        _cameraTransform = Camera.main != null ? Camera.main.transform : ovrCameraRig;

        // Solo mostrar menu
        menuPrincipal.SetActive(true);
        pantallaSeleccion.SetActive(false);

        // Ocultar grupos completos
        laboratorioInteractables.SetActive(false);
        laboratorioStatics.SetActive(false);

        // Activar assets pero ocultar todo EXCEPTO floor (1)
        laboratorioAssets.SetActive(true);
        foreach (Transform child in laboratorioAssets.transform)
        {
            if (child.name == "floor (1)") continue;
            child.gameObject.SetActive(false);
        }

        // Esperar a que OVR estabilice la posicion de la camara
        StartCoroutine(PosicionarCuandoListo());
    }

    IEnumerator PosicionarCuandoListo()
    {
        yield return new WaitForSeconds(0.3f);
        yield return null;
        PosicionarCanvasFrenteAlJugador();
    }

    void PosicionarCanvasFrenteAlJugador()
    {
        if (menuCanvasTransform == null || _cameraTransform == null)
            return;

        // Usar posicion de la camara (CenterEyeAnchor, ya con altura OVR ajustada)
        Vector3 pos = _cameraTransform.position;

        // Usar forward del RIG (fijo, no cambia con el movimiento de cabeza)
        Vector3 forward = ovrCameraRig.forward;
        forward.y = 0f;
        if (forward.magnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        // Canvas delante del jugador a la altura de sus ojos
        Vector3 targetPos = pos + forward * distanciaAlFrente;
        menuCanvasTransform.position = targetPos;

        // Canvas +Z apunta en la misma direccion que el jugador
        // Asi los botones (-Z local) miran hacia el jugador
        menuCanvasTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    public void IrAModoLibre()
    {
        menuPrincipal.SetActive(false);
        pantallaSeleccion.SetActive(false);

        foreach (Transform child in laboratorioAssets.transform)
            child.gameObject.SetActive(true);

        laboratorioInteractables.SetActive(true);
        laboratorioStatics.SetActive(true);

        if (spawnFrenteMesa != null && ovrCameraRig != null)
        {
            ovrCameraRig.SetPositionAndRotation(
                spawnFrenteMesa.position,
                spawnFrenteMesa.rotation
            );
        }
    }

    public void IniciarLaboratorio()
    {
        IrAModoLibre();
    }

    public void Salir()
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finish");
        }
#else
        Application.Quit();
#endif
    }
}
