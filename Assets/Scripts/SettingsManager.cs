using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// Manages the Eldoria Options screen.
///
/// Applied patterns:
///   Observer   — Static events notify other systems of changes.
///   State      — ShowPanel() is a 4-state machine (active tab).
public class SettingsManager : MonoBehaviour
{
    // ── Observer events ───────────────────────────────────────────────────
    public static event System.Action<float> OnMusicVolumeChanged;
    public static event System.Action<float> OnSFXVolumeChanged;
    public static event System.Action<bool>  OnFullscreenChanged;

    // ── Dynamic title ─────────────────────────────────────────────────────
    [Header("Title")]
    [SerializeField] private TMP_Text panelTitleLabel;

    // ── Main panels ───────────────────────────────────────────────────────
    [Header("Panels")]
    [SerializeField] private GameObject graficosPanel;
    [SerializeField] private GameObject sonidoPanel;
    [SerializeField] private GameObject controlesPanel;
    [SerializeField] private GameObject ajustesPanel;

    // ── Tab buttons ───────────────────────────────────────────────────────
    [Header("Tabs")]
    [SerializeField] private Button graficosTabButton;
    [SerializeField] private Button sonidoTabButton;
    [SerializeField] private Button controlesTabButton;
    [SerializeField] private Button ajustesTabButton;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabNormalSprite;

    // ── Graphics — Display ────────────────────────────────────────────────
    [Header("Graphics · Display")]
    [SerializeField] private SelectionControl resolutionSelector;
    [SerializeField] private SelectionControl screenModeSelector;   // Fullscreen / Windowed / Borderless
    [SerializeField] private SelectionControl fpsSelector;          // 30 / 60 / 120 / Unlimited
    [SerializeField] private Toggle           vsyncToggle;
    [SerializeField] private SelectionControl qualitySelector;

    // ── Graphics — Visual ─────────────────────────────────────────────────
    [Header("Graphics · Visual")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private Slider saturationSlider;

    // ── Graphics — Visual Accessibility ──────────────────────────────────
    [Header("Graphics · Accessibility")]
    [SerializeField] private Toggle           colorBlindToggle;
    [SerializeField] private GameObject       colorBlindOptionsGroup; // shown only when toggle is ON
    [SerializeField] private SelectionControl colorBlindTypeSelector;  // Protanopia / Deuteranopia / Tritanopia
    [SerializeField] private Slider           colorBlindIntensitySlider;

    // ── Audio ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider voicesSlider;
    [SerializeField] private Slider uiSlider;

    // ── General settings ──────────────────────────────────────────────────
    [Header("General settings")]
    [SerializeField] private SelectionControl languageSelector;
    [SerializeField] private Toggle           showFpsToggle;

    // ── Navigation ────────────────────────────────────────────────────────
    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private static readonly string[] TabNames = { "GRÁFICOS", "SONIDO", "CONTROLES", "AJUSTES" };
    private static readonly FullScreenMode[] ScreenModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed
    };

    // Fixed resolutions: only the three used by the game
    private static readonly (int w, int h)[] FixedResolutions =
    {
        (1920, 1080),
        (1600, 900),
        (1200, 675)
    };

    private static readonly int[] FpsValues = { 30, 60, 120, 144, -1 };

    private int  _currentTabIndex;
    private bool _initialized;

    // ── Observer: language ────────────────────────────────────────────────
    void OnEnable()  => LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.OnLanguageChanged -= OnLanguageChanged;

    void OnLanguageChanged(string _)
    {
        RefreshLocalizedSelectorOptions();
        if (_initialized)
            ShowPanel(PanelAtIndex(_currentTabIndex), TabButtonAtIndex(_currentTabIndex), _currentTabIndex);
    }

    private void RefreshLocalizedSelectorOptions()
    {
        if (screenModeSelector != null)
        {
            int v = screenModeSelector.value;
            screenModeSelector.SetOptions(new List<string>
            {
                LocalizationManager.Get("Pantalla completa"),
                LocalizationManager.Get("Sin bordes"),
                LocalizationManager.Get("Ventana")
            });
            screenModeSelector.value = v;
        }
        if (fpsSelector != null)
        {
            int v = fpsSelector.value;
            fpsSelector.SetOptions(new List<string>
                { "30", "60", "120", "144", LocalizationManager.Get("Sin límite") });
            fpsSelector.value = v;
        }
        if (qualitySelector != null)
        {
            int v = qualitySelector.value;
            qualitySelector.SetOptions(new List<string>
            {
                LocalizationManager.Get("Bajo"),
                LocalizationManager.Get("Medio"),
                LocalizationManager.Get("Alto")
            });
            qualitySelector.value = v;
        }
    }

