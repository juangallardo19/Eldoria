using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Pattern: Command  — encapsulates the "go to the next scene" action (skip or natural end).
//          Observer — VideoPlayer fires loopPointReached; IntroVideoManager reacts.
public class IntroVideoManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage    videoDisplay;

    [Header("Subtitles")]
    [SerializeField] private GameObject      subtitleBox;   // root panel (background Image)
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private SubtitleData    subtitleData;

    [Header("Skip UI")]
    [SerializeField] private TextMeshProUGUI skipHint;     // "[ ESPACIO / Z ] Saltar"

    [Header("Destination scene")]
    [SerializeField] private string nextScene = EldoriaSceneNames.HV01_Interior;

    [Header("Skip")]
    [SerializeField] private float skipHoldSeconds = 1.2f; // hold duration required to skip

    // Reuse the BackgroundVideoManager RenderTexture if it exists; otherwise create a
    // local one so we don't depend on the background singleton.
    private RenderTexture _rt;
    private bool          _exiting;
    private float         _holdTimer;
    private int           _lastEntryIdx = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Music should only play in MainMenu and Settings
        AudioManager.Instance?.StopMusic();

        if (subtitleBox != null) subtitleBox.SetActive(false);
        if (skipHint    != null) UpdateSkipHint(0f);

        // RenderTexture for the VideoPlayer
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

    // ── Subtitles ─────────────────────────────────────────────────────────
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

    // ── Skip input (hold or press) ────────────────────────────────────────
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

    // ── Video end ─────────────────────────────────────────────────────────
    private void OnVideoEnd(VideoPlayer vp) => ExitIntro();

    // ── Transition to the next scene ──────────────────────────────────────
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
