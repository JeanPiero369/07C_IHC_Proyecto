using UnityEngine;

/// <summary>
/// Editor/PC helper to test RF-03 pouring without VR headset/controllers.
/// Attach this to a grabbable container that already has LiquidPourController.
/// Use Play Mode, move/rotate it with keyboard, and watch LiquidPourController transfer liquid.
/// Remove or disable it for the final VR build if not needed.
/// </summary>
[DisallowMultipleComponent]
public class LiquidPourKeyboardTester : MonoBehaviour
{
    [Header("Keyboard Test Controls")]
    [SerializeField] private bool enableKeyboardTest = true;
    [SerializeField] private bool makeRigidbodyKinematicInKeyboardTest = true;
    [SerializeField] private float moveSpeed = 0.6f;
    [SerializeField] private float rotationSpeed = 70f;
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody cachedRigidbody;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        cachedRigidbody = GetComponent<Rigidbody>();
        if (enableKeyboardTest && makeRigidbodyKinematicInKeyboardTest && cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }
    }

    private void Update()
    {
        if (!enableKeyboardTest)
        {
            return;
        }

        float dt = Time.deltaTime;
        Vector3 worldMove = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) worldMove += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) worldMove += Vector3.back;
        if (Input.GetKey(KeyCode.A)) worldMove += Vector3.left;
        if (Input.GetKey(KeyCode.D)) worldMove += Vector3.right;
        if (Input.GetKey(KeyCode.E)) worldMove += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) worldMove += Vector3.down;

        Quaternion deltaRotation = Quaternion.identity;
        if (Input.GetKey(KeyCode.LeftArrow)) deltaRotation *= Quaternion.AngleAxis(rotationSpeed * dt, Vector3.forward);
        if (Input.GetKey(KeyCode.RightArrow)) deltaRotation *= Quaternion.AngleAxis(-rotationSpeed * dt, Vector3.forward);
        if (Input.GetKey(KeyCode.UpArrow)) deltaRotation *= Quaternion.AngleAxis(rotationSpeed * dt, Vector3.right);
        if (Input.GetKey(KeyCode.DownArrow)) deltaRotation *= Quaternion.AngleAxis(-rotationSpeed * dt, Vector3.right);
        if (Input.GetKey(KeyCode.Z)) deltaRotation *= Quaternion.AngleAxis(rotationSpeed * dt, Vector3.up);
        if (Input.GetKey(KeyCode.X)) deltaRotation *= Quaternion.AngleAxis(-rotationSpeed * dt, Vector3.up);

        Vector3 targetPosition = transform.position + worldMove * moveSpeed * dt;
        Quaternion targetRotation = deltaRotation * transform.rotation;

        if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
        {
            cachedRigidbody.MovePosition(targetPosition);
            cachedRigidbody.MoveRotation(targetRotation);
        }
        else
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        if (Input.GetKeyDown(resetKey))
        {
            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.position = initialPosition;
                cachedRigidbody.rotation = initialRotation;
            }
            else
            {
                transform.SetPositionAndRotation(initialPosition, initialRotation);
            }
        }
    }
}
