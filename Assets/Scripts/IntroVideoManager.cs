using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Command  — encapsula la acción "ir a la siguiente escena" (skip o fin natural).
// Observer — el VideoPlayer notifica loopPointReached; IntroVideoManager reacciona.
public class IntroVideoManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage    videoDisplay;

    [Header("Subtítulos")]
    [SerializeField] private GameObject      subtitleBox;   // panel raíz (Image de fondo)
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private SubtitleData    subtitleData;

    [Header("UI de skip")]
    [SerializeField] private TextMeshProUGUI skipHint;     // "[ ESPACIO / Z ] Saltar"

    [Header("Escena destino")]
    [SerializeField] private string nextScene = "HV01_Interior";

    [Header("Skip")]
    [SerializeField] private float skipHoldSeconds = 1.2f; // tiempo que hay que mantener para saltar

    // Reutilizamos el RenderTexture del BackgroundVideoManager si existe;
    // si no, creamos uno propio para no depender del singleton de fondo.
    private RenderTexture _rt;
    private bool          _exiting;
    private float         _holdTimer;
    private int           _lastEntryIdx = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // La música solo debe sonar en MainMenu y Settings
        AudioManager.Instance?.StopMusic();

        if (subtitleBox != null) subtitleBox.SetActive(false);
        if (skipHint    != null) UpdateSkipHint(0f);

        // RenderTexture para el VideoPlayer
        _rt                        = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture  = _rt;
        if (videoDisplay != null) videoDisplay.texture = _rt;

        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_exiting) return;

        UpdateSubtitles();
        HandleSkipInput();
    }

    // ── Subtítulos ────────────────────────────────────────────────────────
    private void UpdateSubtitles()
    {
        if (subtitleData == null || subtitleData.entries == null) return;

        float t = (float)videoPlayer.time;
        int   found = -1;

        for (int i = 0; i < subtitleData.entries.Length; i++)
        {
            var e = subtitleData.entries[i];
            if (t >= e.startTime && t < e.endTime) { found = i; break; }
        }

        if (found == _lastEntryIdx) return;
        _lastEntryIdx = found;

        bool show = found >= 0;
        if (subtitleBox != null) subtitleBox.SetActive(show);
        if (show && subtitleText != null)
            subtitleText.text = subtitleData.entries[found].text;
    }

    // ── Skip por tecla (mantener o pulsar) ───────────────────────────────
    private void HandleSkipInput()
    {
        bool held = Input.anyKey;

        if (held)
        {
            _holdTimer += Time.deltaTime;
            UpdateSkipHint(_holdTimer / skipHoldSeconds);

            if (_holdTimer >= skipHoldSeconds)
                ExitIntro();
        }
        else
        {
            if (_holdTimer > 0f)
            {
                _holdTimer = 0f;
                UpdateSkipHint(0f);
            }
        }
    }

    private void UpdateSkipHint(float progress)
    {
        if (skipHint == null) return;
        int pct = Mathf.RoundToInt(progress * 100f);
        skipHint.text = progress <= 0f
            ? "[ ESPACIO / Z ]  Mantén para saltar"
            : $"[ ESPACIO / Z ]  Saltando… {pct}%";
    }

    // ── Fin del video ─────────────────────────────────────────────────────
    private void OnVideoEnd(VideoPlayer vp) => ExitIntro();

    // ── Transición a la siguiente escena ──────────────────────────────────
    private void ExitIntro()
    {
        if (_exiting) return;
        _exiting = true;
        videoPlayer.Stop();

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(nextScene);
        else SceneManager.LoadScene(nextScene);
    }

    void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoEnd;
        if (_rt         != null) { _rt.Release(); Destroy(_rt); }
    }
}
