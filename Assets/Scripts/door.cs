using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public float interactDistance = 2f;
    public float openAngle = 90f;
    public float openSpeed = 5f;

    private static HashSet<Door> allDoors = new HashSet<Door>();

    public bool IsOpen { get; private set; }

    private bool isMoving;
    private bool targetOpen;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    private void OnEnable()
    {
        allDoors.Add(this);
    }

    private void OnDisable()
    {
        allDoors.Remove(this);
    }

    private void Start()
    {
        // Door starts closed.
        closedRotation = transform.rotation;

        openRotation = closedRotation *
            Quaternion.Euler(0f, openAngle, 0f);

        targetRotation = closedRotation;

        IsOpen = false;
        targetOpen = false;
    }

    private void Update()
    {
        // Smoothly open or close the door.
        if (isMoving)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * openSpeed
            );

            if (Quaternion.Angle(
                transform.rotation,
                targetRotation
            ) < 1f)
            {
                transform.rotation = targetRotation;
                isMoving = false;
                IsOpen = targetOpen;
            }
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

        // Press E near the door.
        if (distance > interactDistance ||
            !GameInput.InteractPressed())
            return;

        if (isMoving)
            return;

        // Toggle open/closed.
        targetOpen = !IsOpen;

        targetRotation = targetOpen
            ? openRotation
            : closedRotation;

        isMoving = true;

        GameManager.Instance.ShowHint(
            targetOpen ? "Door opening..." : "Door closing!"
        );
    }

    // GhostScript uses this to detect closed doors.
    public static bool IsClosedBetween(
        Vector3 from,
        Vector3 to)
    {
        Vector3 midpoint = (from + to) / 2f;
        midpoint.y = 0f;

        foreach (Door door in allDoors)
        {
            if (door == null)
                continue;

            Vector3 doorPosition = door.transform.position;
            doorPosition.y = 0f;

            if (Vector3.Distance(
                doorPosition,
                midpoint
            ) < 0.4f)
            {
                return !door.IsOpen;
            }
        }

        return false;
    }
}