
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GhostScript : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float moveSpeed = 2f;
    public float catchDistance = 0.8f;
    public float pathUpdateInterval = 0.3f;

    private CharacterController controller;
    private MazeCell[,] mazeGrid;
    private Transform player;

    private List<Vector2Int> currentPath = new List<Vector2Int>();

    private float nextPathUpdate;
    private float verticalVelocity;

    private bool initializationWarningShown;
    private bool pathWarningShown;

    // Receive maze grid and player from Obstaclerandom
    public void Initialize(MazeCell[,] grid, Transform target)
    {
        mazeGrid = grid;
        player = target;

        Debug.Log("Ghost initialized!");
        Debug.Log("Maze grid assigned: " + (mazeGrid != null));
        Debug.Log("Player assigned: " + (player != null));
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        // Check player reference
        if (player == null)
        {
            if (!initializationWarningShown)
            {
                Debug.LogWarning("Ghost has no player reference!");
                initializationWarningShown = true;
            }

            return;
        }

        // Check whether ghost catches player
        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= catchDistance)
        {
            GameManager.Instance.Lose("A ghost caught you!");
            return;
        }

        // Check maze reference
        if (mazeGrid == null)
        {
            if (!initializationWarningShown)
            {
                Debug.LogWarning("Ghost has no maze grid!");
                initializationWarningShown = true;
            }

            return;
        }

        // Recalculate path regularly
        if (Time.time >= nextPathUpdate)
        {
            FindPath();
            nextPathUpdate = Time.time + pathUpdateInterval;
        }

        // Follow the calculated path
        FollowPath();

        // Apply gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= 9.81f * Time.deltaTime;
        }

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );
    }

    private void FindPath()
    {
        int width = mazeGrid.GetLength(0);
        int depth = mazeGrid.GetLength(1);

        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        Vector2Int target = new Vector2Int(
            Mathf.RoundToInt(player.position.x),
            Mathf.RoundToInt(player.position.z)
        );

        currentPath.Clear();

        // Check grid boundaries
        if (!InsideGrid(start, width, depth) ||
            !InsideGrid(target, width, depth))
        {
            return;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> previous =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        previous[start] = start;

        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        // BFS pathfinding
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == target)
                break;

            MazeCell cell = mazeGrid[current.x, current.y];

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (!InsideGrid(next, width, depth))
                    continue;

                if (previous.ContainsKey(next))
                    continue;

                bool wallBlocked =
                    (direction == Vector2Int.right &&
                     cell.HasRightWall()) ||

                    (direction == Vector2Int.left &&
                     cell.HasLeftWall()) ||

                    (direction == Vector2Int.up &&
                     cell.HasFrontWall()) ||

                    (direction == Vector2Int.down &&
                     cell.HasBackWall());

                if (wallBlocked)
                    continue;

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        // No route found
        if (!previous.ContainsKey(target))
        {
            if (!pathWarningShown)
            {
                Debug.LogWarning(
                    "Ghost cannot find a path to the player!"
                );

                pathWarningShown = true;
            }

            return;
        }

        pathWarningShown = false;

        // Rebuild path
        Vector2Int step = target;

        while (step != start)
        {
            currentPath.Add(step);
            step = previous[step];
        }

        currentPath.Reverse();

        Debug.Log("Ghost found path. Steps: " + currentPath.Count);
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0)
            return;

        Vector2Int nextCell = currentPath[0];

        Vector3 destination = new Vector3(
            nextCell.x,
            transform.position.y,
            nextCell.y
        );

        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.magnitude < 0.08f)
        {
            currentPath.RemoveAt(0);
            return;
        }

        direction.Normalize();

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool InsideGrid(
        Vector2Int position,
        int width,
        int depth)
    {
        return position.x >= 0 &&
               position.x < width &&
               position.y >= 0 &&
               position.y < depth;
    }
}
