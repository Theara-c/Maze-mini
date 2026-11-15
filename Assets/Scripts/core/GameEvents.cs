using System;

public enum GameState
{
    MainMenu,
    SpawningMaze,
    Navigating,
    Paused,
    LevelComplete,
    GameOver
}

/// <summary>
/// Shared event contract. Gameplay scripts call the Raise methods (KeyPickup, Door, Hazard,
/// MazeGenerator). GameManager and UIManager subscribe. Nobody references anybody else.
/// </summary>
public static class GameEvents
{
    public static event Action OnMazeReady;
    public static event Action OnKeyCollected;
    public static event Action OnGoalReached;
    public static event Action OnPlayerCaught;
    public static event Action<GameState> OnGameStateChanged;

    public static void MazeReady()              => OnMazeReady?.Invoke();
    public static void KeyCollected()           => OnKeyCollected?.Invoke();
    public static void GoalReached()            => OnGoalReached?.Invoke();
    public static void PlayerCaught()           => OnPlayerCaught?.Invoke();
    public static void StateChanged(GameState s) => OnGameStateChanged?.Invoke(s);
}
