
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
    [Tooltip("Ghosts spawn at least this many cells away from the player's start (0,0). " +
             "If the maze is too small for that, they use the farthest cells available.")]
    [SerializeField] private float _ghostMinSpawnDistance = 8f;
    
    [Range(1, 25)][SerializeField] private int _doorCount = 1;

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



  private void SpawnDoors()
{
    List<System.Action> doorPlacements =
        new List<System.Action>();

    for (int x = 0; x < _mazeWidth; x++)
    {
        for (int z = 0; z < _mazeDepth; z++)
        {
            MazeCell cell = _mazeGrid[x, z];

            // Avoid placing doors too close to the player start.
            if (x + z < 3)
                continue;

            // Right-side passage
            if (x + 1 < _mazeWidth && !cell.HasRightWall)
            {
                int cellX = x;
                int cellZ = z;

                doorPlacements.Add(() =>
                {
                    Vector3 position =
                        _mazeGrid[cellX, cellZ].transform.position
                        + new Vector3(0.5f, 0.5f, 0f);

                    Instantiate(
                        _doorPrefab,
                        position,
                        Quaternion.Euler(0f, 90f, 0f)
                    );
                });
            }

            // Front-side passage
            if (z + 1 < _mazeDepth && !cell.HasFrontWall)
            {
                int cellX = x;
                int cellZ = z;

                doorPlacements.Add(() =>
                {
                    Vector3 position =
                        _mazeGrid[cellX, cellZ].transform.position
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

    // Randomize available door positions.
    doorPlacements = doorPlacements
        .OrderBy(_ => Random.value)
        .ToList();

    int doorsSpawned = Mathf.Min(
        _doorCount,
        doorPlacements.Count
    );

    for (int i = 0; i < doorsSpawned; i++)
    {
        doorPlacements[i].Invoke();
    }

    Debug.Log("Doors spawned: " + doorsSpawned);
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
        // Create a list of available cells, excluding the starting position (0,0)
        List<MazeCell> availableCells = new List<MazeCell>();

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                // Skip the player's start corner (0,0) so they don't die or win instantly
                if (x == 0 && z == 0) continue;
                // CHANGED: also skip the exit cell so nothing blocks the way out
                if (x == _mazeWidth - 1 && z == _mazeDepth - 1) continue;
                availableCells.Add(_mazeGrid[x, z]);
            }
        }

        // Shuffle the list randomly
        availableCells = availableCells.OrderBy(_ => Random.value).ToList();

        int currentIndex = 0;

        // 1. Spawn Doors (1 to 3)
        SpawnDoors();

        // 2. Spawn the Key Chest (Only 1 chest contains the key)
        if (currentIndex < availableCells.Count)
        {
            GameObject keyChest = Instantiate(_treasureChestPrefab, availableCells[currentIndex].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            currentIndex++;
        }

        // 3. Spawn Coin Chests
        for (int i = 0; i < _coinChestCount && currentIndex < availableCells.Count; i++)
        {
            GameObject coinChest = Instantiate(_treasureChestPrefab, availableCells[currentIndex].transform.position + Vector3.up * 0.5f, Quaternion.identity);

            // coin chests give a coin (a life), not the door key
            var keyPickup = coinChest.GetComponent<TreasureChest>();
            if (keyPickup != null) Destroy(keyPickup);
            if (coinChest.GetComponent<Coin>() == null) coinChest.AddComponent<Coin>();

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
  
        // 5. Spawn Ghosts - far away from the player's start corner (0,0)
        var remaining = availableCells.GetRange(currentIndex, availableCells.Count - currentIndex);
        Vector3 playerStart = _mazeGrid[0, 0].transform.position;

        var far = remaining.Where(c => Vector3.Distance(c.transform.position, playerStart) >= _ghostMinSpawnDistance).ToList();
        if (far.Count < _ghostCount)
            far = remaining.OrderByDescending(c => Vector3.Distance(c.transform.position, playerStart))
                           .Take(_ghostCount).ToList();   // small maze: take the farthest cells instead

        for (int i = 0; i < _ghostCount && i < far.Count; i++)
        {
            var ghost = Instantiate(_ghostPrefab, far[i].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            if (ghost.GetComponent<GhostChase>() == null) ghost.AddComponent<GhostChase>();
        }
    }
}
    

    // --------------------------------
    // DOOR SPAWNING
    // --------------------------------


