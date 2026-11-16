using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the whole Canvas UI from code (HUD, main menu, pause menu, win/lose panel)
// and updates it by listening to GameManager's events.
// Created automatically by GameManager. Needs the "Unity UI" (uGUI) package, which is
// installed by default in Unity's 3D templates.
public class UIManager : MonoBehaviour
{
    // ---- Easy to tweak ----
    static readonly Color BtnNormal  = new Color(0.20f, 0.22f, 0.32f, 1f);
    static readonly Color BtnHover   = new Color(0.32f, 0.36f, 0.55f, 1f);
    static readonly Color BtnPressed = new Color(0.12f, 0.13f, 0.20f, 1f);
    static readonly Color WinColor   = new Color(0.4f, 1f, 0.5f);
    static readonly Color LoseColor  = new Color(1f, 0.4f, 0.4f);

    Font font;
    Text hintText, keyText, coinText, helpText, endText;
    GameObject menuPanel, pausePanel, endPanel;

    // ---------- Lifecycle ----------

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
    }

    void OnEnable()
    {
        GameManager.OnHintChanged  += HandleHint;
        GameManager.OnKeyChanged   += HandleKey;
        GameManager.OnCoinsChanged += HandleCoins;
        GameManager.OnMenuChanged  += HandleMenu;
        GameManager.OnPauseChanged += HandlePause;
        GameManager.OnGameEnded    += HandleEnd;
    }

    void OnDisable()
    {
        GameManager.OnHintChanged  -= HandleHint;
        GameManager.OnKeyChanged   -= HandleKey;
        GameManager.OnCoinsChanged -= HandleCoins;
        GameManager.OnMenuChanged  -= HandleMenu;
        GameManager.OnPauseChanged -= HandlePause;
        GameManager.OnGameEnded    -= HandleEnd;
    }

    // ---------- Event handlers ----------

    void HandleHint(string text) { hintText.text = text; }

    void HandleKey(bool hasKey)
    {
        var gm = GameManager.Instance;
        keyText.gameObject.SetActive(gm != null && gm.showKeyStatus);
        keyText.text = "Key: " + (hasKey ? "Yes" : "No");
    }

    void HandleCoins(int coins) { coinText.text = "Coins: " + coins; }

    void HandleMenu(bool show)
    {
        menuPanel.SetActive(show);
        helpText.gameObject.SetActive(!show);
    }

    void HandlePause(bool paused) { pausePanel.SetActive(paused); }

    void HandleEnd(bool win, string message)
    {
        pausePanel.SetActive(false);
        endText.text = message;
        endText.color = win ? WinColor : LoseColor;
        endPanel.SetActive(true);
    }

    // ---------- Build the UI ----------

    void BuildUI()
    {
        var canvasGO = new GameObject("Game UI Canvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        Transform root = canvasGO.transform;

        // ----- HUD -----
        hintText = MakeText("Hint Text", root, "", 40, TextAnchor.UpperCenter, Color.white, FontStyle.Bold);
        Anchor(hintText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
               new Vector2(0, -40), new Vector2(-300, 140));
        AddOutline(hintText);

        keyText = MakeText("Key Text", root, "Key: No", 40, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.3f), FontStyle.Bold);
        Anchor(keyText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
               new Vector2(30, -30), new Vector2(400, 60));
        AddOutline(keyText);
        keyText.gameObject.SetActive(false);

        coinText = MakeText("Coin Text", root, "Coins: 0", 40, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.2f), FontStyle.Bold);
        Anchor(coinText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
               new Vector2(30, -90), new Vector2(400, 60));
        AddOutline(coinText);

        helpText = MakeText("Help Text", root, "WASD: move   Mouse: look   Space: jump   Click: shrink wall   Esc / P: pause   R: restart",
                            26, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.7f), FontStyle.Normal);
        Anchor(helpText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
               new Vector2(30, 20), new Vector2(1200, 50));
        AddOutline(helpText);
        helpText.gameObject.SetActive(false);

        // ----- Main menu -----
        menuPanel = MakePanel("Menu Panel", root, new Color(0.04f, 0.05f, 0.10f, 0.92f));
        Place(MakeText("Title", menuPanel.transform, "MAZE ESCAPE", 110, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold).rectTransform,
              new Vector2(0, 230), new Vector2(1400, 160));
        Place(MakeText("Subtitle", menuPanel.transform, "Find the way out of the maze.", 40, TextAnchor.MiddleCenter,
                       new Color(1f, 1f, 1f, 0.8f), FontStyle.Normal).rectTransform,
              new Vector2(0, 120), new Vector2(1400, 80));
        MakeButton("PLAY", menuPanel.transform, new Vector2(0, -20),  () => GameManager.Instance.StartGame());
        MakeButton("QUIT", menuPanel.transform, new Vector2(0, -130), () => GameManager.Instance.QuitGame());

        // ----- Pause menu -----
        pausePanel = MakePanel("Pause Panel", root, new Color(0f, 0f, 0f, 0.65f));
        Place(MakeText("Title", pausePanel.transform, "PAUSED", 100, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold).rectTransform,
              new Vector2(0, 210), new Vector2(1400, 140));
        MakeButton("RESUME",    pausePanel.transform, new Vector2(0, 40),   () => GameManager.Instance.Resume());
        MakeButton("RESTART",   pausePanel.transform, new Vector2(0, -60),  () => GameManager.Instance.Restart());
        MakeButton("MAIN MENU", pausePanel.transform, new Vector2(0, -160), () => GameManager.Instance.ReturnToMenu());

        // ----- Win / lose panel -----
        endPanel = MakePanel("End Panel", root, new Color(0f, 0f, 0f, 0.7f));
        endText = MakeText("End Text", endPanel.transform, "", 80, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Place(endText.rectTransform, new Vector2(0, 170), new Vector2(1500, 320));
        MakeButton("RESTART",   endPanel.transform, new Vector2(0, -80),  () => GameManager.Instance.Restart());
        MakeButton("MAIN MENU", endPanel.transform, new Vector2(0, -180), () => GameManager.Instance.ReturnToMenu());
    }

    // ---------- Small UI helpers ----------

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // full-screen dim background that also blocks clicks on whatever is behind it
    GameObject MakePanel(string name, Transform parent, Color color)
    {
        var r = MakeRect(name, parent);
        Stretch(r);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        r.gameObject.SetActive(false);
        return r.gameObject;
    }

    Text MakeText(string name, Transform parent, string content, int size, TextAnchor align, Color color, FontStyle style)
    {
        var r = MakeRect(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(string label, Transform parent, Vector2 pos, UnityAction onClick)
    {
        var r = MakeRect(label + " Button", parent);
        Place(r, pos, new Vector2(420, 90));

        var img = r.gameObject.AddComponent<Image>();
        img.color = Color.white;

        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.navigation = new Navigation { mode = Navigation.Mode.None };   // Space/Enter must not re-click it

        var c = b.colors;
        c.normalColor = BtnNormal;
        c.highlightedColor = BtnHover;
        c.pressedColor = BtnPressed;
        c.selectedColor = BtnNormal;
        b.colors = c;

        var t = MakeText("Label", r, label, 40, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        Stretch(t.rectTransform);

        b.onClick.AddListener(onClick);
        return b;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    // centered on screen, offset by pos
    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        Anchor(r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
    }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static void AddOutline(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.8f);
        o.effectDistance = new Vector2(2f, -2f);
    }
}
