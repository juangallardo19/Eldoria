using UnityEngine;

// Mueve el fondo con efecto parallax garantizando que siempre cubra la cámara.
// Fórmula: bg = cam * (1-factor) + origin * factor
//   factor=0 → sigue cámara exacto  |  factor=1 → fondo totalmente estático
//   factor≈0.12 → fondo viaja al 88% de la velocidad de la cámara (parallax sutil de cielo).
// Patrón: Observer — reacciona a la posición de la cámara cada frame en LateUpdate.
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("X: 0=sigue cámara, 1=estático. 0.12 = parallax sutil.")]
    [Range(0f, 1f)]
    public float parallaxFactor  = 0.12f;

    [Tooltip("Y: 0=sigue cámara, 1=estático. Mantener igual o menor que X.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0.12f;

    private Camera  _cam;
    private Vector3 _originPos;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _cam       = Camera.main;
        _originPos = transform.position;
    }

    void LateUpdate()
    {
        if (_cam == null) return;
        Vector3 cam = _cam.transform.position;
        transform.position = new Vector3(
            cam.x * (1f - parallaxFactor)  + _originPos.x * parallaxFactor,
            cam.y * (1f - parallaxFactorY) + _originPos.y * parallaxFactorY,
            _originPos.z);
    }
}
