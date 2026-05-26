using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Patrón Observer — escucha BossObsesion.OnHealthChanged / OnPhaseChanged / OnBossDead.
// Barra de vida con dos sprites superpuestos:
//   Capa 1 (fondo):  0%.png  — siempre visible, representa la barra vacía
//   Capa 2 (fill):   100%.png — Image.Type.Filled horizontal, se recorta sin deformar
// Posición: top-center de la pantalla.
public class BossHealthBar : MonoBehaviour
{
    [Header("Sprites (asignar en Inspector)")]
    [SerializeField] private Sprite fullBarSprite;    // 100%.png
    [SerializeField] private Sprite emptyBarSprite;   // 0%.png

    private Canvas          _canvas;
    private CanvasGroup     _canvasGroup;
    private Image           _hpFill;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _phaseText;

    // Colores de fallback (cuando no hay sprites asignados)
    private static readonly Color Col1 = new Color(0.85f, 0.15f, 0.1f);
    private static readonly Color Col2 = new Color(0.9f, 0.45f, 0.0f);
    private static readonly Color Col3 = new Color(1f, 0.9f, 0.0f);

    void Awake()
    {
        BuildUI();   // _canvasGroup se inicializa dentro de BuildUI
        BossObsesion.OnHealthChanged += HandleHealthChanged;
        BossObsesion.OnPhaseChanged  += HandlePhaseChanged;
        BossObsesion.OnBossDead      += HandleBossDead;
    }

    void OnDestroy()
    {
        BossObsesion.OnHealthChanged -= HandleHealthChanged;
        BossObsesion.OnPhaseChanged  -= HandlePhaseChanged;
        BossObsesion.OnBossDead      -= HandleBossDead;
    }

    // ── Eventos ──────────────────────────────────────────────────────────────

    private void HandleHealthChanged(int current, int max)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        if (_hpFill != null)
        {
            _hpFill.fillAmount = (float)current / max;
            StartCoroutine(FlashFill());
        }
    }

    private void HandlePhaseChanged(BossObsesion.BossPhase phase)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        if (_phaseText != null)
        {
            _phaseText.text = phase switch
            {
                BossObsesion.BossPhase.Phase2 => "— FASE II —",
                BossObsesion.BossPhase.Phase3 => "— FASE III  FRENESÍ —",
                _                             => ""
            };
        }

        // Solo tinta de color si no hay sprite asignado (modo fallback)
        if (fullBarSprite == null && _hpFill != null)
        {
            _hpFill.color = phase switch
            {
                BossObsesion.BossPhase.Phase2 => Col2,
                BossObsesion.BossPhase.Phase3 => Col3,
                _                             => Col1
            };
        }
    }

    private void HandleBossDead() => StartCoroutine(FadeOut());

    // ── Construcción de UI ────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGO = new GameObject("BossHUD_Canvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        _canvasGroup       = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Sprites 2172×724px → aspect ratio 3:1 exacto
        // A 690px de ancho → alto = 690 / (2172/724) = 230px
        const float BAR_W = 690f;
        const float BAR_H = 230f;

        // ── Nombre del boss (arriba de la barra) ─────────────────────────────
        var nameGO = new GameObject("BossName");
        nameGO.transform.SetParent(canvasGO.transform, false);
        _nameText = nameGO.AddComponent<TextMeshProUGUI>();
        _nameText.text      = "LA OBSESIÓN";
        _nameText.fontSize  = 26;
        _nameText.alignment = TextAlignmentOptions.Center;
        _nameText.color     = new Color(1f, 0.85f, 0.5f);
        var nrt = nameGO.GetComponent<RectTransform>();
        nrt.anchorMin        = new Vector2(0.5f, 1f);
        nrt.anchorMax        = new Vector2(0.5f, 1f);
        nrt.pivot            = new Vector2(0.5f, 1f);
        nrt.anchoredPosition = new Vector2(0f, -4f);
        nrt.sizeDelta        = new Vector2(BAR_W, 32f);

        // ── Contenedor de la barra ────────────────────────────────────────────
        var barGO = new GameObject("BarContainer");
        barGO.transform.SetParent(canvasGO.transform, false);
        barGO.AddComponent<RectTransform>();
        var brt = barGO.GetComponent<RectTransform>();
        brt.anchorMin        = new Vector2(0.5f, 1f);
        brt.anchorMax        = new Vector2(0.5f, 1f);
        brt.pivot            = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -40f);
        brt.sizeDelta        = new Vector2(BAR_W, BAR_H);

        // Capa 1: barra vacía — 0%.png, siempre visible
        var bgGO  = MakeRect("HPBar_Empty", barGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        if (emptyBarSprite != null)
        {
            bgImg.sprite         = emptyBarSprite;
            bgImg.type           = Image.Type.Simple;
            bgImg.preserveAspect = false;   // el sizeDelta ya mantiene el ratio correcto
        }
        else
        {
            bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        }
        StretchFull(bgGO.GetComponent<RectTransform>());

        // Capa 2: barra llena — 100%.png, se recorta de derecha a izquierda
        var fillGO = MakeRect("HPBar_Fill", barGO.transform);
        _hpFill   = fillGO.AddComponent<Image>();
        if (fullBarSprite != null)
        {
            _hpFill.sprite     = fullBarSprite;
            _hpFill.type       = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _hpFill.fillAmount = 1f;
        }
        else
        {
            _hpFill.color      = Col1;
            _hpFill.type       = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;
        }
        StretchFull(fillGO.GetComponent<RectTransform>());

        // Texto de fase desactivado — _phaseText queda null; HandlePhaseChanged hace null check.
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private IEnumerator FlashFill()
    {
        if (_hpFill == null) yield break;
        var prev = _hpFill.color;
        _hpFill.color = new Color(1f, 0.35f, 0.35f);   // tinte rojo breve al recibir daño
        yield return new WaitForSeconds(0.07f);
        _hpFill.color = prev;
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(2f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f - t;
            yield return null;
        }
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
    }
}
