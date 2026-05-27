using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections;

// State Machine — each slot is self-contained; clicking acts directly.
// Observer     — restart/delete buttons react to the slot's state.
public class SlotsScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button          cardButton;      // click → new game or continue
        public GameObject      emptyState;      // "?" visual, active when empty
        public GameObject      occupiedState;   // silhouette/image, active when filled
        public TextMeshProUGUI slotTitleText;   // "Slot X" (empty) or zone+time (filled)
        public TextMeshProUGUI subtitleText;    // "Nueva partida" or "Continuar partida"
        public TextMeshProUGUI statusText;      // "Vacío" below the card (empty only)
        public Button          restartButton;   // restart game (filled only)
        public Button          deleteButton;    // delete game (filled only)
    }

    [SerializeField] private SlotUI[] slots = new SlotUI[4];

    [Header("Sprites — Empty slot")]
    [SerializeField] private Sprite _sprEmptyNormal;
    [SerializeField] private Sprite _sprEmptyHover;
    [SerializeField] private Sprite _sprEmptyPress;

    [Header("Sprites — Filled slot")]
    [SerializeField] private Sprite _sprFilledNormal;
    [SerializeField] private Sprite _sprFilledHover;
    [SerializeField] private Sprite _sprFilledPress;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("Background video")]
    [SerializeField] private VideoClip slotsBgClip;

    [Header("Background")]
    [SerializeField] private RawImage backgroundImage;

    [Header("Audio")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Confirm panel")]
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
        // Auto-detect backgroundImage if the Inspector left it unwired
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

    // ── Card content ──────────────────────────────────────────────────────────
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
            if (slots[idx].slotTitleText != null)
                slots[idx].slotTitleText.text = $"{_saves[idx].zoneName}  {time}";
            if (slots[idx].subtitleText  != null)
                slots[idx].subtitleText.text  = "Continuar partida";
        }
        else
        {
            if (slots[idx].slotTitleText != null)
                slots[idx].slotTitleText.text = $"Slot {idx + 1}";
            if (slots[idx].subtitleText  != null)
                slots[idx].subtitleText.text  = "Nueva partida";
        }
    }

    // ── Card actions ──────────────────────────────────────────────────────────
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

    // ── Game sessions ─────────────────────────────────────────────────────────
    private void StartNewGame(int slot)
    {
        // Clear visited map zones so the new game starts with no discoveries
        WorldMapController.ClearAllVisited();

        // Explicit health=5 — prevents inheriting lives from a previous session if the DDOL
        // GameSaveController flushes before CrystalRespawnManager reads the save.
        var data = new SaveData { isEmpty = false, slotName = $"Partida {slot + 1}", zoneName = "Inicio", health = 5 };
        SaveManager.Instance?.Save(slot, data);
        SaveManager.Instance?.SelectSlot(slot);
        CrystalRespawnManager.InvalidatePersistedLives();

        // Reset session counters on the DDOL GameSaveController so accumulated time
        // from the previous session doesn't carry over to the new game.
        GameSaveController.Instance?.ResetForNewGame();

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(EldoriaSceneNames.Intro);
        else SceneManager.LoadScene(EldoriaSceneNames.Intro);
    }

    private void ContinueGame(int slot)
    {
        var data = SaveManager.Instance != null ? SaveManager.Instance.Load(slot) : null;
        SaveManager.Instance?.SelectSlot(slot);
        CrystalRespawnManager.InvalidatePersistedLives();

        // sanctuaryScene is used only for death respawn (CrystalRespawnManager),
        // not to determine where to load the save — that is decided by sceneName.
        PlayerSpawnManager.NextSpawnId = "default";

        string scene = (data != null && !string.IsNullOrEmpty(data.sceneName))
            ? data.sceneName : EldoriaSceneNames.HV01_Interior;

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    private void OnBack()
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(EldoriaSceneNames.MainMenu);
        else SceneManager.LoadScene(EldoriaSceneNames.MainMenu);
    }

    // ── Background video ──────────────────────────────────────────────────────
    // Dual-path: if coming from MainMenu uses BackgroundVideoManager (Singleton);
    // if the scene opens directly, spins up a local VideoPlayer.
    private void SetupBackground()
    {
        if (BackgroundVideoManager.Instance != null)
        {
            // Assign the RT to the RawImage directly (don't wait for BackgroundVideoDisplay)
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
            // No BackgroundVideoManager — local VideoPlayer on the same GO as the RawImage
            var vp = backgroundImage.gameObject.GetComponent<VideoPlayer>();
            if (vp == null) vp = backgroundImage.gameObject.AddComponent<VideoPlayer>();
            vp.isLooping       = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.clip            = slotsBgClip;

            var rt = new RenderTexture(Screen.width, Screen.height, 0);
            rt.Create();
            vp.targetTexture        = rt;
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
