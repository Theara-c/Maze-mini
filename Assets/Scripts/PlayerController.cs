using UnityEngine;

// Rigidbody first-person movement with sprint (Shift) and jump (Space).
// Created automatically by MazeFrame.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;       // the maze cells are only 1 unit wide, so keep this small
    public float sprintMultiplier = 1.6f; // hold Shift: moveSpeed x this
    public float acceleration = 15f;
    [Range(0f, 1f)]
    public float airControl = 0.5f;      // share of acceleration you keep while in the air

    [Header("Jump")]
    [Tooltip("Height in world units. Walls are 1 high, so keep this under about 0.6 or the player " +
             "can see over (or climb onto) the walls.")]
    public float jumpHeight = 0.35f;
    [Tooltip("Can still jump this long after walking off an edge.")]
    public float coyoteTime = 0.1f;
    [Tooltip("A jump pressed this long before landing still counts.")]
    public float jumpBuffer = 0.1f;

    Rigidbody rb;
    Vector2 moveInput;
    bool sprinting;
    float yaw;

    bool touchingGround;                 // filled in by the collision callbacks each physics step
    float lastGroundedTime = -10f;
    float lastJumpPressTime = -10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.sleepThreshold = 0f;          // a sleeping Rigidbody stops reporting ground contact, which would block jumping
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

        if (playing)
        {
            moveInput = GameInput.Move();
            sprinting = GameInput.SprintHeld() && moveInput.sqrMagnitude > 0.01f;
            if (GameInput.JumpPressed()) lastJumpPressTime = Time.time;
        }
        else
        {
            moveInput = Vector2.zero;
            sprinting = false;
        }
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        if (touchingGround) lastGroundedTime = Time.time;

        // ---------- Horizontal movement ----------
        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 desired = Quaternion.Euler(0f, yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y) * speed;
        Vector3 current = rb.GetPointVelocity(rb.position);

        Vector3 change = desired - current;
        change.y = 0f; // never touch vertical speed here, so gravity and falling stay natural
        float accel = acceleration * (touchingGround ? 1f : airControl);
        change = Vector3.ClampMagnitude(change, accel * Time.fixedDeltaTime);

        rb.AddForce(change, ForceMode.VelocityChange);

        // ---------- Jump ----------
        bool wantsJump = Time.time - lastJumpPressTime <= jumpBuffer;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;

        if (wantsJump && canJump)
        {
            float jumpSpeed = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
            rb.AddForce(Vector3.up * (jumpSpeed - current.y), ForceMode.VelocityChange);

            // use up both timers so one key press can never jump twice
            lastJumpPressTime = -10f;
            lastGroundedTime = -10f;
        }

        touchingGround = false;   // the next physics step sets it again if we are still on the ground
    }

    // ---------- Ground detection ----------

    void OnCollisionEnter(Collision collision) { CheckGround(collision); }
    void OnCollisionStay(Collision collision) { CheckGround(collision); }

    void CheckGround(Collision collision)
    {
        // a contact whose surface faces mostly upward is floor; walls face sideways and are ignored
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.6f)
            {
                touchingGround = true;
                return;
            }
        }
    }
}