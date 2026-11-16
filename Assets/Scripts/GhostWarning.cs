using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows a pulsing red warning panel when any ghost is closer to the player than warningDistance.
// Creates itself automatically when the game starts, so no scene setup is needed.
// (To change the settings in the Inspector, add this script to an empty GameObject yourself.)
public class GhostWarning : MonoBehaviour
{
    [Header("Warning")]
    [Tooltip("Show the warning when a ghost is closer than this (world units). The maze cells are only " +
             "1 unit wide, so 3.5 is about 3 cells away. Set it very high (e.g. 100) to test the panel.")]
    public float warningDistance = 3.5f;
    [Tooltip("The warning only turns off again once the ghost is this much farther away (stops flickering).")]
    public float hideBuffer = 0.5f;
    public string message = "WARNING: GHOST NEARBY!";
    public bool showDistance = true;
    public float pulseSpeed = 6f;

    [Header("Sound")]
    [Tooltip("Plays on a loop for as long as the warning is showing. If empty, loads Assets/Resources/GhostWarning (.mp3 / .wav / .ogg).")]
    public AudioClip warningClip;
    [Range(0f, 1f)] public float warningVolume = 1f;

    AudioSource audioSrc;

    GameObject panel;
    CanvasGroup panelGroup;
    Text distanceText;
    Font font;

    Transform player;
    GhostChase[] ghosts = new GhostChase[0];
    float nextRefresh;
    bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        // The game restarts by reloading the scene, which destroys this object, and the attribute
        // above only runs once at launch. So create it again every time a scene finishes loading.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Ensure(); }

    static void Ensure()
    {
        if (FindFirstObjectByType<GhostWarning>() == null)
            new GameObject("Ghost Warning").AddComponent<GhostWarning>();
    }

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();

        if (warningClip == null) warningClip = Resources.Load<AudioClip>("GhostWarning");
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.clip = warningClip;
        audioSrc.loop = true;            // keeps repeating until we call Stop()
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;      // 2D sound, same volume wherever the ghost is
        audioSrc.volume = warningVolume;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying)     // menu, paused or game over
        {
            SetVisible(false);
            return;
        }

        if (player == null)
        {
            player = gm.player;
            if (player == null) return;
        }

        // ghosts are spawned after the maze is built, so look for them again from time to time
        if (Time.time >= nextRefresh)
        {
            ghosts = FindObjectsByType<GhostChase>(FindObjectsSortMode.None);
            nextRefresh = Time.time + 0.5f;
        }

        float nearest = float.MaxValue;
        foreach (var g in ghosts)
        {
            if (g == null) continue;
            Vector3 d = g.transform.position - player.position;
            float flat = new Vector2(d.x, d.z).magnitude;   // ignore height, the ghost bobs up and down
            if (flat < nearest) nearest = flat;
        }

        float limit = visible ? warningDistance + hideBuffer : warningDistance;
        SetVisible(nearest < limit);

        if (visible)
        {
            panelGroup.alpha = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            if (showDistance) distanceText.text = "Distance: " + nearest.ToString("F1");
        }
    }

    void SetVisible(bool show)
    {
        if (visible == show) return;
        visible = show;
        panel.SetActive(show);

        // sound starts with the warning and stops when it disappears
        if (audioSrc != null && audioSrc.clip != null)
        {
            if (show) audioSrc.Play();
            else audioSrc.Stop();
        }
    }

    // ---------- Build the panel ----------

    void BuildUI()
    {
        var canvasGO = new GameObject("Ghost Warning Canvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // below the menus (UIManager uses 100), so they cover it

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Warning Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasGO.transform, false);

        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -190f);   // just under the hint text
        rt.sizeDelta = new Vector2(760f, 130f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.55f, 0f, 0f, 0.8f);
        bg.raycastTarget = false;

        panelGroup = panel.GetComponent<CanvasGroup>();
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        var title = MakeText(panel.transform, "Title", 46, FontStyle.Bold, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
        title.text = message;

        distanceText = MakeText(panel.transform, "Distance", 30, FontStyle.Normal, new Vector2(0f, 0f), new Vector2(1f, 0.42f));
        distanceText.gameObject.SetActive(showDistance);

        panel.SetActive(false);
    }

    Text MakeText(Transform parent, string name, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
