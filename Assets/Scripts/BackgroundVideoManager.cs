using UnityEngine;
using UnityEngine.Video;

// Singleton that keeps the background VideoPlayer alive across scenes.
// Pattern: Singleton + DontDestroyOnLoad — mirrors AudioManager.
// The VideoPlayer always renders to the same RenderTexture; any RawImage
// in any scene using that texture sees the current frame without restarting the video.
[RequireComponent(typeof(VideoPlayer))]
public class BackgroundVideoManager : MonoBehaviour
{
    public static BackgroundVideoManager Instance { get; private set; }

    private VideoPlayer _vp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _vp = GetComponent<VideoPlayer>();
        _vp.playOnAwake     = false; // Play is controlled manually in Start
        _vp.isLooping       = true;
        _vp.audioOutputMode = VideoAudioOutputMode.None;

        // renderMode cannot be changed while the video is playing, so we disable
        // playOnAwake and configure the RT before calling Play().
        if (_vp.targetTexture == null)
        {
            _vp.Stop();
            var rt = new RenderTexture(Screen.width, Screen.height, 0);
            rt.Create();
            _vp.renderMode    = VideoRenderMode.RenderTexture;
            _vp.targetTexture = rt;
        }
    }

    void Start()
    {
        _vp.Play();
    }

    public double CurrentTime => _vp.time;

    public RenderTexture TargetTexture => _vp.targetTexture;

    // Swaps the playing clip (useful for per-scene background videos).
    public void SwitchClip(UnityEngine.Video.VideoClip clip)
    {
        if (clip == null) return;
        _vp.Stop();
        _vp.clip = clip;
        _vp.Play();
    }
}
