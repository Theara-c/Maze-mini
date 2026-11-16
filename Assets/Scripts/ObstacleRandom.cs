
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Obstaclerandom : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private int _mazeWidth;
    [SerializeField] private int _mazeDepth;

    [Header("First Person / Exit")]
    [SerializeField] private Material _floorMaterial;

    [Header("Game Prefabs")]
    [SerializeField] private GameObject _treasureChestPrefab;
    [SerializeField] private GameObject _doorPrefab;
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private GameObject _ghostPrefab;

    [Header("Spawn Counts")]
    [SerializeField] private int _coinChestCount = 5;
    [SerializeField] private int _trapCount = 15;
    [SerializeField] private int _ghostCount = 2;
    [Range(1, 25)]
    [SerializeField] private int _doorCount = 1;

    private MazeCell[,] _mazeGrid;

    public MazeCell[,] MazeGrid => _mazeGrid;

    public Transform Player => GameManager.Instance != null
        ? GameManager.Instance.player
        : null;

    void Awake()
    {
        var duplicate = GetComponent<MazeGenerator>();

        if (duplicate != null)
        {
            duplicate.enabled = false;
        }
    }

    void Start()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        // Create maze cells
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(
                    _mazeCellPrefab,
                    new Vector3(x, 0, z),
                    Quaternion.identity
                );
            }
        }

        // Generate maze
        GenerateMaze(null, _mazeGrid[0, 0]);

        // Build floor, entrance, exit and player
        MazeFrame.Build(
            _mazeGrid,
            _mazeWidth,
            _mazeDepth,
            _mazeCellPrefab,
            _floorMaterial
        );

        // Spawn game objects
        SpawnGameElements();
    }

    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();

        ClearWalls(previousCell, currentCell);

        MazeCell nextCell;

        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }

        } while (nextCell != null);
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        return unvisitedCells
            .OrderBy(_ => Random.Range(1, 10))
            .FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < _mazeWidth)
        {
            var cellToRight = _mazeGrid[x + 1, z];

            if (!cellToRight.IsVisited)
                yield return cellToRight;
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];

            if (!cellToLeft.IsVisited)
                yield return cellToLeft;
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];

            if (!cellToFront.IsVisited)
                yield return cellToFront;
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];

            if (!cellToBack.IsVisited)
                yield return cellToBack;
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
            return;

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
        }
        else if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
        }
        else if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
        }
        else if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
        }
    }

    // --------------------------------
    // RANDOM SPAWNING
    // --------------------------------

    private void SpawnGameElements()
    {
        // Create available cells
        List<MazeCell> availableCells = new List<MazeCell>();

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                // Skip player starting cell
                if (x == 0 && z == 0)
                    continue;

                // Skip exit cell
                if (x == _mazeWidth - 1 && z == _mazeDepth - 1)
                    continue;

                availableCells.Add(_mazeGrid[x, z]);
            }
        }

        // Shuffle cells
        availableCells = availableCells
            .OrderBy(_ => Random.value)
            .ToList();

        int currentIndex = 0;

        // 1. Spawn doors in maze openings
        SpawnDoors();

        // 2. Spawn coin chests
        for (int i = 0;
             i < _coinChestCount && currentIndex < availableCells.Count;
             i++)
        {
            Instantiate(
                _treasureChestPrefab,
                availableCells[currentIndex].transform.position
                    + Vector3.up * 0.5f,
                Quaternion.identity
            );

            currentIndex++;
        }

        // 3. Spawn traps
        for (int i = 0;
             i < _trapCount && currentIndex < availableCells.Count;
             i++)
        {
            Instantiate(
                _trapPrefab,
                availableCells[currentIndex].transform.position
                    + Vector3.up * 0.1f,
                Quaternion.identity
            );

            currentIndex++;
        }

        // 4. Spawn ghosts
        for (int i = 0;
             i < _ghostCount && currentIndex < availableCells.Count;
             i++)
        {
            GameObject ghost = Instantiate(
                _ghostPrefab,
                availableCells[currentIndex].transform.position
                    + Vector3.up * 0.5f,
                Quaternion.identity
            );

            GhostScript ghostScript = ghost.GetComponent<GhostScript>();

            if (ghostScript != null && GameManager.Instance != null)
            {
                ghostScript.Initialize(
                    _mazeGrid,
                    GameManager.Instance.player
                );
            }

            currentIndex++;
        }
    }

    // --------------------------------
    // DOOR SPAWNING
    // --------------------------------

private void SpawnDoors()
{
    List<System.Action> doorPlacements = new List<System.Action>();

    for (int x = 0; x < _mazeWidth; x++)
    {
        for (int z = 0; z < _mazeDepth; z++)
        {
            MazeCell cell = _mazeGrid[x, z];

            // Right-side opening
            if (x + 1 < _mazeWidth && !cell.HasRightWall())
            {
                int cellX = x;
                int cellZ = z;

                doorPlacements.Add(() =>
                {
                    Vector3 position = _mazeGrid[cellX, cellZ].transform.position
                        + new Vector3(0.5f, 0.5f, 0f);

                    Instantiate(
                        _doorPrefab,
                        position,
                        Quaternion.Euler(0f, 90f, 0f)
                    );
                });
            }

            // Front-side opening
            if (z + 1 < _mazeDepth && !cell.HasFrontWall())
            {
                int cellX = x;
                int cellZ = z;

                doorPlacements.Add(() =>
                {
                    Vector3 position = _mazeGrid[cellX, cellZ].transform.position
                        + new Vector3(0f, 0.5f, 0.5f);

                    Instantiate(
                        _doorPrefab,
                        position,
                        Quaternion.identity
                    );
                });
            }
        }
    }

    // Randomize door locations
    doorPlacements = doorPlacements
        .OrderBy(_ => Random.value)
        .ToList();

    int doorsSpawned = Mathf.Min(_doorCount, doorPlacements.Count);

    for (int i = 0; i < doorsSpawned; i++)
    {
        doorPlacements[i].Invoke();
    }

    Debug.Log("Doors spawned: " + doorsSpawned);
}

}
