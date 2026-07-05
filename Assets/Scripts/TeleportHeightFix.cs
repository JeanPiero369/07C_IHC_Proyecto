using UnityEngine;

public class TeleportHeightFix : MonoBehaviour
{
    private float floorY;

    void Start()
    {
        Invoke(nameof(CaptureFloor), 0.5f);
    }

    void CaptureFloor()
    {
        floorY = transform.position.y- 0.5f;
    }

    void Update()
    {
        if (transform.position.y < floorY)
        {
            Vector3 p = transform.position;
            p.y = floorY;
            transform.position = p;
        }
    }
}