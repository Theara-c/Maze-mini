
using UnityEngine;

public class Door : MonoBehaviour
{
    public float interactDistance = 2f;
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private bool isOpen = false;
    private bool isOpening = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        if (isOpen || isOpening)
        {
            if (isOpening)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    openRotation,
                    Time.deltaTime * openSpeed
                );

                if (Quaternion.Angle(transform.rotation, openRotation) < 1f)
                {
                    transform.rotation = openRotation;
                    isOpening = false;
                    isOpen = true;
                }
            }

            return;
        }

        if (GameManager.Instance == null)
            return;

        Transform player = GameManager.Instance.player;

        if (player == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance <= interactDistance && GameInput.InteractPressed())
        {
            if (GameManager.Instance.hasKey)
            {
                GameManager.Instance.ShowHint("Door unlocked!");
                isOpening = true;
            }
            else
            {
                GameManager.Instance.ShowHint("You need a key! Find the coin first.");
            }
        }
    }
}
