using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// Gestiona la pantalla de Opciones de Eldoria.
///
/// Patrones aplicados:
///   Observer   — Eventos estáticos notifican cambios a otros sistemas.
///   State      — ShowPanel() es una máquina de 4 estados (pestaña activa).
public class SettingsManager : MonoBehaviour
{
    // ── Observer ──────────────────────────────────────────────────────────
    public static event System.Action<float> OnMusicVolumeChanged;
    public static event System.Action<float> OnSFXVolumeChanged;
    public static event System.Action<bool>  OnFullscreenChanged;

    // ── Título dinámico ───────────────────────────────────────────────────
    [Header("Título")]
    [SerializeField] private TMP_Text panelTitleLabel;

    // ── Paneles principales ───────────────────────────────────────────────
    [Header("Paneles")]
    [SerializeField] private GameObject graficosPanel;
    [SerializeField] private GameObject sonidoPanel;
    [SerializeField] private GameObject controlesPanel;
    [SerializeField] private GameObject ajustesPanel;

    // ── Botones de pestaña ────────────────────────────────────────────────
    [Header("Pestañas")]
    [SerializeField] private Button graficosTabButton;
    [SerializeField] private Button sonidoTabButton;
    [SerializeField] private Button controlesTabButton;
    [SerializeField] private Button ajustesTabButton;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabNormalSprite;

    // ── Gráficos — Pantalla ───────────────────────────────────────────────
    [Header("Gráficos · Pantalla")]
    [SerializeField] private SelectionControl resolutionSelector;
    [SerializeField] private SelectionControl screenModeSelector;   // Completa / Ventana / Sin bordes
    [SerializeField] private SelectionControl fpsSelector;          // 30 / 60 / 120 / Sin límite
    [SerializeField] private Toggle           vsyncToggle;
    [SerializeField] private SelectionControl qualitySelector;

    // ── Gráficos — Visual ─────────────────────────────────────────────────
    [Header("Gráficos · Visual")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private Slider saturationSlider;

    // ── Gráficos — Accesibilidad Visual ───────────────────────────────────
    [Header("Gráficos · Accesibilidad")]
    [SerializeField] private Toggle           colorBlindToggle;
    [SerializeField] private GameObject       colorBlindOptionsGroup; // se muestra solo si toggle ON
    [SerializeField] private SelectionControl colorBlindTypeSelector;  // Protanopia / Deuteranopia / Tritanopia
    [SerializeField] private Slider           colorBlindIntensitySlider;

    // ── Sonido ────────────────────────────────────────────────────────────
    [Header("Sonido")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider voicesSlider;
    [SerializeField] private Slider uiSlider;

    // ── Ajustes generales ─────────────────────────────────────────────────
    [Header("Ajustes generales")]
    [SerializeField] private SelectionControl languageSelector;

    // ── Navegación ────────────────────────────────────────────────────────
    [Header("Navegación")]
    [SerializeField] private Button backButton;

    // ── PlayerPrefs keys ──────────────────────────────────────────────────
    private const string K_SCREEN_MODE       = "ScreenMode";
    private const string K_FPS               = "FPS";
    private const string K_VSYNC             = "VSync";
    private const string K_QUALITY           = "Quality";
    private const string K_RESOLUTION        = "Resolution";
    private const string K_BRIGHTNESS        = "Brightness";
    private const string K_CONTRAST          = "Contrast";
    private const string K_SATURATION        = "Saturation";
    private const string K_COLOR_BLIND       = "ColorBlind";
    private const string K_COLOR_BLIND_TYPE  = "ColorBlindType";
    private const string K_COLOR_BLIND_INT   = "ColorBlindIntensity";
    private const string K_MASTER_VOL        = "MasterVolume";
    private const string K_MUSIC_VOL         = "MusicVolume";
    private const string K_SFX_VOL           = "SFXVolume";
    private const string K_VOICES_VOL        = "VoicesVolume";
    private const string K_UI_VOL            = "UIVolume";
    private const string K_LANGUAGE          = "Language";

    private static readonly string[] TabNames = { "GRÁFICOS", "SONIDO", "CONTROLES", "AJUSTES" };
    private static readonly FullScreenMode[] ScreenModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed
    };

    // Resoluciones fijas: solo las tres que usa el juego
    private static readonly (int w, int h)[] FixedResolutions =
    {
        (1920, 1080),
        (1600, 900),
        (1200, 675)
    };

    private static readonly int[] FpsValues = { 120, 144, -1 };

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        SetupGraficos();
        SetupSonido();
        SetupAjustes();
        SetupTabs();
        SetupNavigation();
        ShowPanel(graficosPanel, graficosTabButton, 0);
    }

