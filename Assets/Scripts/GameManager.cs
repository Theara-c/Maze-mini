using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Game state + win/lose logic. Keeps the same public API your other scripts already use
// (Instance, player, fallY, hasKey, IsPlaying, CollectKey, ShowHint, Win, Lose, Restart).
// The screen UI is drawn by UIManager, which listens to the static events below.
// Created automatically by MazeFrame if the scene does not already have one.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ---- Events the UI listens to (no direct reference to the UI needed) ----
    public static event Action<string> OnHintChanged;       // hint text ("" = hide)
    public static event Action<bool> OnKeyChanged;          // hasKey
    public static event Action<int> OnCoinsChanged;         // coins (= lives)
    public static event Action<bool> OnMenuChanged;         // main menu visible?
    public static event Action<bool> OnPauseChanged;        // paused?
    public static event Action<bool, string> OnGameEnded;   // (isWin, message)

    [Header("Settings")]
    public float fallY = -30f;          // safety net: lose if the player somehow drops below this
    public Transform player;
    public bool showKeyStatus = false;  // turn on once you add a key to the game
    public bool useMainMenu = true;     // show a Play / Quit screen when the scene starts

    [Header("Coins = Lives")]
    public int startingCoins = 0;       // 0 = the first ghost touch ends the game unless you picked up a coin
    public float hitCooldown = 2f;      // seconds of protection after a ghost takes a coin

    public int coins { get; private set; }
    float invulnerableUntil;

    public bool hasKey { get; private set; }
    public bool IsPlaying { get; private set; }   // false while in menu, paused, or after the game ends
    public bool IsPaused { get; private set; }
    public bool InMenu { get; private set; }

    const string StartHint = "Find the way out. The exit is a pit at the far corner: walk off the ledge!";

    static bool s_skipMenuOnce;         // Restart skips the main menu; "Main Menu" button does not
    bool ended;
    string hint = "";
    float hintClearTime;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        coins = startingCoins;

        InMenu = useMainMenu && !s_skipMenuOnce;
        s_skipMenuOnce = false;
        IsPlaying = !InMenu;

        // build the on-screen UI if the scene does not have one
        if (FindFirstObjectByType<UIManager>() == null)
            gameObject.AddComponent<UIManager>();
    }

    void Start()
    {
        SetCursor(IsPlaying);

        OnKeyChanged?.Invoke(hasKey);
        OnCoinsChanged?.Invoke(coins);
        OnMenuChanged?.Invoke(InMenu);

        if (!InMenu) ShowHint(StartHint, 10f);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;   // never leave the editor frozen
        }
    }

    void Update()
    {
        if (hint != "" && Time.time > hintClearTime)
        {
            hint = "";
            OnHintChanged?.Invoke("");
        }

        if (InMenu) return;

        if (GameInput.RestartPressed()) { Restart(); return; }
        if (GameInput.PausePressed()) TogglePause();

        if (!IsPlaying) return;

        if (player != null && player.position.y < fallY)
            Lose("You fell off the map!");
    }

    // ---------- Menu / pause ----------

    public void StartGame()
    {
        if (!InMenu) return;
        InMenu = false;
        IsPlaying = true;
        SetCursor(true);
        OnMenuChanged?.Invoke(false);
        ShowHint(StartHint, 10f);
    }

    public void TogglePause()
    {
        if (InMenu || ended) return;

        IsPaused = !IsPaused;
        IsPlaying = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        SetCursor(!IsPaused);
        OnPauseChanged?.Invoke(IsPaused);
    }

    public void Resume() { if (IsPaused) TogglePause(); }

    // ---------- Gameplay ----------

    public void CollectKey()
    {
        hasKey = true;
        OnKeyChanged?.Invoke(true);
        ShowHint("You got the key!", 3f);
    }

    public void AddCoin(int amount = 1)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
        ShowHint("+1 coin! (" + coins + ")", 2f);
    }

    // Called by a ghost when it touches the player: costs one coin, or ends the game at 0 coins.
    // Returns true if the ghost used up a coin (the ghost should then destroy itself).
    public bool HitByGhost()
    {
        if (!IsPlaying || ended) return false;
        if (Time.time < invulnerableUntil) return false;   // just got hit, give the player a moment to run

        if (coins <= 0)
        {
            Lose("A ghost caught you!");
            return false;
        }

        coins--;
        invulnerableUntil = Time.time + hitCooldown;
        OnCoinsChanged?.Invoke(coins);
        ShowHint("A ghost took a coin!  Coins left: " + coins, 2.5f);
        return true;
    }

    public void ShowHint(string message, float seconds = 2f)
    {
        hint = message;
        hintClearTime = Time.time + seconds;
        OnHintChanged?.Invoke(hint);
    }

    public void Win(string message = "YOU ESCAPED!")
    {
        EndGame(message, true);
    }

    public void Lose(string reason)
    {
        EndGame("GAME OVER\n" + reason, false);
    }

    void EndGame(string message, bool win)
    {
        if (ended || InMenu || IsPaused) return;   // stops win and lose from both firing
        ended = true;
        IsPlaying = false;

        // free the mouse so the buttons can be clicked
        SetCursor(false);
        OnGameEnded?.Invoke(win, message);
    }

    // ---------- Scene control ----------

    public void Restart()
    {
        Time.timeScale = 1f;
        s_skipMenuOnce = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        s_skipMenuOnce = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}