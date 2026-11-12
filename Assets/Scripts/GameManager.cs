using UnityEngine;
using UnityEngine.SceneManagement;

// Self-contained: draws its own UI with OnGUI, so no Canvas or TextMeshPro setup is needed.
// Created automatically by MazeFrame if the scene does not have one.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float fallY = -30f;          // safety net: lose if the player somehow drops below this
    public Transform player;
    public bool showKeyStatus = false;  // turn on once you add a key to the game

    public bool hasKey { get; private set; }
    public bool IsPlaying { get; private set; }

    string hint = "";
    float hintClearTime;
    string endMessage = "";
    bool endIsWin;

    GUIStyle hintStyle, titleStyle, smallStyle, buttonStyle;

    void Awake()
    {
        Instance = this;
        IsPlaying = true;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ShowHint("Find the way out. The exit is a pit at the far corner: walk off the ledge!", 10f);
    }

    void Update()
    {
        if (GameInput.RestartPressed()) Restart();

        if (hint != "" && Time.time > hintClearTime) hint = "";

        if (!IsPlaying) return;

        if (player != null && player.position.y < fallY)
            Lose("You fell off the map!");
    }

    public void CollectKey()
    {
        hasKey = true;
        ShowHint("You got the key!", 3f);
    }

    public void ShowHint(string message, float seconds = 2f)
    {
        hint = message;
        hintClearTime = Time.time + seconds;
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
        if (!IsPlaying) return;   // stops win and lose from both firing
        IsPlaying = false;
        endMessage = message;
        endIsWin = win;

        // free the mouse so the Restart button can be clicked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void MakeStyles()
    {
        if (hintStyle != null) return;

        int basic = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.03f));

        hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = basic, fontStyle = FontStyle.Bold };
        hintStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = basic * 2, fontStyle = FontStyle.Bold, wordWrap = true };

        smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerLeft, fontSize = Mathf.RoundToInt(basic * 0.7f) };
        smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = basic };
    }

    void OnGUI()
    {
        MakeStyles();

        if (hint != "")
            GUI.Label(new Rect(0, Screen.height * 0.05f, Screen.width, Screen.height * 0.1f), hint, hintStyle);

        if (showKeyStatus)
            GUI.Label(new Rect(15, 10, 300, 30), "Key: " + (hasKey ? "Yes" : "No"), hintStyle);

        GUI.Label(new Rect(15, 0, 400, Screen.height - 10), "WASD: move   Mouse: look   R: restart", smallStyle);

        if (!IsPlaying)
        {
            float w = Mathf.Min(Screen.width * 0.7f, 640f);
            float h = Mathf.Min(Screen.height * 0.6f, 380f);
            Rect box = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            GUI.Box(box, GUIContent.none);

            titleStyle.normal.textColor = endIsWin ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(box.x, box.y + 10, w, h - 110), endMessage, titleStyle);

            if (GUI.Button(new Rect(box.x + w / 2f - 120, box.yMax - 90, 240, 60), "Restart (R)", buttonStyle))
                Restart();
        }
    }
}