using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Singleton DontDestroyOnLoad — persistent FPS counter overlay.
// Patterns: Singleton (global instance) + Observer (reacts to sceneLoaded).
// Auto-created at game start; shows/hides based on the ShowFPS preference.
public class FpsCounter : MonoBehaviour
{
    public static FpsCounter Instance { get; private set; }

    [SerializeField] TextMeshProUGUI _label;

    float _elapsed;
    int   _frames;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_label == null) BuildDisplay();

        SceneManager.sceneLoaded += OnSceneLoaded;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Refresh();

    void Update()
    {
        _frames++;
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed >= 0.5f)
        {
            if (_label != null) _label.text = $"{_frames / _elapsed:F0} FPS";
            _elapsed = 0f;
            _frames  = 0;
        }
    }

    void Refresh() => gameObject.SetActive(PlayerPrefs.GetInt(EldoriaPrefsKeys.ShowFPS, 0) == 1);

    public static void SetVisible(bool on)
    {
        PlayerPrefs.SetInt(EldoriaPrefsKeys.ShowFPS, on ? 1 : 0);
        if (Instance != null) Instance.gameObject.SetActive(on);
    }

    // Creates a Canvas overlay + label in the top-right corner when no prefab is assigned.
    void BuildDisplay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>();

        var go = new GameObject("FpsLabel");
        go.transform.SetParent(transform, false);

        _label              = go.AddComponent<TextMeshProUGUI>();
        _label.fontSize     = 18;
        _label.color        = Color.white;
        _label.alignment    = TextAlignmentOptions.TopRight;
        _label.fontStyle    = FontStyles.Bold;
        _label.outlineColor = Color.black;
        _label.outlineWidth = 0.2f;
        _label.text         = "-- FPS";

        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.one;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = Vector2.one;
        rt.anchoredPosition = new Vector2(-12f, -12f);
        rt.sizeDelta        = new Vector2(120f, 30f);
    }

    // Runs once at game startup — creates the instance if it doesn't exist.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("FpsCounter");
        go.AddComponent<FpsCounter>();
    }
}
