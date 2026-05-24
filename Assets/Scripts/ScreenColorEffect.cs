using UnityEngine;

// Efecto de post-proceso Built-in RP: brillo / contraste / saturación.
// Patrón: Strategy — se adjunta a cualquier Camera y aplica el efecto en OnRenderImage.
// ScreenEffectsManager lo añade/actualiza dinámicamente al Main Camera en cada escena.
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ScreenColorEffect : MonoBehaviour
{
    [Range(-0.5f,  0.5f)] public float brightness = 0f;  // neutro = 0
    [Range(-0.5f,  0.5f)] public float contrast   = 0f;  // neutro = 0
    [Range(-1f,    1f)]   public float saturation  = 0f;  // neutro = 0

    Material _mat;

    void Awake()    => EnsureMaterial();
    void OnEnable() => EnsureMaterial();

    void EnsureMaterial()
    {
        if (_mat != null) return;
        var shader = Shader.Find("Eldoria/ScreenColorEffect");
        if (shader == null)
        {
            Debug.LogWarning("[ScreenColorEffect] Shader 'Eldoria/ScreenColorEffect' no encontrado. " +
                             "Asegúrate de que Assets/Shaders/ScreenColorEffect.shader esté en el proyecto.");
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
