using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Patrón Observer — muestra los corazones de vida del jugador en pantalla.
// DontDestroyOnLoad: persiste entre escenas. Se actualiza cada frame leyendo
// CrystalRespawnManager.Instance.Lives (singleton local de cada escena).
public class PlayerLivesHUD : MonoBehaviour
{
    private static PlayerLivesHUD _instance;

    private Canvas           _canvas;
    private CanvasGroup      _group;
    private TextMeshProUGUI  _heartsText;

    private int _lastLives = -1;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Update()
    {
        var crm = CrystalRespawnManager.Instance;
        if (crm == null)
        {
            if (_group != null) _group.alpha = 0f;
            return;
        }

        if (_group != null) _group.alpha = 1f;

        int lives = crm.Lives;
        if (lives == _lastLives) return;
        _lastLives = lives;
        RefreshDisplay(lives);
    }

    private void RefreshDisplay(int lives)
    {
        // Corazones: llenos en rojo, vacíos en gris (hasta 5 total)
        int maxLives = 5;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < maxLives; i++)
        {
            if (i < lives)
                sb.Append("<color=#FF4444>♥</color>");
            else
                sb.Append("<color=#444444>♥</color>");
            if (i < maxLives - 1) sb.Append(" ");
        }

        if (_heartsText != null) _heartsText.text = sb.ToString();

        if (lives <= 1 && _heartsText != null)
            StartCoroutine(FlashWarning());
    }

    private IEnumerator FlashWarning()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_group != null) _group.alpha = 0.3f;
            yield return new WaitForSeconds(0.1f);
            if (_group != null) _group.alpha = 1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("PlayerHUD_Canvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 150;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        _group = canvasGO.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.alpha          = 0f;

        // Panel fondo semitransparente — esquina superior izquierda
        var panel = MakeRect("LivesPanel", canvasGO.transform);
        var pImg  = panel.AddComponent<Image>();
        pImg.color = new Color(0f, 0f, 0f, 0.45f);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot     = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(20f, -20f);
        prt.sizeDelta = new Vector2(220f, 50f);

        // Corazones
        var heartsGO = new GameObject("Hearts");
        heartsGO.transform.SetParent(panel.transform, false);
        _heartsText = heartsGO.AddComponent<TextMeshProUGUI>();
        _heartsText.fontSize  = 22;
        _heartsText.richText  = true;
        _heartsText.alignment = TextAlignmentOptions.MidlineLeft;
        var hrt = heartsGO.GetComponent<RectTransform>();
        hrt.anchorMin        = Vector2.zero;
        hrt.anchorMax        = Vector2.one;
        hrt.offsetMin        = new Vector2(10f, 0f);
        hrt.offsetMax        = new Vector2(-10f, 0f);
    }

    private static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
