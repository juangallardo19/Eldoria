using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.4f;

    private Image fadeImage;
    private bool _isFading;

    // True mientras hay un fade en curso (transición o respawn local).
    // Bloquea llamadas concurrentes a LoadScene para evitar doble-carga.
    public bool IsFading => _isFading;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeCanvas();
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    private void BuildFadeCanvas()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("FadePanel");
        imgGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        var rect = imgGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void LoadScene(string sceneName)
    {
        if (_isFading) return;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        _isFading = true;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
        yield return StartCoroutine(FadeIn());
        _isFading = false;
    }

    // Expuestos para respawn/hazard sin cambio de escena (CrystalRespawnManager).
    // Marcan _isFading para que LoadScene no se dispare durante el respawn local.
    public Coroutine FadeOutAsync()
    {
        _isFading = true;
        return StartCoroutine(FadeOut());
    }

    public Coroutine FadeInAsync() => StartCoroutine(FadeInAndClear());

    private IEnumerator FadeInAndClear()
    {
        yield return StartCoroutine(FadeIn());
        _isFading = false;
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
    }

    private IEnumerator FadeIn()
    {
        SetAlpha(1f);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(1f - t / fadeDuration));
            yield return null;
        }
    }

    // Tutorial de dash — persiste hasta que el jugador presione C
    public void StartDashTutorial() => StartCoroutine(DashTutorialRoutine());

    private IEnumerator DashTutorialRoutine()
    {
        var canvasGO = new GameObject("DashTutCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        var bgRt  = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin        = new Vector2(0.5f, 0f);
        bgRt.anchorMax        = new Vector2(0.5f, 0f);
        bgRt.pivot            = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 80f);
        bgRt.sizeDelta        = new Vector2(640f, 72f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var tmp    = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "✦ DASH DESBLOQUEADO ✦\nPresiona  [ C ]  para activarlo";
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.9f, 0.35f);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin        = new Vector2(0.5f, 0f);
        trt.anchorMax        = new Vector2(0.5f, 0f);
        trt.pivot            = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 88f);
        trt.sizeDelta        = new Vector2(620f, 60f);

        // Parpadeo
        StartCoroutine(BlinkTMP(tmp));

        yield return new WaitForSeconds(0.6f);
        while (!Input.GetKeyDown(KeyCode.C))
            yield return null;

        Destroy(canvasGO);
    }

    private IEnumerator BlinkTMP(TextMeshProUGUI tmp)
    {
        while (tmp != null)
        {
            tmp.color = new Color(1f, 0.9f, 0.35f);
            yield return new WaitForSeconds(0.6f);
            if (tmp == null) break;
            tmp.color = new Color(1f, 0.9f, 0.35f, 0.3f);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // Flash abrupto configurable — para eventos de juego sin cambio de escena
    public Coroutine FastFadeOutAsync(float duration) => StartCoroutine(DoFastFadeOut(duration));
    public Coroutine FastFadeInAsync(float duration)  => StartCoroutine(DoFastFadeIn(duration));

    private IEnumerator DoFastFadeOut(float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; SetAlpha(Mathf.Clamp01(t / dur)); yield return null; }
        SetAlpha(1f);
    }

    private IEnumerator DoFastFadeIn(float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; SetAlpha(Mathf.Clamp01(1f - t / dur)); yield return null; }
        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
