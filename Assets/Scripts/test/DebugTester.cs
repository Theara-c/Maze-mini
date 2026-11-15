using UnityEngine;

/// <summary>
/// TEMPORARY test helper. Fakes the gameplay events with the keyboard so you can test
/// GameManager and the UI without a player, key, door or maze. Delete before the final merge.
/// </summary>
public class DebugTester : MonoBehaviour
{
    private void OnEnable()  => GameEvents.OnGameStateChanged += LogState;
    private void OnDisable() => GameEvents.OnGameStateChanged -= LogState;

    private void LogState(GameState s) => Debug.Log($"[DebugTester] OnGameStateChanged: {s}");

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Fake gameplay events
        if (Input.GetKeyDown(KeyCode.M)) GameEvents.MazeReady();
        if (Input.GetKeyDown(KeyCode.K)) GameEvents.KeyCollected();
        if (Input.GetKeyDown(KeyCode.G)) GameEvents.GoalReached();
        if (Input.GetKeyDown(KeyCode.H)) GameEvents.PlayerCaught();

        // Fake the UI buttons
        if (Input.GetKeyDown(KeyCode.Return)) gm.StartGame();
        if (Input.GetKeyDown(KeyCode.R))      gm.RestartLevel();
        if (Input.GetKeyDown(KeyCode.N))      gm.NextLevel();
        if (Input.GetKeyDown(KeyCode.Q))      gm.QuitToMenu();
        // Escape (pause / resume) is handled by GameManager itself.
    }

    private void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 360, 220), GUI.skin.box);
        GUILayout.Label($"State: {gm.State}   HasKey: {gm.HasKey}   Level: {gm.Level}");
        GUILayout.Label("Enter = Start    Esc = Pause/Resume\n" +
                        "M = MazeReady    K = KeyCollected\n" +
                        "G = GoalReached  H = PlayerCaught\n" +
                        "R = Restart      N = NextLevel    Q = QuitToMenu");
        GUILayout.EndArea();
    }
}
