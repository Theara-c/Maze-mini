using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Owns the game state machine. Listens to gameplay events and decides state changes.
/// Every transition goes through SetState().
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Maze hookup")]
    [Tooltip("Wire MazeGenerator.Generate() here in the Inspector. If left empty, " +
             "OnMazeReady is raised automatically after a short delay (testing only).")]
    [SerializeField] private UnityEvent onGenerateMaze;
    [SerializeField] private float autoMazeReadyDelay = 0.5f;

    public GameState State { get; private set; } = GameState.MainMenu;
    public bool HasKey { get; private set; }
    public int Level { get; private set; } = 1;

    // ---------- Unity lifecycle ----------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnMazeReady    += HandleMazeReady;
        GameEvents.OnKeyCollected += HandleKeyCollected;
        GameEvents.OnGoalReached  += HandleGoalReached;
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
    }

    private void OnDisable()
    {
        GameEvents.OnMazeReady    -= HandleMazeReady;
        GameEvents.OnKeyCollected -= HandleKeyCollected;
        GameEvents.OnGoalReached  -= HandleGoalReached;
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f; // never leave the editor frozen
        }
    }

    private void Start() => SetState(GameState.MainMenu);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Navigating) PauseGame();
            else if (State == GameState.Paused) ResumeGame();
        }
    }

    // ---------- Public API (UI buttons call these) ----------

    public void StartGame()    { if (State == GameState.MainMenu)  SetState(GameState.SpawningMaze); }
    public void PauseGame()    { if (State == GameState.Navigating) SetState(GameState.Paused); }
    public void ResumeGame()   { if (State == GameState.Paused)     SetState(GameState.Navigating); }

    public void RestartLevel()
    {
        if (State == GameState.GameOver || State == GameState.Paused)
            SetState(GameState.SpawningMaze);
    }

    public void NextLevel()
    {
        if (State != GameState.LevelComplete) return;
        Level++;
        SetState(GameState.SpawningMaze);
    }

    public void QuitToMenu()
    {
        Level = 1;
        SetState(GameState.MainMenu);
    }

    // ---------- State machine ----------

    public void SetState(GameState newState)
    {
        CancelInvoke(nameof(AutoMazeReady));
        State = newState;

        if (newState == GameState.SpawningMaze)
            HasKey = false;

        // Freeze physics, Ghost and obstacles whenever the player is not actively navigating.
        Time.timeScale = (newState == GameState.Paused ||
                          newState == GameState.LevelComplete ||
                          newState == GameState.GameOver) ? 0f : 1f;

        // First-person cursor: locked while playing, free for menus.
        bool playing = newState == GameState.Navigating;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;

        Debug.Log($"[GameManager] State -> {newState}");
        GameEvents.StateChanged(newState); // raise before generating, so UI order stays correct

        if (newState == GameState.SpawningMaze)
            BeginMazeGeneration();
    }

    private void BeginMazeGeneration()
    {
        if (onGenerateMaze != null && onGenerateMaze.GetPersistentEventCount() > 0)
        {
            onGenerateMaze.Invoke(); // generator raises GameEvents.MazeReady() when finished
        }
        else
        {
            Debug.LogWarning("[GameManager] No maze generator wired. Auto-raising OnMazeReady (testing).");
            Invoke(nameof(AutoMazeReady), autoMazeReadyDelay);
        }
    }

    private void AutoMazeReady() => GameEvents.MazeReady();

    // ---------- Event handlers (guarded so stray events are ignored) ----------

    private void HandleMazeReady()
    {
        if (State == GameState.SpawningMaze) SetState(GameState.Navigating);
    }

    private void HandleKeyCollected()
    {
        if (State != GameState.Navigating) return;
        HasKey = true;
        Debug.Log("[GameManager] Key collected");
    }

    private void HandleGoalReached()
    {
        if (State != GameState.Navigating) return;
        if (!HasKey)
        {
            Debug.Log("[GameManager] Door reached without key, ignored");
            return;
        }
        SetState(GameState.LevelComplete);
    }

    private void HandlePlayerCaught()
    {
        if (State == GameState.Navigating) SetState(GameState.GameOver);
    }
}