using UnityEngine;

// Moves the background with a parallax effect, guaranteeing it always covers the camera.
// Formula: bg = ref * (1-factor) + origin * factor
//   factor=0 → follows reference exactly  |  factor=1 → background fully static
//   factor≈0.12 → background travels at 88% of the reference speed.
// Pattern: Observer — reacts to the camera (or player) position every frame in LateUpdate.
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("X: 0=follows reference, 1=static. 0.12 = subtle parallax.")]
    [Range(0f, 1f)]
    public float parallaxFactor  = 0.12f;

    [Tooltip("Y: 0=follows reference, 1=static. Use 1.0 to keep the sky from moving vertically.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0.12f;

    [Tooltip("When active, follows the player instead of the camera. Useful when the camera is static (FitRoom).")]
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
