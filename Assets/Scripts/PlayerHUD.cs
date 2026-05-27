using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Patrón Singleton + Observer + State Machine (una AraIcon por vida).
// DontDestroyOnLoad — persiste entre todas las escenas del juego.
// Se oculta automáticamente en MainMenu, Settings, SlotsScreen, Intro.
// Retrato en vivo: cámara secundaria ortográfica sigue la cara de Kael → RenderTexture → RawImage.
public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    static readonly HashSet<string> s_HideScenes = new HashSet<string>
        { "MainMenu", "Settings", "SlotsScreen", "Intro" };

    // ── Layout (referencia 1920×1080) ──────────────────────────────────────────
    const float PORTRAIT_SIZE  = 180f;   // tamaño del cuadro de retrato (cuadrado)
    const float ARA_SIZE       = 110f;   // tamaño de cada icono Ara
    const float ARA_GAP        = 10f;    // espacio entre Aras
    const float GAP_PORT_ARAS  = 14f;    // espacio entre retrato y sección de Aras
    const float HUD_PAD_X      = 18f;
    const float HUD_PAD_Y      = 18f;
    const float CONTAINER_PAD  = 14f;    // padding interior del container de Aras

    // ── Cámara de retrato ──────────────────────────────────────────────────────
    // HEAD_OFFSET_Y: unidades sobre el pivot del jugador hasta el centro de la cara
    // (sprite 128×128, PPU=16, pivot.y=0.4 → la cabeza está a ~3.5u sobre el pivot)
    const float HEAD_OFFSET_Y  = 2.5f;
    const float PORTRAIT_ORTHO = 1.8f;  // radio ortográfico en unidades mundo (zoom de cara)
    const float CAM_Z_OFFSET   = -9f;   // la cámara de retrato está delante de la cámara principal

    const float KAEL_FPS = 10f;  // usado solo como fallback si la cámara falla

    PlayerHUDConfig  _cfg;
    CanvasGroup      _group;
    RawImage         _portraitRaw;   // muestra el RenderTexture de la cámara de cara
    Camera           _portraitCam;
    RenderTexture    _portraitRT;
    Transform        _playerTransform;
    AraIcon[]        _aras;

    // ── Bootstrap ─────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[PlayerHUD]");
        go.AddComponent<PlayerHUD>();
        DontDestroyOnLoad(go);
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cfg = Resources.Load<PlayerHUDConfig>("PlayerHUDConfig");
        if (_cfg == null)
        {
            Debug.LogWarning("[PlayerHUD] PlayerHUDConfig no encontrado en Resources/. Ejecuta Eldoria/Setup Player HUD.");
            return;
        }

        BuildPortraitCamera();
        BuildUI();
        UpdateVisibility(SceneManager.GetActiveScene().name);

        CrystalRespawnManager.OnLivesChanged  += HandleLivesChanged;
        CrystalRespawnManager.OnDamageTaken   += HandleDamageTaken;
        CrystalRespawnManager.OnLivesRestored += HandleLivesRestored;
        SceneManager.sceneLoaded              += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (_portraitRT != null) _portraitRT.Release();
        if (_portraitCam != null) Destroy(_portraitCam.gameObject);
        CrystalRespawnManager.OnLivesChanged  -= HandleLivesChanged;
        CrystalRespawnManager.OnDamageTaken   -= HandleDamageTaken;
        CrystalRespawnManager.OnLivesRestored -= HandleLivesRestored;
        SceneManager.sceneLoaded              -= OnSceneLoaded;
    }

    void Update()
    {
        if (_cfg == null || _group == null || _group.alpha < 0.01f) return;
        UpdatePortraitCamera();
        if (_aras != null)
            foreach (var a in _aras) a.Tick(Time.unscaledDeltaTime, _cfg);
    }

    // ── Eventos ───────────────────────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _playerTransform = null;  // resetear caché — nuevo jugador en nueva escena
        UpdateVisibility(scene.name);
        if (CrystalRespawnManager.Instance != null)
            SyncImmediate(CrystalRespawnManager.Instance.Lives);
    }

    void HandleLivesChanged(int lives) => SyncImmediate(lives);

    void HandleDamageTaken(int newLives, int prevLives)
    {
        int lost = prevLives - newLives;
        for (int i = 0; i < lost; i++)
        {
            int idx = prevLives - 1 - i;
            if (idx >= 0 && idx < _aras.Length) _aras[idx].TriggerDeath();
        }
    }

    void HandleLivesRestored(int lives)
    {
        if (_aras == null) return;
        for (int i = 0; i < _aras.Length; i++)
            _aras[i].Revive(_cfg, i < lives);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void UpdateVisibility(string sceneName)
    {
        if (_group == null) return;
        _group.alpha = s_HideScenes.Contains(sceneName) ? 0f : 1f;
        if (_portraitCam != null)
            _portraitCam.enabled = !s_HideScenes.Contains(sceneName);
    }

    void SyncImmediate(int lives)
    {
        if (_aras == null) return;
        for (int i = 0; i < _aras.Length; i++)
        {
            if (i < lives) _aras[i].ForceAlive(_cfg);
            else           _aras[i].ForceDead(_cfg);
        }
    }

    // ── Cámara de retrato en vivo ─────────────────────────────────────────────

    void BuildPortraitCamera()
    {
        // RenderTexture cuadrada para la cara de Kael
        _portraitRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        _portraitRT.filterMode = FilterMode.Point;  // pixel art — sin interpolación
        _portraitRT.Create();

        var camGO = new GameObject("[PortraitCam]");
        DontDestroyOnLoad(camGO);

        _portraitCam = camGO.AddComponent<Camera>();
        _portraitCam.orthographic     = true;
        _portraitCam.orthographicSize = PORTRAIT_ORTHO;
        _portraitCam.clearFlags       = CameraClearFlags.SolidColor;
        _portraitCam.backgroundColor  = new Color(0.08f, 0.06f, 0.05f, 1f);
        _portraitCam.depth            = -2;          // renderiza antes que la cámara principal
        _portraitCam.targetTexture    = _portraitRT;
        _portraitCam.nearClipPlane    = 0.3f;
        _portraitCam.farClipPlane     = 20f;
        // Excluir capa UI del retrato para que el HUD no aparezca en su propio retrato
        _portraitCam.cullingMask      = ~(1 << LayerMask.NameToLayer("UI"));
        _portraitCam.enabled          = false;  // se activa en UpdateVisibility
    }

    void UpdatePortraitCamera()
    {
        if (_portraitCam == null) return;

        // Buscar jugador si no está cacheado
        if (_playerTransform == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) _playerTransform = pc.transform;
        }

        if (_playerTransform != null)
        {
            var p = _playerTransform.position;
            _portraitCam.transform.position = new Vector3(p.x, p.y + HEAD_OFFSET_Y, p.z + CAM_Z_OFFSET);
        }
    }

    // ── Construcción de UI ────────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas raíz — sortingOrder 50 (encima del juego, bajo BossHUD 100 y PauseMenu 200)
        var cvGO = new GameObject("HUD_Canvas");
        cvGO.transform.SetParent(transform);
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;

        _group = cvGO.AddComponent<CanvasGroup>();
        _group.interactable   = false;
        _group.blocksRaycasts = false;

        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // ── Panel unificado: container cubre retrato + Aras ──────────────────
        float araRowW = 5f * ARA_SIZE + 4f * ARA_GAP;
        float hudW    = CONTAINER_PAD + PORTRAIT_SIZE + GAP_PORT_ARAS + araRowW + CONTAINER_PAD;
        float hudH    = Mathf.Max(PORTRAIT_SIZE, ARA_SIZE) + 2f * CONTAINER_PAD;

        var hudGO = MakeRect("HUDPanel", cvGO.transform);
        var hudRt = hudGO.GetComponent<RectTransform>();
        hudRt.anchorMin        = new Vector2(0f, 1f);
        hudRt.anchorMax        = new Vector2(0f, 1f);
        hudRt.pivot            = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(HUD_PAD_X, -HUD_PAD_Y);
        hudRt.sizeDelta        = new Vector2(hudW, hudH);

        // ── Container: carga el sprite directamente; HUDPanel sin fondo propio ─
        Sprite containerSprite = _cfg.araContainer;