    private GameObject PanelAtIndex(int i) =>
        i switch { 1 => sonidoPanel, 2 => controlesPanel, 3 => ajustesPanel, _ => graficosPanel };
    private Button TabButtonAtIndex(int i) =>
        i switch { 1 => sonidoTabButton, 2 => controlesTabButton, 3 => ajustesTabButton, _ => graficosTabButton };

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        SetupGraficos();
        SetupSonido();
        SetupAjustes();
        SetupTabs();
        SetupNavigation();
        ShowPanel(graficosPanel, graficosTabButton, 0);
        _initialized = true;
    }

    // ── Graphics ──────────────────────────────────────────────────────────
    private void SetupGraficos()
    {
        // Resolution — fixed list of three options
        if (resolutionSelector != null)
        {
            resolutionSelector.SetOptions(new List<string>
                { "1920 × 1080", "1600 × 900", "1200 × 675" });
            resolutionSelector.value = PlayerPrefs.GetInt(EldoriaPrefsKeys.Resolution, 0);
            resolutionSelector.onValueChanged += idx =>
            {
                var (w, h) = FixedResolutions[Mathf.Clamp(idx, 0, FixedResolutions.Length - 1)];
                Screen.SetResolution(w, h, Screen.fullScreenMode);
                PlayerPrefs.SetInt(EldoriaPrefsKeys.Resolution, idx);
            };
        }

        // Screen mode: Fullscreen / Borderless / Windowed
        if (screenModeSelector != null)
        {
            screenModeSelector.SetOptions(new List<string>
            {
                LocalizationManager.Get("Pantalla completa"),
                LocalizationManager.Get("Sin bordes"),
                LocalizationManager.Get("Ventana")
            });
            screenModeSelector.value = PlayerPrefs.GetInt(EldoriaPrefsKeys.ScreenMode, 0);
            screenModeSelector.onValueChanged += idx =>
            {
                Screen.fullScreenMode = ScreenModes[Mathf.Clamp(idx, 0, 2)];
                PlayerPrefs.SetInt(EldoriaPrefsKeys.ScreenMode, idx);
                OnFullscreenChanged?.Invoke(idx == 0);
            };
        }

        // FPS: 30 / 60 / 120 / 144 / Unlimited
        if (fpsSelector != null)
        {
            fpsSelector.SetOptions(new List<string>
                { "30", "60", "120", "144", LocalizationManager.Get("Sin límite") });
            int savedFps = PlayerPrefs.GetInt(EldoriaPrefsKeys.FPS, 4); // Unlimited by default (index 4)
            fpsSelector.value = savedFps;
            Application.targetFrameRate = FpsValues[Mathf.Clamp(savedFps, 0, FpsValues.Length - 1)];
            fpsSelector.onValueChanged += idx =>
            {
                Application.targetFrameRate = FpsValues[Mathf.Clamp(idx, 0, FpsValues.Length - 1)];
                PlayerPrefs.SetInt(EldoriaPrefsKeys.FPS, idx);
            };
        }

        // VSync toggle
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = PlayerPrefs.GetInt(EldoriaPrefsKeys.VSync, 0) == 1;
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
            vsyncToggle.onValueChanged.AddListener(on =>
            {
                QualitySettings.vSyncCount = on ? 1 : 0;
                PlayerPrefs.SetInt(EldoriaPrefsKeys.VSync, on ? 1 : 0);
            });
        }

        // Quality preset
        if (qualitySelector != null)
        {
            qualitySelector.SetOptions(new List<string>
            {
                LocalizationManager.Get("Bajo"),
                LocalizationManager.Get("Medio"),
                LocalizationManager.Get("Alto")
            });
            qualitySelector.value = PlayerPrefs.GetInt(EldoriaPrefsKeys.Quality, 1);
            qualitySelector.onValueChanged += idx =>
            {
                int[] map = { 0, 2, 5 };
                QualitySettings.SetQualityLevel(map[Mathf.Clamp(idx, 0, 2)], true);
                PlayerPrefs.SetInt(EldoriaPrefsKeys.Quality, idx);
            };
        }

        // Visual — Brightness / Contrast / Saturation
        ScreenEffectsManager.EnsureExists();
        SetupVisualSlider(brightnessSlider,  EldoriaPrefsKeys.Brightness,  0.5f);
        SetupVisualSlider(contrastSlider,    EldoriaPrefsKeys.Contrast,    0.5f);
        SetupVisualSlider(saturationSlider,  EldoriaPrefsKeys.Saturation,  0.5f);
        ApplyVisualEffects();

        // Accessibility — Color blindness
        bool cbActive = PlayerPrefs.GetInt(EldoriaPrefsKeys.ColorBlind, 0) == 1;
        if (colorBlindOptionsGroup != null) colorBlindOptionsGroup.SetActive(cbActive);

        if (colorBlindToggle != null)
        {
            colorBlindToggle.isOn = cbActive;
            colorBlindToggle.onValueChanged.AddListener(active =>
            {
                if (colorBlindOptionsGroup != null) colorBlindOptionsGroup.SetActive(active);
                PlayerPrefs.SetInt(EldoriaPrefsKeys.ColorBlind, active ? 1 : 0);
            });
        }

        if (colorBlindTypeSelector != null)
        {
            colorBlindTypeSelector.SetOptions(new List<string>
                { "Protanopia", "Deuteranopia", "Tritanopia" });
            colorBlindTypeSelector.value = PlayerPrefs.GetInt(EldoriaPrefsKeys.ColorBlindType, 0);
            colorBlindTypeSelector.onValueChanged += idx =>
                PlayerPrefs.SetInt(EldoriaPrefsKeys.ColorBlindType, idx);
        }

        if (colorBlindIntensitySlider != null)
        {
            colorBlindIntensitySlider.minValue = 0f;
            colorBlindIntensitySlider.maxValue = 1f;
            colorBlindIntensitySlider.value = PlayerPrefs.GetFloat(EldoriaPrefsKeys.ColorBlindIntensity, 1f);
            colorBlindIntensitySlider.onValueChanged.AddListener(v =>
                PlayerPrefs.SetFloat(EldoriaPrefsKeys.ColorBlindIntensity, v));
        }
    }

    private void SetupVisualSlider(Slider s, string key, float defaultVal)
    {
        if (s == null) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = PlayerPrefs.GetFloat(key, defaultVal);
        s.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); ApplyVisualEffects(); });
    }

    private void ApplyVisualEffects()
    {
        float b = brightnessSlider  != null ? brightnessSlider.value  : PlayerPrefs.GetFloat(EldoriaPrefsKeys.Brightness,  0.5f);
        float c = contrastSlider    != null ? contrastSlider.value    : PlayerPrefs.GetFloat(EldoriaPrefsKeys.Contrast,    0.5f);
        float s = saturationSlider  != null ? saturationSlider.value  : PlayerPrefs.GetFloat(EldoriaPrefsKeys.Saturation,  0.5f);
        ScreenEffectsManager.Apply(b, c, s);
    }

    // ── Audio ─────────────────────────────────────────────────────────────
    private void SetupSonido()
    {
        SetupVolumeSlider(masterVolumeSlider, EldoriaPrefsKeys.MasterVolume, 1f, vol =>
        {
            // Read slider value from memory, not PlayerPrefs — avoids AudioManager contaminating these keys
            float m = musicSlider != null ? musicSlider.value : GetPref(EldoriaPrefsKeys.MusicVolume, 1f);
            float s = sfxSlider   != null ? sfxSlider.value   : GetPref(EldoriaPrefsKeys.SFXVolume,   1f);
            AudioManager.Instance?.SetMusicVolume(vol * m);
            AudioManager.Instance?.SetSFXVolume(vol * s);
        });

        SetupVolumeSlider(musicSlider, EldoriaPrefsKeys.MusicVolume, 1f, vol =>
        {
            AudioManager.Instance?.SetMusicVolume(vol * GetPref(EldoriaPrefsKeys.MasterVolume, 1f));
            OnMusicVolumeChanged?.Invoke(vol);
        });

        SetupVolumeSlider(sfxSlider, EldoriaPrefsKeys.SFXVolume, 1f, vol =>
        {
            AudioManager.Instance?.SetSFXVolume(vol * GetPref(EldoriaPrefsKeys.MasterVolume, 1f));
            OnSFXVolumeChanged?.Invoke(vol);
        });

        SetupVolumeSlider(voicesSlider, EldoriaPrefsKeys.VoicesVolume, 1f, _ => { });
        SetupVolumeSlider(uiSlider,     EldoriaPrefsKeys.UIVolume,     1f, _ => { });
    }

    private void SetupVolumeSlider(Slider s, string key, float def, System.Action<float> onChange)
    {
        if (s == null) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = PlayerPrefs.GetFloat(key, def);
        s.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(key, v); onChange(v); });
    }

    // ── General settings ──────────────────────────────────────────────────
    private void SetupAjustes()
    {
        if (languageSelector != null)
        {
            languageSelector.SetOptions(new List<string> { "Español", "English" });
            int savedLang = PlayerPrefs.GetInt(EldoriaPrefsKeys.Language, 0);
            languageSelector.value = savedLang;
            LocalizationManager.SetLanguage(savedLang);
            languageSelector.onValueChanged += idx =>
            {
                PlayerPrefs.SetInt(EldoriaPrefsKeys.Language, idx);
                LocalizationManager.SetLanguage(idx);
            };
        }

        if (showFpsToggle != null)
        {
            showFpsToggle.isOn = PlayerPrefs.GetInt(EldoriaPrefsKeys.ShowFPS, 0) == 1;
            showFpsToggle.onValueChanged.AddListener(on => FpsCounter.SetVisible(on));
        }
    }

    // ── State Machine: tabs ───────────────────────────────────────────────
    private void SetupTabs()
    {
        graficosTabButton ?.onClick.AddListener(() => ShowPanel(graficosPanel,  graficosTabButton,  0));
        sonidoTabButton   ?.onClick.AddListener(() => ShowPanel(sonidoPanel,    sonidoTabButton,    1));
        controlesTabButton?.onClick.AddListener(() => ShowPanel(controlesPanel, controlesTabButton, 2));
        ajustesTabButton  ?.onClick.AddListener(() => ShowPanel(ajustesPanel,   ajustesTabButton,   3));
    }

    private void ShowPanel(GameObject target, Button activeTab, int tabIndex)
    {
        _currentTabIndex = tabIndex;

        if (graficosPanel)  graficosPanel .SetActive(target == graficosPanel);
        if (sonidoPanel)    sonidoPanel   .SetActive(target == sonidoPanel);
        if (controlesPanel) controlesPanel.SetActive(target == controlesPanel);
        if (ajustesPanel)   ajustesPanel  .SetActive(target == ajustesPanel);

        UpdateTabSprite(graficosTabButton,  activeTab == graficosTabButton);
        UpdateTabSprite(sonidoTabButton,    activeTab == sonidoTabButton);
        UpdateTabSprite(controlesTabButton, activeTab == controlesTabButton);
        UpdateTabSprite(ajustesTabButton,   activeTab == ajustesTabButton);

        if (panelTitleLabel != null)
            panelTitleLabel.text = LocalizationManager.Get(TabNames[tabIndex]);
    }

    private void UpdateTabSprite(Button btn, bool isActive)
    {
        if (btn == null || tabActiveSprite == null || tabNormalSprite == null) return;
        btn.image.sprite = isActive ? tabActiveSprite : tabNormalSprite;
    }

    // ── Navigation ────────────────────────────────────────────────────────
    private void SetupNavigation()
    {
        backButton?.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            // If coming from an in-game pause, return to that scene
            string target = !string.IsNullOrEmpty(PauseMenuManager.ReturnScene)
                ? PauseMenuManager.ReturnScene
                : EldoriaSceneNames.MainMenu;
            PauseMenuManager.ReturnScene = null;
            if (SceneFader.Instance != null)
                SceneFader.Instance.LoadScene(target);
            else
                SceneManager.LoadScene(target);
        });
    }

    private float GetPref(string key, float def) => PlayerPrefs.GetFloat(key, def);
}
