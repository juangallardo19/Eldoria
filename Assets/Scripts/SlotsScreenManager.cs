using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

// State Machine — rastrea el slot seleccionado (_selectedSlot).
// Observer     — botones globales y label SELECCIONAR reaccionan al estado activo.
public class SlotsScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button          cardButton;    // cubre toda la tarjeta; usa SpriteSwap
        public GameObject      emptyState;    // contenido visible cuando el slot está vacío
        public GameObject      occupiedState; // contenido visible cuando el slot está lleno
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI zoneText;
        public TextMeshProUGUI playTimeText;
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

    [Header("Botones globales")]
    [SerializeField] private Button          backButton;
    [SerializeField] private Button          deleteButton;
    [SerializeField] private Button          selectButton;
    [SerializeField] private TextMeshProUGUI selectButtonLabel;

    [Header("Panel confirmación borrar")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private Button     confirmDeleteYes;
    [SerializeField] private Button     confirmDeleteNo;

    [Header("Audio")]
    [SerializeField] private AudioSource ambienceSource;

    // Array fijo de 4 entradas — acceso O(1) por índice, apropiado para slots de tamaño fijo
    private readonly SaveData[] _saves        = new SaveData[4];
    private          int        _selectedSlot = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        AudioManager.Instance?.StopMusic();
        if (ambienceSource != null && ambienceSource.clip != null)
            ambienceSource.Play();

        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

        backButton  ?.onClick.AddListener(OnBack);
        deleteButton?.onClick.AddListener(OnDelete);
        selectButton?.onClick.AddListener(OnSelect);

        confirmDeleteYes?.onClick.AddListener(ConfirmDelete);
        confirmDeleteNo ?.onClick.AddListener(() => deleteConfirmPanel.SetActive(false));

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            _saves[i] = SaveManager.Instance != null
                ? SaveManager.Instance.Load(idx)
                : new SaveData();

            ApplySlotSprites(idx);
            RefreshSlotUI(idx);

            if (slots[i].cardButton == null) continue;

            // Desactivar navegación con teclado para evitar selección involuntaria
            var nav = slots[i].cardButton.navigation;
            nav.mode = Navigation.Mode.None;
            slots[i].cardButton.navigation = nav;

            slots[i].cardButton.onClick.AddListener(() => SelectSlot(idx));
        }

        RefreshGlobalButtons();
    }

    // ── Sprites ──────────────────────────────────────────────────────────
    // Asigna los sprites correctos (vacío o lleno) al SpriteSwap del botón.
    // Unity maneja hover/press automáticamente; el estado Selected queda
    // persistente vía EventSystem.current.SetSelectedGameObject().
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

    // ── Render contenido tarjeta ──────────────────────────────────────────
    private void RefreshSlotUI(int idx)
    {
        bool occupied = !_saves[idx].isEmpty;
        slots[idx].emptyState   ?.SetActive(!occupied);
        slots[idx].occupiedState?.SetActive(occupied);

        if (!occupied) return;

        if (slots[idx].levelText    != null)
            slots[idx].levelText.text    = $"NIV. {_saves[idx].level}";
        if (slots[idx].zoneText     != null)
            slots[idx].zoneText.text     = _saves[idx].zoneName;
        if (slots[idx].playTimeText != null)
            slots[idx].playTimeText.text = FormatTime(_saves[idx].playTimeSeconds);
    }

    // ── Selección ─────────────────────────────────────────────────────────
    private void SelectSlot(int idx)
    {
        _selectedSlot = idx;
        // EventSystem pone la tarjeta en estado Selected (muestra selectedSprite = hover)
        // y quita Selected del anterior automáticamente.
        EventSystem.current?.SetSelectedGameObject(slots[idx].cardButton?.gameObject);
        RefreshGlobalButtons();
    }

    // ── Botones globales ──────────────────────────────────────────────────
    private void OnBack()
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    private void OnSelect()
    {
        if (_selectedSlot < 0) return;
        if (_saves[_selectedSlot].isEmpty) StartNewGame(_selectedSlot);
        else                               ContinueGame(_selectedSlot);
    }

    private void OnDelete()
    {
        if (_selectedSlot < 0 || _saves[_selectedSlot].isEmpty) return;
        deleteConfirmPanel?.SetActive(true);
    }

    private void ConfirmDelete()
    {
        if (_selectedSlot < 0) return;
        SaveManager.Instance?.Delete(_selectedSlot);
        deleteConfirmPanel?.SetActive(false);
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("SlotsScreen");
        else SceneManager.LoadScene("SlotsScreen");
    }

    private void RefreshGlobalButtons()
    {
        bool hasSlot    = _selectedSlot >= 0;
        bool isOccupied = hasSlot && !_saves[_selectedSlot].isEmpty;

        if (deleteButton != null) deleteButton.interactable = isOccupied;
        if (selectButton != null) selectButton.interactable = hasSlot;

        if (selectButtonLabel != null)
            selectButtonLabel.text = (hasSlot && isOccupied) ? "CONTINUAR" : "NUEVA PARTIDA";
    }

    // ── Acciones de partida ───────────────────────────────────────────────
    private void StartNewGame(int slot)
    {
        var data = new SaveData { isEmpty = false, slotName = $"Partida {slot + 1}", zoneName = "Inicio" };
        SaveManager.Instance?.Save(slot, data);
        SaveManager.Instance?.SelectSlot(slot);
        // Nueva partida → intro cinemática primero; ContinueGame va directo al juego
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("Intro");
        else SceneManager.LoadScene("Intro");
    }

    private void ContinueGame(int slot)
    {
        SaveManager.Instance?.SelectSlot(slot);
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("HV01_Interior");
        else SceneManager.LoadScene("HV01_Interior");
    }

    static string FormatTime(float s)
    {
        int h = (int)(s / 3600), m = (int)(s % 3600 / 60), sec = (int)(s % 60);
        return $"{h:D2}:{m:D2}:{sec:D2}";
    }
}
