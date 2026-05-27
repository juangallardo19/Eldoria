using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Patrón Singleton DDOL + Command — panel de diálogo ancho en la parte superior de la pantalla.
// Show(pages, onComplete) encapsula la conversación; onComplete se llama al terminar.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive => Instance != null && Instance._active;

    public static event Action OnDialogueComplete;

    public struct DialoguePage
    {
        public string speakerName;
        public Color  nameColor;
        public Sprite portrait;
        public Sprite portraitBlink;
        public string text;
    }

    // ── Layout (referencia 1920×1080) ──────────────────────────────────────
    const float PANEL_H       = 200f;
    const float PANEL_PAD_Y   = 0f;     // pegado al borde superior
    const float PORTRAIT_SIZE = 160f;
    const float INNER_PAD     = 18f;
    const float NAME_H        = 36f;
    const float TYPING_SPEED  = 32f;    // chars/seg

    CanvasGroup  _group;
    Image        _portrait;
    TMP_Text     _nameLabel;
    TMP_Text     _bodyLabel;
    AudioSource  _typingSource;
    AudioClip    _typingBeep;

    bool           _active;
    DialoguePage[] _pages;
    int            _pageIndex;
    bool           _typing;
    string         _fullText;
    Coroutine      _typeCoroutine;
    Action         _onComplete;

    Sprite _idleSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[DialogueManager]");
        go.AddComponent<DialogueManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        BuildTypingAudio();
        SetVisible(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!_active) return;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) ||
            Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            Advance();
    }

    // ── API pública ────────────────────────────────────────────────────────

    public void Show(DialoguePage[] pages, Action onComplete = null)
    {
        if (pages == null || pages.Length == 0) { onComplete?.Invoke(); return; }
        _pages      = pages;
        _pageIndex  = 0;
        _onComplete = onComplete;
        _active     = true;
        SetVisible(true);
        ShowPage(0);
    }

    // ── Lógica interna ─────────────────────────────────────────────────────

    void Advance()
    {
        if (_typing)
        {
            if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
            _bodyLabel.text = _fullText;
            _typing = false;
            if (_typingSource != null && _typingSource.isPlaying) _typingSource.Stop();
            return;
        }
        _pageIndex++;
        if (_pageIndex >= _pages.Length) { Close(); return; }
        ShowPage(_pageIndex);
    }

    void ShowPage(int idx)
    {
        var p = _pages[idx];
        _nameLabel.text  = p.speakerName;
        _nameLabel.color = p.nameColor;

        _idleSprite      = p.portrait;
        _portrait.sprite  = _idleSprite;
        _portrait.enabled = _idleSprite != null;

        _fullText = p.text;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(_fullText));
    }

    IEnumerator TypeText(string text)
    {
        _typing = true;
        _bodyLabel.text = "";
        float delay    = 1f / TYPING_SPEED;
        int   charIdx  = 0;
        foreach (char c in text)
        {
            _bodyLabel.text += c;
            charIdx++;
            // Sonido de typing cada 2 caracteres no espacios
            if (charIdx % 2 == 0 && c != ' ' && _typingSource != null && _typingBeep != null)
                _typingSource.PlayOneShot(_typingBeep);
            yield return new WaitForSecondsRealtime(delay);
        }
        _typing = false;
    }

    void Close()
    {
        _active = false;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        if (_typingSource != null && _typingSource.isPlaying) _typingSource.Stop();
        SetVisible(false);
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
        OnDialogueComplete?.Invoke();
    }

    void SetVisible(bool v) { if (_group != null) _group.alpha = v ? 1f : 0f; }

    // ── Construcción de UI ─────────────────────────────────────────────────

    void BuildUI()
    {
        var cvGO            = new GameObject("Dlg_Canvas");
        cvGO.transform.SetParent(transform);
        var cv              = cvGO.AddComponent<Canvas>();
        cv.renderMode       = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder     = 150;

        _group              = cvGO.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var sc              = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // ── Panel ancho completo ─────────────────────────────────────────
        var panelGO = Rect("Panel", cvGO.transform);
        var panelRt = panelGO.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 1f);
        panelRt.anchorMax        = new Vector2(1f, 1f);
        panelRt.pivot            = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -PANEL_PAD_Y);
        panelRt.sizeDelta        = new Vector2(0f, PANEL_H);  // ancho = stretch full

        var bg       = panelGO.AddComponent<Image>();
        bg.color     = new Color(0f, 0f, 0f, 0.93f);
        bg.raycastTarget = false;

        // Línea separadora inferior sutil
        var lineGO = Rect("BottomLine", panelGO.transform);
        var lineRt = lineGO.GetComponent<RectTransform>();
        lineRt.anchorMin        = new Vector2(0f, 0f);
        lineRt.anchorMax        = new Vector2(1f, 0f);
        lineRt.pivot            = new Vector2(0.5f, 1f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta        = new Vector2(0f, 2f);
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        lineImg.raycastTarget = false;

        // ── Retrato ──────────────────────────────────────────────────────
        var portGO = Rect("Portrait", panelGO.transform);
        var portRt = portGO.GetComponent<RectTransform>();
        portRt.anchorMin        = new Vector2(0f, 0.5f);
        portRt.anchorMax        = new Vector2(0f, 0.5f);
        portRt.pivot            = new Vector2(0f, 0.5f);
        portRt.anchoredPosition = new Vector2(INNER_PAD, 0f);
        portRt.sizeDelta        = new Vector2(PORTRAIT_SIZE, PORTRAIT_SIZE);
        _portrait               = portGO.AddComponent<Image>();
        _portrait.preserveAspect = true;
        _portrait.raycastTarget  = false;

        float textX = INNER_PAD + PORTRAIT_SIZE + INNER_PAD;

        // ── Nombre del hablante ──────────────────────────────────────────
        var nameGO = Rect("Name", panelGO.transform);
        var nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin        = new Vector2(0f, 1f);
        nameRt.anchorMax        = new Vector2(1f, 1f);
        nameRt.pivot            = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(textX, -INNER_PAD);
        nameRt.sizeDelta        = new Vector2(-(textX + INNER_PAD), NAME_H);
        _nameLabel              = nameGO.AddComponent<TextMeshProUGUI>();
        _nameLabel.fontSize     = 30f;
        _nameLabel.fontStyle    = FontStyles.Bold;
        SetFont(_nameLabel);

        // ── Cuerpo del texto ─────────────────────────────────────────────
        float bodyY = -(INNER_PAD + NAME_H + 6f);
        float bodyH = PANEL_H - INNER_PAD - NAME_H - 6f - INNER_PAD;
        var bodyGO = Rect("Body", panelGO.transform);
        var bodyRt = bodyGO.GetComponent<RectTransform>();
        bodyRt.anchorMin        = new Vector2(0f, 1f);
        bodyRt.anchorMax        = new Vector2(1f, 1f);
        bodyRt.pivot            = new Vector2(0f, 1f);
        bodyRt.anchoredPosition = new Vector2(textX, bodyY);
        bodyRt.sizeDelta        = new Vector2(-(textX + INNER_PAD), bodyH);
        _bodyLabel              = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyLabel.fontSize     = 26f;
        _bodyLabel.color        = Color.white;
        _bodyLabel.overflowMode = TextOverflowModes.Overflow;
        SetFont(_bodyLabel);

        // ── Hint continuar ───────────────────────────────────────────────
        var hintGO = Rect("Hint", panelGO.transform);
        var hintRt = hintGO.GetComponent<RectTransform>();
        hintRt.anchorMin        = new Vector2(1f, 0f);
        hintRt.anchorMax        = new Vector2(1f, 0f);
        hintRt.pivot            = new Vector2(1f, 0f);
        hintRt.anchoredPosition = new Vector2(-INNER_PAD, INNER_PAD);
        hintRt.sizeDelta        = new Vector2(260f, 24f);
        var hintTmp             = hintGO.AddComponent<TextMeshProUGUI>();
        hintTmp.text            = "[ Z / X ]  Continuar";
        hintTmp.fontSize        = 16f;
        hintTmp.color           = new Color(1f, 1f, 1f, 0.45f);
        hintTmp.alignment       = TextAlignmentOptions.Right;
        SetFont(hintTmp);
    }

    // ── Audio de typing ────────────────────────────────────────────────────

    void BuildTypingAudio()
    {
        _typingSource           = gameObject.AddComponent<AudioSource>();
        _typingSource.playOnAwake = false;
        _typingSource.volume    = 0.18f;
        _typingSource.spatialBlend = 0f;
        _typingBeep             = CreateBeep();
    }

    static AudioClip CreateBeep()
    {
        const int SAMPLE_RATE = 22050;
        const int SAMPLES     = 512;   // ~23ms — corto y sutil
        var clip = AudioClip.Create("TypingBeep", SAMPLES, 1, SAMPLE_RATE, false);
        var data = new float[SAMPLES];
        for (int i = 0; i < SAMPLES; i++)
        {
            float t        = (float)i / SAMPLE_RATE;
            float envelope = 1f - (float)i / SAMPLES;  // fade out lineal
            data[i] = Mathf.Sin(2f * Mathf.PI * 900f * t) * envelope * 0.25f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static void SetFont(TMP_Text t)
    {
#if UNITY_EDITOR
        var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var f = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (f != null) t.font = f;
    }

    static GameObject Rect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