    // ── Gráficos ──────────────────────────────────────────────────────────
    private void SetupGraficos()
    {
        // Resolución — lista fija de tres opciones
        if (resolutionSelector != null)
        {
            resolutionSelector.SetOptions(new List<string>
                { "1920 × 1080", "1600 × 900", "1200 × 675" });
            resolutionSelector.value = PlayerPrefs.GetInt(K_RESOLUTION, 0);
            resolutionSelector.onValueChanged += idx =>
            {
                var (w, h) = FixedResolutions[Mathf.Clamp(idx, 0, FixedResolutions.Length - 1)];
                Screen.SetResolution(w, h, Screen.fullScreenMode);
                PlayerPrefs.SetInt(K_RESOLUTION, idx);
            };
        }

        // Modo de pantalla: Pantalla completa / Sin bordes / Ventana
        if (screenModeSelector != null)
        {
            screenModeSelector.SetOptions(new List<string>
                { "Pantalla completa", "Sin bordes", "Ventana" });
            screenModeSelector.value = PlayerPrefs.GetInt(K_SCREEN_MODE, 0);
            screenModeSelector.onValueChanged += idx =>
            {
                Screen.fullScreenMode = ScreenModes[Mathf.Clamp(idx, 0, 2)];
                PlayerPrefs.SetInt(K_SCREEN_MODE, idx);
                OnFullscreenChanged?.Invoke(idx == 0);
            };
        }

        // FPS: 120 / 144 / Sin límite
        if (fpsSelector != null)
        {
            fpsSelector.SetOptions(new List<string> { "120", "144", "Sin límite" });
            int savedFps = PlayerPrefs.GetInt(K_FPS, 2); // Sin límite por defecto
            fpsSelector.value = savedFps;
            Application.targetFrameRate = FpsValues[Mathf.Clamp(savedFps, 0, FpsValues.Length - 1)];
            fpsSelector.onValueChanged += idx =>
            {
                Application.targetFrameRate = FpsValues[Mathf.Clamp(idx, 0, FpsValues.Length - 1)];
                PlayerPrefs.SetInt(K_FPS, idx);
            };
        }

        // VSync
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = PlayerPrefs.GetInt(K_VSYNC, 0) == 1;
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
            vsyncToggle.onValueChanged.AddListener(on =>
            {
                QualitySettings.vSyncCount = on ? 1 : 0;
                PlayerPrefs.SetInt(K_VSYNC, on ? 1 : 0);
            });
        }

        // Calidad
        if (qualitySelector != null)
        {
            qualitySelector.SetOptions(new List<string> { "Bajo", "Medio", "Alto" });
            qualitySelector.value = PlayerPrefs.GetInt(K_QUALITY, 1);
            qualitySelector.onValueChanged += idx =>
            {
                int[] map = { 0, 2, 5 };
                QualitySettings.SetQualityLevel(map[Mathf.Clamp(idx, 0, 2)], true);
                PlayerPrefs.SetInt(K_QUALITY, idx);
            };
        }

        // Visual — Brillo / Contraste / Saturación
        // (requiere post-procesado para efecto visual; valores guardados en PlayerPrefs)
        SetupVisualSlider(brightnessSlider,  K_BRIGHTNESS,  0.5f);
        SetupVisualSlider(contrastSlider,    K_CONTRAST,    0.5f);
        SetupVisualSlider(saturationSlider,  K_SATURATION,  0.5f);

        // Accesibilidad — Daltonismo
        bool cbActive = PlayerPrefs.GetInt(K_COLOR_BLIND, 0) == 1;
        if (colorBlindOptionsGroup != null) colorBlindOptionsGroup.SetActive(cbActive);

        if (colorBlindToggle != null)
        {
            colorBlindToggle.isOn = cbActive;
            colorBlindToggle.onValueChanged.AddListener(active =>
            {
                if (colorBlindOptionsGroup != null) colorBlindOptionsGroup.SetActive(active);
                PlayerPrefs.SetInt(K_COLOR_BLIND, active ? 1 : 0);
            });
        }

        if (colorBlindTypeSelector != null)
        {
            colorBlindTypeSelector.SetOptions(new List<string>
                { "Protanopia", "Deuteranopia", "Tritanopia" });
            colorBlindTypeSelector.value = PlayerPrefs.GetInt(K_COLOR_BLIND_TYPE, 0);
            colorBlindTypeSelector.onValueChanged += idx =>
                PlayerPrefs.SetInt(K_COLOR_BLIND_TYPE, idx);
        }

