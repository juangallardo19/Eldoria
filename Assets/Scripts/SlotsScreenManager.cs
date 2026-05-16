using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// State Machine — rastrea qué slot está seleccionado (0-3, -1 = ninguno).
// Observer     — los botones globales (Borrar / Seleccionar) reaccionan al slot activo.
public class SlotsScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject      selectionHighlight; // borde/glow que se activa al seleccionar
        public GameObject      emptyState;         // contenido cuando el slot está vacío
        public GameObject      occupiedState;      // contenido cuando hay partida guardada
        public TextMeshProUGUI slotNumberText;     // "1" "2" "3" "4" — visible en emptyState
        public TextMeshProUGUI zoneText;           // zona — visible en occupiedState
        public TextMeshProUGUI playTimeText;       // HH:MM:SS — visible en occupiedState
        public Button          cardButton;         // clic en la tarjeta → la selecciona
        public Button          newGameButton;      // "NUEVA PARTIDA" dentro de emptyState
        public Button          continueButton;     // "CONTINUAR" dentro de occupiedState
    }

    [SerializeField] private SlotUI[] slots = new SlotUI[4];

    [Header("Botones globales")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button selectButton;

    [Header("Panel confirmación borrar")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private Button     confirmDeleteYes;
    [SerializeField] private Button     confirmDeleteNo;

    private SaveData[] _saves       = new SaveData[4];
    private int        _selectedSlot = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

        backButton  ?.onClick.AddListener(OnBack);
        deleteButton?.onClick.AddListener(OnDelete);
        selectButton?.onClick.AddListener(OnSelect);

        confirmDeleteYes?.onClick.AddListener(ConfirmDelete);
        confirmDeleteNo ?.onClick.AddListener(() => deleteConfirmPanel.SetActive(false));

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            _saves[i] = SaveManager.Instance.Load(idx);
            RefreshSlotUI(idx);
            slots[i].cardButton    ?.onClick.AddListener(() => SelectSlot(idx));
            slots[i].newGameButton ?.onClick.AddListener(() => StartNewGame(idx));
            slots[i].continueButton?.onClick.AddListener(() => ContinueGame(idx));
        }

        RefreshGlobalButtons();
    }

    // ── Render de la tarjeta ──────────────────────────────────────────────
    private void RefreshSlotUI(int idx)
    {
        bool occupied = !_saves[idx].isEmpty;

        slots[idx].emptyState   ?.SetActive(!occupied);
        slots[idx].occupiedState?.SetActive(occupied);
        slots[idx].selectionHighlight?.SetActive(false);

        if (!occupied)
        {
            if (slots[idx].slotNumberText != null)
                slots[idx].slotNumberText.text = (idx + 1).ToString();
        }
        else
        {
            if (slots[idx].zoneText    != null)
                slots[idx].zoneText.text    = _saves[idx].zoneName;
            if (slots[idx].playTimeText != null)
                slots[idx].playTimeText.text = FormatTime(_saves[idx].playTimeSeconds);
        }
    }

    // ── Selección de tarjeta ──────────────────────────────────────────────
    private void SelectSlot(int idx)
    {
        _selectedSlot = idx;

        for (int i = 0; i < slots.Length; i++)
            slots[i].selectionHighlight?.SetActive(i == _selectedSlot);

        RefreshGlobalButtons();
    }

    // ── Botones globales ──────────────────────────────────────────────────
    private void OnBack()
    {
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    private void OnSelect()
    {
        if (_selectedSlot < 0) return;
        if (_saves[_selectedSlot].isEmpty)
            StartNewGame(_selectedSlot);
        else
            ContinueGame(_selectedSlot);
    }

    private void OnDelete()
    {
        if (_selectedSlot < 0 || _saves[_selectedSlot].isEmpty) return;
        deleteConfirmPanel?.SetActive(true);
    }

    private void ConfirmDelete()
    {
        if (_selectedSlot < 0) return;
        SaveManager.Instance.Delete(_selectedSlot);
        deleteConfirmPanel?.SetActive(false);
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene("SlotsScreen");
        else
            SceneManager.LoadScene("SlotsScreen");
    }

    private void RefreshGlobalButtons()
    {
        bool hasSlot     = _selectedSlot >= 0;
        bool isOccupied  = hasSlot && !_saves[_selectedSlot].isEmpty;

        if (deleteButton != null) deleteButton.interactable = isOccupied;
        if (selectButton != null) selectButton.interactable = hasSlot;
    }

    // ── Acciones de partida ───────────────────────────────────────────────
    private void StartNewGame(int slot)
    {
        var data = new SaveData
        {
            isEmpty  = false,
            slotName = $"Partida {slot + 1}",
            zoneName = "Inicio"
        };
        SaveManager.Instance.Save(slot, data);
        SaveManager.Instance.SelectSlot(slot);
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene("Game");
        else
            SceneManager.LoadScene("Game");
    }

    private void ContinueGame(int slot)
    {
        SaveManager.Instance.SelectSlot(slot);
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene("Game");
        else
            SceneManager.LoadScene("Game");
    }

    // ── Utilidades ────────────────────────────────────────────────────────
    private static string FormatTime(float seconds)
    {
        int h = (int)(seconds / 3600);
        int m = (int)(seconds % 3600 / 60);
        int s = (int)(seconds % 60);
        return $"{h:D2}:{m:D2}:{s:D2}";
    }
}
