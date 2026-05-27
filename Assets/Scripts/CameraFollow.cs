using UnityEngine;

// Three 2D camera modes for Metroidvania rooms.
// Pattern: Strategy — camera behavior is selected with an enum;
//          the active algorithm can be swapped at runtime (e.g., cutscenes).
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        FitRoom,        // Static: fits orthoSize to background and centers. Ideal for HV01_Interior.
        FollowClamped,  // Follows the player but stays within the background bounds.
        FreeFollow,     // Follows the player with no constraints (debug / infinite rooms).
        FollowBounded   // Follows the player clamped to explicit boundsMin/Max.
    }

    [Header("Mode")]
    public CameraMode mode = CameraMode.FitRoom;

    [Header("Room background (assign Background SR)")]
    public SpriteRenderer roomBackground;

    [Header("Player (only if mode != FitRoom)")]
    public Transform target;

    [Header("Manual bounds (FollowBounded only — ignored when a CameraBoundsZone exists in the scene)")]
    public Vector2 boundsMin = new Vector2(-30f, -12f);
    public Vector2 boundsMax = new Vector2( 30f,   8f);

    // Active zone: assigned in Start (single-zone scenes) or at runtime when the player
    // enters a CameraBoundsZone (multi-zone scenes for irregular rooms).
    private CameraBoundsZone _boundsZone;
    public  CameraBoundsZone ActiveBoundsZone => _boundsZone;

    // Stepped bounds: alternative to CameraBoundsZone for irregular ceilings.
    // Priority: SteppedCameraBounds > CameraBoundsZone > boundsMin/Max manual.
    private SteppedCameraBounds _steppedBounds;

    // Called by CameraBoundsZone when the player enters/exits a zone.
    public void SetActiveBoundsZone(CameraBoundsZone zone) => _boundsZone = zone;

    [Header("Target offset (Follow modes only)")]
    [Tooltip("Shifts the point the camera follows. Y>0 = Kael appears lower on screen.")]
    public Vector2 targetOffset = new Vector2(0f, 3f);

    [Header("Camera size")]
    [Tooltip("If > 0, forces this size at game start (useful in FollowBounded). " +
             "Leave at 0 to use the Camera component value unchanged.")]
    [SerializeField] private float manualOrthoSize = 0f;

    [Tooltip("FitRoom only: when enabled, calculates orthographicSize to fit the background exactly " +
             "(overrides manualOrthoSize). Disable to respect the manual size.")]
    [SerializeField] private bool autoFitOrthoSize = true;

    [Header("Smoothing (Follow modes only)")]
    [SerializeField] private float smoothTime = 0.18f;

    private Camera _cam;
    private Vector3 _velocity;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake() => _cam = GetComponent<Camera>();

    void Start()
    {
        // Auto-assign target if not set in the Inspector
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        // Detect SteppedCameraBounds first; fall back to CameraBoundsZone if absent
        _steppedBounds = FindObjectOfType<SteppedCameraBounds>();
        _boundsZone    = FindObjectOfType<CameraBoundsZone>();

        // Apply manual size before FitToRoom (FitRoom+autoFit may override it)
        if (manualOrthoSize > 0f)
            _cam.orthographicSize = manualOrthoSize;

        if (mode == CameraMode.FitRoom || mode == CameraMode.FollowClamped)
            FitToRoom();
    }

    void LateUpdate()
    {
        switch (mode)
        {
            case CameraMode.FitRoom:
                // Static camera: nothing to do in LateUpdate
                break;

            case CameraMode.FollowClamped:
                FollowClamped();
                break;

            case CameraMode.FreeFollow:
                FreeFollow();
                break;

            case CameraMode.FollowBounded:
                FollowBounded();
                break;
        }
    }

    // ── FitRoom ───────────────────────────────────────────────────────────────
    // Calculates the exact orthographicSize to fill the screen with the background,
    // then centers the camera. Runs once in Start.
    private void FitToRoom()
    {
        if (roomBackground == null)
        {
            Debug.LogWarning("[CameraFollow] roomBackground not assigned — camera unchanged.");
            return;
        }

        var bounds = roomBackground.bounds;

        if (autoFitOrthoSize)
        {
            // Fit height: orthoSize = half sprite height
            _cam.orthographicSize = bounds.size.y / 2f;

            // Expand if aspect ratio would produce black bars on the sides
            float neededOrthoForWidth = (bounds.size.x / 2f) / _cam.aspect;
            if (neededOrthoForWidth > _cam.orthographicSize)
                _cam.orthographicSize = neededOrthoForWidth;
        }

        // Center on the sprite
        Vector3 center = bounds.center;
        center.z = transform.position.z;
        transform.position = center;
    }

    // ── FollowClamped ─────────────────────────────────────────────────────────
    // Follows the player but prevents the camera from showing outside the background.
    private void FollowClamped()
    {
        if (target == null) return;

        if (roomBackground != null)
        {
            var   bounds = roomBackground.bounds;
            float hv     = _cam.orthographicSize;
            float wv     = hv * _cam.aspect;

            float minX = bounds.min.x + wv;
            float maxX = bounds.max.x - wv;
            float minY = bounds.min.y + hv;
            float maxY = bounds.max.y - hv;

            float tx = Mathf.Clamp(target.position.x + targetOffset.x, minX, maxX);
            float ty = Mathf.Clamp(target.position.y + targetOffset.y, minY, maxY);
            var desired = new Vector3(tx, ty, transform.position.z);

            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _velocity, smoothTime);
        }
        else
        {
            FreeFollow();
        }
    }

    // ── FollowBounded ─────────────────────────────────────────────────────────
    // Follows the player clamped to the room bounds.
    // Priority: SteppedCameraBounds > CameraBoundsZone > boundsMin/Max manual.
    private void FollowBounded()
    {
        if (target == null) return;

        float hv = _cam.orthographicSize;
        float wv = hv * _cam.aspect;

        float xMin, xMax, yMin, yMax;

        if (_steppedBounds != null)
        {
            // Dynamic Y bounds based on the player's current X position
            Vector2 yb = _steppedBounds.GetYBoundsAtX(target.position.x);
            xMin = _steppedBounds.XMin;
            xMax = _steppedBounds.XMax;
            yMin = yb.x;
            yMax = yb.y;
        }
        else if (_boundsZone != null)
        {
            var b = _boundsZone.GetWorldBounds();
            xMin = b.min.x; xMax = b.max.x;
            yMin = b.min.y; yMax = b.max.y;
        }
        else
        {
            xMin = boundsMin.x; xMax = boundsMax.x;
            yMin = boundsMin.y; yMax = boundsMax.y;
        }

        float tx = Mathf.Clamp(target.position.x + targetOffset.x, xMin + wv, xMax - wv);
        float ty = Mathf.Clamp(target.position.y + targetOffset.y, yMin + hv, yMax - hv);
        var desired = new Vector3(tx, ty, transform.position.z);

        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref _velocity, smoothTime);
    }

    // ── FreeFollow ────────────────────────────────────────────────────────────
    private void FreeFollow()
    {
        if (target == null) return;
        var desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref _velocity, smoothTime);
    }

    // ── Public API (change mode at runtime, e.g. cutscenes) ──────────────────
    public void SetMode(CameraMode newMode) => mode = newMode;

    public void SnapToTarget()
    {
        if (target == null) return;
        _velocity = Vector3.zero;
        var p = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = p;
    }
}
