using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Patrón Singleton DDOL — texto de pista grande en la parte inferior de la pantalla.
public class TutorialHint : MonoBehaviour
{
    public static TutorialHint Instance { get; private set; }

    CanvasGroup _group;
    TMP_Text    _label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[TutorialHint]");
        go.AddComponent<TutorialHint>();
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

    public static void Show(string text)
    {
        if (Instance == null) return;
        Instance._label.text  = text;
        Instance._group.alpha = 1f;
    }

    public static void Hide()
    {
        if (Instance == null) return;
        Instance._group.alpha = 0f;
    }

    void BuildUI()
    {
        var cvGO            = new GameObject("Hint_Canvas");
        cvGO.transform.SetParent(transform);
        var cv              = cvGO.AddComponent<Canvas>();
        cv.renderMode       = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder     = 140;

        _group              = cvGO.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var sc              = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        var go  = new GameObject("HintText");
        go.transform.SetParent(cvGO.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 55f);
        rt.sizeDelta        = new Vector2(0f, 70f);

        _label              = go.AddComponent<TextMeshProUGUI>();
        _label.fontSize     = 38f;
        _label.color        = Color.white;
        _label.alignment    = TextAlignmentOptions.Center;
        _label.fontStyle    = FontStyles.Bold;

#if UNITY_EDITOR
        var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var f = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (f != null) _label.font = f;
    }
}
