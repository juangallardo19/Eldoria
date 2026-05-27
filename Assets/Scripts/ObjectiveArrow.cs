using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Patrón Observer — flecha en pantalla que apunta hacia un objetivo en el mundo.
// Cuando el objetivo está en pantalla muestra ▼ sobre él; si está fuera, en el borde.
public class ObjectiveArrow : MonoBehaviour
{
    public static ObjectiveArrow Instance { get; private set; }

    CanvasGroup   _group;
    RectTransform _arrowRt;
    TMP_Text      _arrowTmp;

    Vector3 _worldTarget;
    bool    _showing;

    const float EDGE_PAD = 70f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[ObjectiveArrow]");
        go.AddComponent<ObjectiveArrow>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        _group.alpha = 0f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Show(Vector3 worldPos)
    {
        if (Instance == null) return;
        Instance._worldTarget = worldPos;
        Instance._showing     = true;
        Instance._group.alpha = 1f;
    }

    public static void Hide()
    {
        if (Instance == null) return;
        Instance._showing    = false;
        Instance._group.alpha = 0f;
    }

    void LateUpdate()
    {
        if (!_showing || Camera.main == null) return;

        Vector3 sp = Camera.main.WorldToScreenPoint(_worldTarget);
        bool onScreen = sp.z > 0f
            && sp.x > EDGE_PAD && sp.x < Screen.width  - EDGE_PAD
            && sp.y > EDGE_PAD && sp.y < Screen.height - EDGE_PAD;

        float bob = Mathf.Sin(Time.unscaledTime * 5f) * 9f;

        if (onScreen)
        {
            _arrowRt.position   = new Vector3(sp.x, sp.y + 40f + bob, 0f);
            _arrowTmp.text      = "▼";
        }
        else
        {
            Vector2 center  = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir     = new Vector2(sp.x - center.x, sp.y - center.y).normalized;
            float   angle   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float cx = Mathf.Clamp(sp.x, EDGE_PAD, Screen.width  - EDGE_PAD);
            float cy = Mathf.Clamp(sp.y, EDGE_PAD, Screen.height - EDGE_PAD);
            _arrowRt.position   = new Vector3(cx, cy + bob, 0f);
            _arrowTmp.text      = EdgeArrow(angle);
        }
    }

    static string EdgeArrow(float deg)
    {
        if (deg >= -45f && deg < 45f)  return "►";
        if (deg >= 45f  && deg < 135f) return "▲";
        if (deg <= -135f || deg >= 135f) return "◄";
        return "▼";
    }

    void BuildUI()
    {
        var cvGO            = new GameObject("Arrow_Canvas");
        cvGO.transform.SetParent(transform);
        var cv              = cvGO.AddComponent<Canvas>();
        cv.renderMode       = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder     = 145;

        _group              = cvGO.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;
        cvGO.AddComponent<CanvasScaler>();   // 1:1 pixel, sin CanvasScaler extra
        cvGO.AddComponent<GraphicRaycaster>();

        var go  = new GameObject("Arrow");
        go.transform.SetParent(cvGO.transform, false);
        _arrowRt            = go.AddComponent<RectTransform>();
        _arrowRt.sizeDelta  = new Vector2(64f, 64f);

        _arrowTmp           = go.AddComponent<TextMeshProUGUI>();
        _arrowTmp.text      = "►";
        _arrowTmp.fontSize  = 52f;
        _arrowTmp.color     = new Color(1f, 0.88f, 0.18f, 1f);
        _arrowTmp.alignment = TextAlignmentOptions.Center;
        _arrowTmp.fontStyle = FontStyles.Bold;

#if UNITY_EDITOR
        var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var f = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (f != null) _arrowTmp.font = f;
    }
}