        if (colorBlindIntensitySlider != null)
        {
            colorBlindIntensitySlider.minValue = 0f;
            colorBlindIntensitySlider.maxValue = 1f;
            colorBlindIntensitySlider.value = PlayerPrefs.GetFloat(K_COLOR_BLIND_INT, 1f);
            colorBlindIntensitySlider.onValueChanged.AddListener(v =>
                PlayerPrefs.SetFloat(K_COLOR_BLIND_INT, v));
        }
    }

    private void SetupVisualSlider(Slider s, string key, float defaultVal)
    {
        if (s == null) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = PlayerPrefs.GetFloat(key, defaultVal);
        s.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(key, v));
    }

    // ── Sonido ────────────────────────────────────────────────────────────
    private void SetupSonido()
    {
        SetupVolumeSlider(masterVolumeSlider, K_MASTER_VOL, 1f, vol =>
        {
            AudioManager.Instance?.SetMusicVolume(vol * GetPref(K_MUSIC_VOL, 1f));
            AudioManager.Instance?.SetSFXVolume(vol * GetPref(K_SFX_VOL, 1f));
        });

        SetupVolumeSlider(musicSlider, K_MUSIC_VOL, 1f, vol =>
        {
            AudioManager.Instance?.SetMusicVolume(vol * GetPref(K_MASTER_VOL, 1f));
            OnMusicVolumeChanged?.Invoke(vol);
        });

        SetupVolumeSlider(sfxSlider, K_SFX_VOL, 1f, vol =>
        {
            AudioManager.Instance?.SetSFXVolume(vol * GetPref(K_MASTER_VOL, 1f));
            OnSFXVolumeChanged?.Invoke(vol);
        });

        SetupVolumeSlider(voicesSlider, K_VOICES_VOL, 1f, _ => { });
        SetupVolumeSlider(uiSlider,     K_UI_VOL,     1f, _ => { });
    }

    private void SetupVolumeSlider(Slider s, string key, float def, System.Action<float> onChange)
    {
        if (s == null) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = PlayerPrefs.GetFloat(key, def);
        s.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); onChange(v); });
    }

    // ── Ajustes generales ─────────────────────────────────────────────────
    private void SetupAjustes()
    {
        if (languageSelector == null) return;
        languageSelector.SetOptions(new List<string> { "Español", "English" });
        languageSelector.value = PlayerPrefs.GetInt(K_LANGUAGE, 0);
        languageSelector.onValueChanged += idx => PlayerPrefs.SetInt(K_LANGUAGE, idx);
    }

    // ── State Machine: pestañas ───────────────────────────────────────────
    private void SetupTabs()
    {
        graficosTabButton ?.onClick.AddListener(() => ShowPanel(graficosPanel,  graficosTabButton,  0));
        sonidoTabButton   ?.onClick.AddListener(() => ShowPanel(sonidoPanel,    sonidoTabButton,    1));
        controlesTabButton?.onClick.AddListener(() => ShowPanel(controlesPanel, controlesTabButton, 2));
        ajustesTabButton  ?.onClick.AddListener(() => ShowPanel(ajustesPanel,   ajustesTabButton,   3));
    }

    private void ShowPanel(GameObject target, Button activeTab, int tabIndex)
    {
        if (graficosPanel)  graficosPanel .SetActive(target == graficosPanel);
        if (sonidoPanel)    sonidoPanel   .SetActive(target == sonidoPanel);
        if (controlesPanel) controlesPanel.SetActive(target == controlesPanel);
        if (ajustesPanel)   ajustesPanel  .SetActive(target == ajustesPanel);

        UpdateTabSprite(graficosTabButton,  activeTab == graficosTabButton);
        UpdateTabSprite(sonidoTabButton,    activeTab == sonidoTabButton);
        UpdateTabSprite(controlesTabButton, activeTab == controlesTabButton);
        UpdateTabSprite(ajustesTabButton,   activeTab == ajustesTabButton);

        if (panelTitleLabel != null) panelTitleLabel.text = TabNames[tabIndex];
    }

    private void UpdateTabSprite(Button btn, bool isActive)
    {
        if (btn == null || tabActiveSprite == null || tabNormalSprite == null) return;
        btn.image.sprite = isActive ? tabActiveSprite : tabNormalSprite;
    }

    // ── Navegación ────────────────────────────────────────────────────────
    private void SetupNavigation()
    {
        backButton?.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            if (SceneFader.Instance != null)
                SceneFader.Instance.LoadScene("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        });
    }

    private float GetPref(string key, float def) => PlayerPrefs.GetFloat(key, def);
}
