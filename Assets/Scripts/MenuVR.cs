using UnityEngine;

public class MenuVR : MonoBehaviour
{
    public float distancia    = 1.5f;
    public float alturaOffset = -0.1f;
    public float suavidad     = 3f;

    private Transform _camara;

    void Start()
    {
        _camara = GameObject.Find("CenterEyeAnchor").transform;
        SnapAlFrente();
    }

    void Update()
    {
        Vector3 destino = _camara.position
                        + _camara.forward * distancia
                        + Vector3.up * alturaOffset;

        transform.position = Vector3.Lerp(
            transform.position, destino, Time.deltaTime * suavidad);

        Quaternion rot = Quaternion.LookRotation(
            transform.position - _camara.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, rot, Time.deltaTime * suavidad);
    }

    void SnapAlFrente()
    {
        transform.position = _camara.position
                           + _camara.forward * distancia
                           + Vector3.up * alturaOffset;
        transform.LookAt(_camara.position);
        transform.Rotate(0, 180, 0);
    }
}