#if UNITY_EDITOR
        // Fallback directo: si el config no tiene el sprite, cargarlo ahora
        if (containerSprite == null)
            containerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI/Sprites/Hud/Container/Container.png");
#endif
        Debug.Log($"[PlayerHUD] Container sprite: {(containerSprite != null ? containerSprite.name : "NULL — container no visible")}");

        var cSprImg = hudGO.AddComponent<Image>();  // directamente en HUDPanel, no hijo
        if (containerSprite != null)
        {
            cSprImg.sprite         = containerSprite;
            cSprImg.type           = Image.Type.Simple;
            cSprImg.preserveAspect = false;
            cSprImg.color          = Color.white;
        }
        else
        {
            cSprImg.color = new Color(0.06f, 0.04f, 0.03f, 0.90f);
        }
        cSprImg.raycastTarget = false;

        // ── Retrato de Kael — izquierda del panel ────────────────────────────
        var rawGO = MakeRect("Portrait", hudGO.transform);
        var rawRt = rawGO.GetComponent<RectTransform>();
        rawRt.anchorMin        = new Vector2(0f, 0.5f);
        rawRt.anchorMax        = new Vector2(0f, 0.5f);
        rawRt.pivot            = new Vector2(0f, 0.5f);
        rawRt.anchoredPosition = new Vector2(CONTAINER_PAD, 0f);
        rawRt.sizeDelta        = new Vector2(PORTRAIT_SIZE, PORTRAIT_SIZE);

        _portraitRaw = rawGO.AddComponent<RawImage>();
        _portraitRaw.texture = _portraitRT;

        // ── 5 iconos Ara — a la derecha del retrato ──────────────────────────
        float araStartX = CONTAINER_PAD + PORTRAIT_SIZE + GAP_PORT_ARAS;
        _aras = new AraIcon[5];
        for (int i = 0; i < 5; i++)
        {
            var araGO = MakeRect($"Ara_{i}", hudGO.transform);
            var araRt = araGO.GetComponent<RectTransform>();
            araRt.anchorMin        = new Vector2(0f, 0.5f);
            araRt.anchorMax        = new Vector2(0f, 0.5f);
            araRt.pivot            = new Vector2(0f, 0.5f);
            araRt.anchoredPosition = new Vector2(araStartX + i * (ARA_SIZE + ARA_GAP), 0f);
            araRt.sizeDelta        = new Vector2(ARA_SIZE, ARA_SIZE);

            var araImg = araGO.AddComponent<Image>();
            araImg.preserveAspect = true;
            _aras[i] = new AraIcon(araImg, _cfg);
        }
    }

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // ── AraIcon — máquina de estados Idle→Damage→Low→Death→Dead ──────────────
    sealed class AraIcon
    {
        enum Phase { Idle, Damage, Low, Death, Dead }
        const float FPS = 8f;

        readonly Image _img;
        Phase _phase = Phase.Idle;
        int   _frame;
        float _timer;

        public AraIcon(Image img, PlayerHUDConfig cfg) { _img = img; ForceAlive(cfg); }

        public void ForceAlive(PlayerHUDConfig cfg)
        {
            _phase = Phase.Idle; _frame = 0; _timer = 0f;
            SetSprite(cfg?.araIdle, 0);
        }

        public void ForceDead(PlayerHUDConfig cfg)
        {
            _phase = Phase.Dead; _frame = 0; _timer = 0f;
            if (cfg?.araDeath != null && cfg.araDeath.Length > 0)
                SetSprite(cfg.araDeath, cfg.araDeath.Length - 1);
        }

        public void TriggerDeath()
        {
            if (_phase != Phase.Idle) return;
            _phase = Phase.Damage; _frame = 0; _timer = 0f;
        }

        public void Revive(PlayerHUDConfig cfg, bool alive)
        {
            if (alive) ForceAlive(cfg); else ForceDead(cfg);
        }

        public void Tick(float dt, PlayerHUDConfig cfg)
        {
            if (_img == null || cfg == null) return;
            _timer += dt;
            if (_timer < 1f / FPS) return;
            _timer -= 1f / FPS;

            switch (_phase)
            {
                case Phase.Idle:
                    _frame = (_frame + 1) % Len(cfg.araIdle);
                    SetSprite(cfg.araIdle, _frame);
                    break;
                case Phase.Damage:
                    _frame++;
                    if (_frame >= Len(cfg.araDamage)) { _phase = Phase.Low;   _frame = 0; SetSprite(cfg.araLow,   0); }
                    else SetSprite(cfg.araDamage, _frame);
                    break;
                case Phase.Low:
                    _frame++;
                    if (_frame >= Len(cfg.araLow))    { _phase = Phase.Death; _frame = 0; SetSprite(cfg.araDeath, 0); }
                    else SetSprite(cfg.araLow, _frame);
                    break;
                case Phase.Death:
                    _frame++;
                    if (_frame >= Len(cfg.araDeath))  { _phase = Phase.Dead; _frame = Len(cfg.araDeath) - 1; }
                    SetSprite(cfg.araDeath, Mathf.Clamp(_frame, 0, Len(cfg.araDeath) - 1));
                    break;
                case Phase.Dead:
                    if (cfg.araDeath != null && cfg.araDeath.Length > 0)
                        SetSprite(cfg.araDeath, cfg.araDeath.Length - 1);
                    break;
            }
        }

        void SetSprite(Sprite[] arr, int idx)
        {
            if (_img == null || arr == null || arr.Length == 0) return;
            _img.sprite = arr[Mathf.Clamp(idx, 0, arr.Length - 1)];
        }

        static int Len(Sprite[] a) => a == null || a.Length == 0 ? 1 : a.Length;
    }
}
