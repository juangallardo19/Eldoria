using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections;

// State Machine — cada slot es autónomo; hacer clic actúa directamente.
// Observer     — los botones de restart/delete reaccionan al estado del slot.
public class SlotsScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button          cardButton;      // clic → nueva partida o continuar
        public GameObject      emptyState;      // "?" visual, activo cuando vacío
        public GameObject      occupiedState;   // silueta/imagen, activo cuando lleno
        public TextMeshProUGUI slotTitleText;   // "Slot X" (vacío) o zona+tiempo (lleno)
        public TextMeshProUGUI subtitleText;    // "Nueva partida" o "Continuar partida"
        public TextMeshProUGUI statusText;      // "Vacío" debajo de la tarjeta (solo vacío)
        public Button          restartButton;   // reiniciar partida (solo lleno)
        public Button          deleteButton;    // eliminar partida (solo lleno)
    }

    [SerializeField] private SlotUI[] slots = new SlotUI[4];

    [Header("Sprites — Slot vacío")]
    [SerializeField] private Sprite _sprEmptyNormal;
    [SerializeField] private Sprite _sprEmptyHover;
    [SerializeField] private Sprite _sprEmptyPress;

    [Header("Sprites — Slot lleno")]
    [SerializeField] private Sprite _sprFilledNormal;
    [SerializeField] private Sprite _sprFilledHover;
    [SerializeField] private Sprite _sprFilledPress;

    [Header("Navegación")]
    [SerializeField] private Button backButton;

    [Header("Video fondo")]
    [SerializeField] private VideoClip slotsBgClip;

    [Header("Fondo")]
    [SerializeField] private RawImage backgroundImage;

    [Header("Audio")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Panel confirmación")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYes;
    [SerializeField] private Button confirmNo;

    private readonly SaveData[] _saves = new SaveData[4];
    private int _pendingActionSlot = -1;
    private bool _pendingIsRestart;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Auto-detectar backgroundImage si el inspector no lo tiene cableado
        if (backgroundImage == null)
        {
            var bgT = transform.Find("Background");
            if (bgT != null) backgroundImage = bgT.GetComponent<RawImage>();
        }

        AudioManager.Instance?.StopMusic();
        if (ambienceSource != null && ambienceSource.clip != null)
            ambienceSource.Play();

        SetupBackground();

        if (confirmPanel != null) confirmPanel.SetActive(false);

        backButton?.onClick.AddListener(OnBack);
        confirmYes?.onClick.AddListener(OnConfirmYes);
        confirmNo ?.onClick.AddListener(() => confirmPanel?.SetActive(false));

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            _saves[i] = SaveManager.Instance != null
                ? SaveManager.Instance.Load(idx)
                : new SaveData();

            ApplySlotSprites(idx);
            RefreshSlotUI(idx);

            if (slots[i].cardButton != null)
            {
                var nav = slots[i].cardButton.navigation;
                nav.mode = Navigation.Mode.None;
                slots[i].cardButton.navigation = nav;
                slots[i].cardButton.onClick.AddListener(() => OnCardClick(idx));
            }

            if (slots[i].restartButton != null)
                slots[i].restartButton.onClick.AddListener(() => OnRestartClick(idx));

            if (slots[i].deleteButton != null)
                slots[i].deleteButton.onClick.AddListener(() => OnDeleteClick(idx));
        }
    }

    // ── Sprites ───────────────────────────────────────────────────────────────
    private void ApplySlotSprites(int idx)
    {
        if (slots[idx].cardButton == null) return;
        bool filled = !_saves[idx].isEmpty;

        slots[idx].cardButton.image.sprite = filled ? _sprFilledNormal : _sprEmptyNormal;
        slots[idx].cardButton.transition   = Selectable.Transition.SpriteSwap;
        slots[idx].cardButton.spriteState  = new SpriteState
        {
            highlightedSprite = filled ? _sprFilledHover : _sprEmptyHover,
            pressedSprite     = filled ? _sprFilledPress : _sprEmptyPress,
            selectedSprite    = filled ? _sprFilledHover : _sprEmptyHover,
        };
    }

    // ── Contenido de tarjeta ─────────────────────────────────────────────────
    private void RefreshSlotUI(int idx)
    {
        bool filled = !_saves[idx].isEmpty;

        slots[idx].emptyState   ?.SetActive(!filled);
        slots[idx].occupiedState?.SetActive(filled);
        slots[idx].statusText   ?.gameObject.SetActive(!filled);
        slots[idx].restartButton?.gameObject.SetActive(filled);
        slots[idx].deleteButton ?.gameObject.SetActive(filled);

        if (filled)
        {
            string time = FormatTime(_saves[idx].playTimeSeconds);
            if (slots[idx].slotTitleText  != null)
                slots[idx].slotTitleText.text  = $"{_saves[idx].zoneName}  {time}";
            if (slots[idx].subtitleText   != null)
                slots[idx].subtitleText.text   = "Continuar partida";
        }
        else
        {
            if (slots[idx].slotTitleText != null)
                slots[idx].slotTitleText.text  = $"Slot {idx + 1}";
            if (slots[idx].subtitleText  != null)
                slots[idx].subtitleText.text   = "Nueva partida";
        }
    }

    // ── Acciones de tarjeta ──────────────────────────────────────────────────
    private void OnCardClick(int idx)
    {
        if (_saves[idx].isEmpty) StartNewGame(idx);
        else                     ContinueGame(idx);
    }

    private void OnRestartClick(int idx)
    {
        _pendingActionSlot = idx;
        _pendingIsRestart  = true;
        ShowConfirm($"¿Reiniciar la partida del Slot {idx + 1}?\nSe perderá todo el progreso.");
    }

    private void OnDeleteClick(int idx)
    {
        _pendingActionSlot = idx;
        _pendingIsRestart  = false;
        ShowConfirm($"¿Eliminar la partida del Slot {idx + 1}?");
    }

    private void ShowConfirm(string msg)
    {
        if (confirmText != null) confirmText.text = msg;
        confirmPanel?.SetActive(true);
    }

    private void OnConfirmYes()
    {
        if (_pendingActionSlot < 0) return;
        confirmPanel?.SetActive(false);

        if (_pendingIsRestart)
        {
            SaveManager.Instance?.Delete(_pendingActionSlot);
            StartNewGame(_pendingActionSlot);
        }
        else
        {
            SaveManager.Instance?.Delete(_pendingActionSlot);
            // Only clear global map data when deleting the currently active slot;
            // wiping it for a different slot would erase the active session's discoveries.
            if (SaveManager.ActiveSlot == _pendingActionSlot)
                WorldMapController.ClearAllVisited();
            _saves[_pendingActionSlot] = new SaveData();
            ApplySlotSprites(_pendingActionSlot);
            RefreshSlotUI(_pendingActionSlot);
        }
        _pendingActionSlot = -1;
    }

    // ── Partidas ─────────────────────────────────────────────────────────────
    private void StartNewGame(int slot)
    {
        // Limpiar zonas visitadas del mapa para que la nueva partida empiece sin descubrimientos
        WorldMapController.ClearAllVisited();

        // health=5 explícito — evita heredar vidas de la partida anterior si el DDOL
        // GameSaveController hace Flush antes de que CrystalRespawnManager lea el save.
        var data = new SaveData { isEmpty = false, slotName = $"Partida {slot + 1}", zoneName = "Inicio", health = 5 };
        SaveManager.Instance?.Save(slot, data);
        SaveManager.Instance?.SelectSlot(slot);
        CrystalRespawnManager.InvalidatePersistedLives();

        // Resetear contadores de sesión del DDOL GameSaveController para que el tiempo
        // acumulado de la partida anterior no se sume al nuevo juego.
        GameSaveController.Instance?.ResetForNewGame();

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("Intro");
        else SceneManager.LoadScene("Intro");
    }

    private void ContinueGame(int slot)
    {
        var data = SaveManager.Instance != null ? SaveManager.Instance.Load(slot) : null;
        SaveManager.Instance?.SelectSlot(slot);
        CrystalRespawnManager.InvalidatePersistedLives();

        // sanctuaryScene se usa solo para el respawn tras muerte (CrystalRespawnManager),
        // no para determinar dónde cargar la partida — eso lo decide sceneName.
        PlayerSpawnManager.NextSpawnId = "default";

        string scene = (data != null && !string.IsNullOrEmpty(data.sceneName))
            ? data.sceneName : "HV01_Interior";

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    private void OnBack()
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    // ── Background video ──────────────────────────────────────────────────────
    // Dual-path: si viene de MainMenu usa BackgroundVideoManager (Singleton);
    // si abre la escena directamente, levanta un VideoPlayer local.
    private void SetupBackground()
    {
        if (BackgroundVideoManager.Instance != null)
        {
            // Asignar la RT al RawImage directamente (no esperar a BackgroundVideoDisplay)
            var bvp = BackgroundVideoManager.Instance.GetComponent<VideoPlayer>();
            if (bvp != null && backgroundImage != null)
            {
                if (bvp.targetTexture != null)
                    backgroundImage.texture = bvp.targetTexture;
            }
            BackgroundVideoManager.Instance.SwitchClip(slotsBgClip);
        }
        else if (backgroundImage != null && slotsBgClip != null)
        {
            // Sin BackgroundVideoManager — VideoPlayer local en el mismo GO del RawImage
            var vp = backgroundImage.gameObject.GetComponent<VideoPlayer>();
            if (vp == null) vp = backgroundImage.gameObject.AddComponent<VideoPlayer>();
            vp.isLooping       = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.clip            = slotsBgClip;

            var rt = new RenderTexture(Screen.width, Screen.height, 0);
            rt.Create();
            vp.targetTexture      = rt;
            backgroundImage.texture = rt;
            vp.Play();
        }
    }

    static string FormatTime(float s)
    {
        int h = (int)(s / 3600), m = (int)(s % 3600 / 60), sec = (int)(s % 60);
        return $"{h:D2}:{m:D2}:{sec:D2}";
    }
}
