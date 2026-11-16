using UnityEngine;

// Sits on the camera (a child of the Player). Created automatically by MazeFrame.
public class PlayerLook : MonoBehaviour
{
    public Transform playerBody;
    public float sensitivity = 1f;
    public float maxPitch = 80f;

    PlayerController controller;
    float pitch;

    void Start()
    {
        controller = playerBody.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        Vector2 look = GameInput.Look(sensitivity);

        // turn the whole player left/right
        controller.AddYaw(look.x);

        // tilt only the camera up/down
        pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}