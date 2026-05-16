using UnityEngine;
using UnityEngine.Video;

/// Singleton que mantiene el VideoPlayer del fondo entre escenas.
///
/// Patrón Singleton con DontDestroyOnLoad — igual que AudioManager.
/// El VideoPlayer renderiza siempre a la misma RenderTexture; cualquier
/// RawImage en cualquier escena que use esa textura ve el cuadro actual
/// sin reiniciar el video.
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
        _vp.playOnAwake     = false; // Controlamos el Play manualmente en Start
        _vp.isLooping       = true;
        _vp.audioOutputMode = VideoAudioOutputMode.None;

        // renderMode no se puede cambiar mientras el video está reproduciendo,
        // por eso desactivamos playOnAwake y configuramos la RT antes de Play().
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
}
