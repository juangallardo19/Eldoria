using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Pattern: Strategy — decides in Start() whether a persistent video exists (navigating from
// MainMenu) or there is no manager (scene opened directly in the editor).
// Fallback: if BackgroundVideoManager doesn't exist, creates a local VideoPlayer with fallbackClip.
[RequireComponent(typeof(RawImage))]
public class BackgroundVideoDisplay : MonoBehaviour
{
    [SerializeField] private VideoClip fallbackClip;

    void Start()
    {
        if (BackgroundVideoManager.Instance != null)
        {
            VideoPlayer vp = BackgroundVideoManager.Instance.GetComponent<VideoPlayer>();
            if (vp != null && vp.targetTexture != null)
                ShowTexture(vp.targetTexture);
        }
        else if (fallbackClip != null)
        {
            var rt = new RenderTexture(Screen.width, Screen.height, 0);
            rt.Create();

            var vp = gameObject.AddComponent<VideoPlayer>();
            vp.clip            = fallbackClip;
            vp.renderMode      = VideoRenderMode.RenderTexture;
            vp.targetTexture   = rt;
            vp.isLooping       = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.playOnAwake     = false;
            vp.Play();

            ShowTexture(rt);
        }
    }

    void ShowTexture(Texture tex)
    {
        var img   = GetComponent<RawImage>();
        img.texture = tex;
        var c     = img.color;
        c.a       = 1f;
        img.color = c;
    }
}
