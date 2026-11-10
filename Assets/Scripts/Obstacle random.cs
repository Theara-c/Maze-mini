using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Obstaclerandom : MonoBehaviour
{
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private int _mazeWidth;
    [SerializeField] private int _mazeDepth;

    [Header("Game Prefabs")]
    [SerializeField] private GameObject _treasureChestPrefab; // Contains key or coins
    [SerializeField] private GameObject _doorPrefab;
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private GameObject _ghostPrefab;

    [Header("Spawn Counts")]
    [SerializeField] private int _coinChestCount = 5;
    [SerializeField] private int _trapCount = 4;
    [SerializeField] private int _ghostCount = 2;
    [Range(1, 3)] [SerializeField] private int _doorCount = 1;

    private MazeCell[,] _mazeGrid;

    void Start()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }

        GenerateMaze(null, _mazeGrid[0, 0]);

        // Open an outer wall so there's an exit route
        OpenOuterExit();

        // Once maze is built, spawn all random elements
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
        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < _mazeWidth)
        {
            var cellToRight = _mazeGrid[x + 1, z];
            if (!cellToRight.IsVisited) yield return cellToRight;
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];
            if (!cellToLeft.IsVisited) yield return cellToLeft;
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];
            if (!cellToFront.IsVisited) yield return cellToFront;
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];
            if (!cellToBack.IsVisited) yield return cellToBack;
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null) return;

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

    // --- OPEN OUTER EXIT ---
    private void OpenOuterExit()
    {
        // Opens the outer right wall of the top-right corner cell so the player can exit
        if (_mazeGrid != null)
        {
            _mazeGrid[_mazeWidth - 1, _mazeDepth - 1].ClearRightWall();
        }
    }

    // --- RANDOM SPAWNING LOGIC ---
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
                availableCells.Add(_mazeGrid[x, z]);
            }
        }

        // Shuffle the list randomly
        availableCells = availableCells.OrderBy(_ => Random.value).ToList();

        int currentIndex = 0;

        // 1. Spawn Doors (1 to 3)
        for (int i = 0; i < _doorCount && currentIndex < availableCells.Count; i++)
        {
            Instantiate(_doorPrefab, availableCells[currentIndex].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            currentIndex++;
        }

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
            currentIndex++;
        }

        // 4. Spawn Traps
        for (int i = 0; i < _trapCount && currentIndex < availableCells.Count; i++)
        {
            Instantiate(_trapPrefab, availableCells[currentIndex].transform.position + Vector3.up * 0.1f, Quaternion.identity);
            currentIndex++;
        }

        // 5. Spawn Ghosts
        for (int i = 0; i < _ghostCount && currentIndex < availableCells.Count; i++)
        {
            Instantiate(_ghostPrefab, availableCells[currentIndex].transform.position + Vector3.up * 0.5f, Quaternion.identity);
            currentIndex++;
        }
    }
}