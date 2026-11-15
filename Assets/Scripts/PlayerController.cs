// using UnityEngine;

// // Rigidbody first-person movement. Created automatically by MazeFrame.
// [RequireComponent(typeof(Rigidbody))]
// public class PlayerController : MonoBehaviour
// {
//     public float moveSpeed = 1.5f;      // the maze cells are only 1 unit wide, so keep this small
//     public float acceleration = 15f;

//     Rigidbody rb;
//     Vector2 moveInput;
//     float yaw;

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody>();
//         rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
//         rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
//         rb.interpolation = RigidbodyInterpolation.Interpolate;
//         yaw = transform.eulerAngles.y;
//     }

//     // called by PlayerLook
//     public void AddYaw(float degrees)
//     {
//         yaw += degrees;
//     }

//     void Update()
//     {
//         bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
//         moveInput = playing ? GameInput.Move() : Vector2.zero;
//     }

//     void FixedUpdate()
//     {
//         rb.angularVelocity = Vector3.zero;
//         rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

//         Vector3 desired = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
//         Vector3 current = rb.GetPointVelocity(rb.position);

//         Vector3 change = desired - current;
//         change.y = 0f; // never touch vertical speed, so gravity and falling stay natural
//         change = Vector3.ClampMagnitude(change, acceleration * Time.fixedDeltaTime);

//         rb.AddForce(change, ForceMode.VelocityChange);
//     }
// }


// //rakrak


using UnityEngine;

// Rigidbody first-person movement. Created automatically by MazeFrame.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float acceleration = 15f;

    [Header("Jump Settings")]
    public float jumpForce = 2.5f;
    public float groundCheckDistance = 0.2f;

    private Rigidbody rb;
    private Collider playerCollider;
    private Vector2 moveInput;
    private float yaw;
    private bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        yaw = transform.eulerAngles.y;
    }

    // Called by PlayerLook
    public void AddYaw(float degrees)
    {
        yaw += degrees;
    }

    void Update()
    {
        bool playing =
            GameManager.Instance == null ||
            GameManager.Instance.IsPlaying;

        moveInput = playing ? GameInput.Move() : Vector2.zero;

        // Jump using Space
        if (playing && GameInput.JumpPressed())
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null &&
            !GameManager.Instance.IsPlaying)
        {
            moveInput = Vector2.zero;
            jumpRequested = false;
            return;
        }

        // Player rotation
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        // Walking movement
        Vector3 desired =
            Quaternion.Euler(0f, yaw, 0f) *
            new Vector3(moveInput.x, 0f, moveInput.y) *
            moveSpeed;

        Vector3 current = rb.linearVelocity;

        Vector3 change = desired - current;
        change.y = 0f;

        change = Vector3.ClampMagnitude(
            change,
            acceleration * Time.fixedDeltaTime
        );

        rb.AddForce(change, ForceMode.VelocityChange);

        // Jump
        if (jumpRequested)
        {
            if (IsGrounded())
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = jumpForce;
                rb.linearVelocity = velocity;
            }

            jumpRequested = false;
        }
    }

    // Check whether Player is touching the floor
    bool IsGrounded()
    {
        if (playerCollider == null)
            return false;

        Vector3 origin = new Vector3(
            playerCollider.bounds.center.x,
            playerCollider.bounds.min.y + 0.05f,
            playerCollider.bounds.center.z
        );

        return Physics.Raycast(
            origin,
            Vector3.down,
            groundCheckDistance + 0.1f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}
