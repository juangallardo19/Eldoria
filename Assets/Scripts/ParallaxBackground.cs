using UnityEngine;

// Mueve el fondo con efecto parallax garantizando que siempre cubra la cámara.
// Fórmula: bg = ref * (1-factor) + origin * factor
//   factor=0 → sigue referencia exacto  |  factor=1 → fondo totalmente estático
//   factor≈0.12 → fondo viaja al 88% de la velocidad de la referencia.
// Patrón: Observer — reacciona a la posición de la cámara (o el jugador) cada frame en LateUpdate.
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("X: 0=sigue referencia, 1=estático. 0.12 = parallax sutil.")]
    [Range(0f, 1f)]
    public float parallaxFactor  = 0.12f;

    [Tooltip("Y: 0=sigue referencia, 1=estático. Poner 1.0 para que el cielo no suba/baje.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0.12f;

    [Tooltip("Si está activo, sigue al jugador en vez de la cámara. Útil cuando la cámara es estática (FitRoom).")]
    public bool trackPlayer = false;

    private Camera    _cam;
    private Transform _player;
    private Vector3   _originPos;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _cam       = Camera.main;
        _originPos = transform.position;
        TryFindPlayer();
    }

    void LateUpdate()
    {
        if (_cam == null) return;

        if (trackPlayer && _player == null) TryFindPlayer();

        Vector3 reference = (trackPlayer && _player != null)
            ? _player.position
            : _cam.transform.position;

        transform.position = new Vector3(
            reference.x * (1f - parallaxFactor)  + _originPos.x * parallaxFactor,
            reference.y * (1f - parallaxFactorY) + _originPos.y * parallaxFactorY,
            _originPos.z);
    }

    private void TryFindPlayer()
    {
        if (!trackPlayer) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }
}
