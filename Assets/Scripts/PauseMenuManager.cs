using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Singleton + Observer + State — global pause menu persisting across scenes.
// The UI (Canvas/buttons) is built by the "Eldoria/Add Pause Menu" editor script
// and serialized in MainMenu.unity, editable from the Unity Inspector.
// Pattern Observer: SceneManager.sceneLoaded detects whether the new scene has a Player.
// Pattern State: _isPaused + ShowButtons/ShowConfirm manage the sub-panels.
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    // Scene to return to when the player closes Settings from the pause menu.
    // Read by SettingsManager in its Back button handler.
    public static string ReturnScene { get; set; }

    // UI hierarchy references — assigned by the editor script.
    // To resize: open MainMenu.unity → Hierarchy → PauseMenuManager → Overlay → Container.
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject buttonsGroup;
    [SerializeField] private GameObject confirmGroup;

    private bool _isPaused;
    private bool _canPause;

    public bool IsPaused => _isPaused;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Persist the MainMenu EventSystem so buttons work in subsequent scenes
        var es = FindObjectOfType<EventSystem>();
        if (es != null) DontDestroyOnLoad(es.gameObject);

        overlay?.SetActive(false);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isPaused) Resume();

        // Cursor: visible in menus, hidden during gameplay (CheckForPlayer confirms it)
        if (scene.name == EldoriaSceneNames.MainMenu)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }

        StartCoroutine(CheckForPlayer());
    }

    // Waits one frame for PlayerSpawnManager to place the player
    IEnumerator CheckForPlayer()
    {
        yield return null;
        _canPause = FindObjectOfType<PlayerController>() != null;

        // Hide cursor during gameplay; show it in menus
        Cursor.visible   = !_canPause;
        Cursor.lockState = _canPause ? CursorLockMode.Confined : CursorLockMode.None;
    }

    void Update()
    {
        if (!_canPause) return;
        if (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    // Auto-pause on focus loss (alt-tab / click outside window)
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _canPause && !_isPaused)
            Pause();
    }

    // ── State ─────────────────────────────────────────────────────────────────
    public void Toggle() { if (_isPaused) Resume(); else Pause(); }

    void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        overlay?.SetActive(true);
        ShowButtons();
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        overlay?.SetActive(false);

        if (_canPause)
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    // ── Button callbacks (invoked from Button.onClick in the Inspector) ───────
    // IMPORTANT: do NOT rename these methods — their names are stored as strings
    // in serialized scene YAML and renaming them silently breaks the buttons.
    public void OnContinuar() => Resume();

    public void OnAjustes()
    {
        ReturnScene = SceneManager.GetActiveScene().name;
        Resume();
        // Going to Settings: cursor must be visible for menu navigation
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        SceneFader.Instance?.LoadScene(EldoriaSceneNames.Settings);
    }

    public void OnSalir()
    {
        buttonsGroup?.SetActive(false);
        confirmGroup?.SetActive(true);
    }

    public void OnConfirmarSalir()
    {
        // Stop music BEFORE the fade so it doesn't keep playing during the transition
        AudioManager.Instance?.StopMusic();
        Resume();
        SceneFader.Instance?.LoadScene(EldoriaSceneNames.MainMenu);
    }

    public void OnCancelarSalir() => ShowButtons();

    void ShowButtons()
    {
        buttonsGroup?.SetActive(true);
        confirmGroup?.SetActive(false);
    }
}
