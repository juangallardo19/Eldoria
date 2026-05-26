using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Singleton + Observer + State — menú de pausa global persistente entre escenas.
// La UI (Canvas/botones) se crea desde el editor script "Eldoria/Add Pause Menu"
// y queda serializada en MainMenu.unity, editable desde el Inspector de Unity.
// Patrón Observer: SceneManager.sceneLoaded detecta si la nueva escena tiene Player.
// Patrón State: _isPaused + ShowButtons/ShowConfirm gestionan los sub-paneles.
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    // Escena a la que volver cuando el jugador cierra Settings desde la pausa.
    // SettingsManager lo lee en su botón Back.
    public static string ReturnScene { get; set; }

    // Referencias a la jerarquía UI — asignadas por el editor script.
    // Para editar tamaños: abre MainMenu.unity → Hierarchy → PauseMenuManager → Overlay → Container.
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject buttonsGroup;
    [SerializeField] private GameObject confirmGroup;

    private bool _isPaused;
    private bool _canPause;

    public bool IsPaused => _isPaused;

    // ─── Ciclo de vida ────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Persiste el EventSystem del MainMenu para que los botones funcionen en otras escenas
        var es = FindObjectOfType<EventSystem>();
        if (es != null) DontDestroyOnLoad(es.gameObject);

        overlay?.SetActive(false);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isPaused) Resume();

        // Cursor: visible en menús, oculto durante gameplay (CheckForPlayer lo confirma)
        if (scene.name == "MainMenu")
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }
        // Nota: la música por escena la gestiona ZoneMusicController exclusivamente

        StartCoroutine(CheckForPlayer());
    }

    // Espera un frame para que PlayerSpawnManager coloque al jugador
    IEnumerator CheckForPlayer()
    {
        yield return null;
        _canPause = FindObjectOfType<PlayerController>() != null;

        // Ocultar cursor durante gameplay; mostrarlo en menús
        Cursor.visible   = !_canPause;
        Cursor.lockState = _canPause ? CursorLockMode.Confined : CursorLockMode.None;
    }

    void Update()
    {
        if (!_canPause) return;
        if (WorldMapController.Instance != null && WorldMapController.Instance.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    // Pausa automática al perder el foco (alt-tab / click fuera de la ventana)
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _canPause && !_isPaused)
            Pause();
    }

    // ─── Estado ───────────────────────────────────────────────────────────────
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

    // ─── Acciones de botones (llamadas desde Button.onClick en Inspector) ──────
    public void OnContinuar() => Resume();

    public void OnAjustes()
    {
        ReturnScene = SceneManager.GetActiveScene().name;
        Resume();
        // Yendo a Settings: el cursor debe verse para navegar los menús
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        SceneFader.Instance?.LoadScene("Settings");
    }

    public void OnSalir()
    {
        buttonsGroup?.SetActive(false);
        confirmGroup?.SetActive(true);
    }

    public void OnConfirmarSalir()
    {
        // Detener música ANTES del fade para que no siga sonando durante la transición
        AudioManager.Instance?.StopMusic();
        Resume();
        SceneFader.Instance?.LoadScene("MainMenu");
    }

    public void OnCancelarSalir() => ShowButtons();

    void ShowButtons()
    {
        buttonsGroup?.SetActive(true);
        confirmGroup?.SetActive(false);
    }
}
