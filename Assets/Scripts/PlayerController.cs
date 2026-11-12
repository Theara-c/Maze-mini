using UnityEngine;

// Rigidbody first-person movement. Created automatically by MazeFrame.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;      // the maze cells are only 1 unit wide, so keep this small
    public float acceleration = 15f;

    Rigidbody rb;
    Vector2 moveInput;
    float yaw;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        yaw = transform.eulerAngles.y;
    }

    // called by PlayerLook
    public void AddYaw(float degrees)
    {
        yaw += degrees;
    }

    void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        moveInput = playing ? GameInput.Move() : Vector2.zero;
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        Vector3 desired = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        Vector3 current = rb.GetPointVelocity(rb.position);

        Vector3 change = desired - current;
        change.y = 0f; // never touch vertical speed, so gravity and falling stay natural
        change = Vector3.ClampMagnitude(change, acceleration * Time.fixedDeltaTime);

        rb.AddForce(change, ForceMode.VelocityChange);
    }
}