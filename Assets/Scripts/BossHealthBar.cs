using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pattern: Observer — listens to BossObsesion.OnHealthChanged / OnPhaseChanged / OnBossDead.
// Health bar using two layered sprites:
//   Layer 1 (background): 0%.png  — always visible, represents the empty bar
//   Layer 2 (fill):       100%.png — Image.Type.Filled horizontal, clips without stretching
// Position: top-centre of the screen.
public class BossHealthBar : MonoBehaviour
{
    [Header("Sprites (assign in Inspector)")]
    [SerializeField] private Sprite fullBarSprite;    // 100%.png
    [SerializeField] private Sprite emptyBarSprite;   // 0%.png

    [Header("Name font (assign Perfect DOS VGA 437 Win SDF in Inspector)")]
    [SerializeField] private TMP_FontAsset bossNameFont;

    private Canvas          _canvas;
    private CanvasGroup     _canvasGroup;
    private Image           _hpFill;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _phaseText;

    // Fallback colours (when no sprites are assigned)
    private static readonly Color Col1 = new Color(0.85f, 0.15f, 0.1f);
    private static readonly Color Col2 = new Color(0.9f, 0.45f, 0.0f);
    private static readonly Color Col3 = new Color(1f, 0.9f, 0.0f);

    void Awake()
    {
        BuildUI();   // _canvasGroup is initialised inside BuildUI
        BossObsesion.OnHealthChanged  += HandleHealthChanged;
        BossObsesion.OnPhaseChanged   += HandlePhaseChanged;
        BossObsesion.OnBossDefeated   += HandleBossDefeated;
        BossObsesion.OnBossDead       += HandleBossDead;
    }

    void OnDestroy()
    {
        BossObsesion.OnHealthChanged  -= HandleHealthChanged;
        BossObsesion.OnPhaseChanged   -= HandlePhaseChanged;
        BossObsesion.OnBossDefeated   -= HandleBossDefeated;
        BossObsesion.OnBossDead       -= HandleBossDead;
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

        // Colour tint only when no sprite is assigned (fallback mode)
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

    private void HandleBossDefeated() => StartCoroutine(FadeOut(0f));  // disappears immediately
    private void HandleBossDead()     => StartCoroutine(FadeOut(0f));  // safety fallback

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Auto-load the game font if it was not assigned in the Inspector
#if UNITY_EDITOR
        if (bossNameFont == null)
            bossNameFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        if (bossNameFont == null)
            bossNameFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif

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

        const float BAR_W  = 800f;
        const float BAR_H  = 260f;

        // ── HUDGroup: bottom edge, full width ────────────────────────────────
        var hudGO = new GameObject("HUDGroup");
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hudRt = hudGO.AddComponent<RectTransform>();
        hudRt.anchorMin        = new Vector2(0f, 0f);   // stretch X, anchored to bottom
        hudRt.anchorMax        = new Vector2(1f, 0f);
        hudRt.pivot            = new Vector2(0.5f, 1f);
        hudRt.anchoredPosition = new Vector2(0f, 210f); // top edge 210px from bottom
        hudRt.sizeDelta        = new Vector2(0f, 288f); // full width (Left=0, Right=0)

        // ── BarContainer: top-center of HUDGroup, 800×260 ───────────────────
        var barGO = new GameObject("BarContainer");
        barGO.transform.SetParent(hudGO.transform, false);
        barGO.AddComponent<RectTransform>();
        var brt = barGO.GetComponent<RectTransform>();
        brt.anchorMin        = new Vector2(0.5f, 1f);
        brt.anchorMax        = new Vector2(0.5f, 1f);
        brt.pivot            = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, 0f);
        brt.sizeDelta        = new Vector2(BAR_W, BAR_H);

        // Layer 1: empty bar — 0%.png, always visible
        var bgGO  = MakeRect("HPBar_Empty", barGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        if (emptyBarSprite != null)
        {
            bgImg.sprite         = emptyBarSprite;
            bgImg.type           = Image.Type.Simple;
            bgImg.preserveAspect = false;   // sizeDelta already maintains the correct ratio
        }
        else
        {
            bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        }
        StretchFull(bgGO.GetComponent<RectTransform>());

        // Layer 2: full bar — 100%.png, clips from right to left
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

        // Phase text is disabled — _phaseText remains null; HandlePhaseChanged null-checks it.
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
        _hpFill.color = new Color(1f, 0.35f, 0.35f);   // brief red tint on damage taken
        yield return new WaitForSeconds(0.07f);
        _hpFill.color = prev;
    }

    private IEnumerator FadeOut(float delay = 0f)
    {
        if (_canvasGroup == null) yield break;
        if (_canvasGroup.alpha <= 0f) yield break;  // already invisible, nothing to do
        yield return new WaitForSeconds(delay);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;   // 0.5s fade
            _canvasGroup.alpha = 1f - t;
            yield return null;
        }
        _canvasGroup.alpha = 0f;
    }
}
