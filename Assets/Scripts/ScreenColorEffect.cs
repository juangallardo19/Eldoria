using UnityEngine;

// Built-in RP post-process effect: brightness / contrast / saturation.
// Pattern: Strategy — attach to any Camera and the effect is applied in OnRenderImage.
// ScreenEffectsManager adds/updates this component on the Main Camera each scene load.
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ScreenColorEffect : MonoBehaviour
{
    [Range(-0.5f,  0.5f)] public float brightness = 0f;  // neutral = 0
    [Range(-0.5f,  0.5f)] public float contrast   = 0f;  // neutral = 0
    [Range(-1f,    1f)]   public float saturation  = 0f;  // neutral = 0

    Material _mat;

    void Awake()    => EnsureMaterial();
    void OnEnable() => EnsureMaterial();

    void EnsureMaterial()
    {
        if (_mat != null) return;
        var shader = Shader.Find("Eldoria/ScreenColorEffect");
        if (shader == null)
        {
            Debug.LogWarning("[ScreenColorEffect] Shader 'Eldoria/ScreenColorEffect' not found. " +
                             "Make sure Assets/Shaders/ScreenColorEffect.shader is in the project.");
            return;
        }
        _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (_mat == null) EnsureMaterial();
        if (_mat == null) { Graphics.Blit(src, dst); return; }

        _mat.SetFloat("_Brightness", brightness);
        _mat.SetFloat("_Contrast",   contrast);
        _mat.SetFloat("_Saturation", saturation);
        Graphics.Blit(src, dst, _mat);
    }

    void OnDestroy()
    {
        if (_mat != null) DestroyImmediate(_mat);
    }
}
