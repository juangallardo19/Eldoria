using UnityEngine;

// Tres modos de cámara 2D para salas de Metroidvania.
// Patrón: Strategy — el comportamiento de cámara se selecciona con un enum;
//          el algoritmo activo puede cambiarse en runtime (ej: cutscenes).
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        FitRoom,        // Estática: ajusta orthoSize al fondo y centra. Ideal para HUB-01.
        FollowClamped,  // Sigue al jugador pero no sale del área del fondo.
        FreeFollow,     // Sigue al jugador sin restricciones (debug / salas infinitas).
        FollowBounded   // Sigue al jugador clampeando a límites explícitos (boundsMin/Max).
    }

    [Header("Modo")]
    public CameraMode mode = CameraMode.FitRoom;

    [Header("Fondo de la sala (asignar Background SR)")]
    public SpriteRenderer roomBackground;

    [Header("Jugador (solo si modo ≠ FitRoom)")]
    public Transform target;

    [Header("Límites manuales (solo FollowBounded — ignorados si hay CameraBoundsZone en la escena)")]
    public Vector2 boundsMin = new Vector2(-30f, -12f);
    public Vector2 boundsMax = new Vector2( 30f,   8f);

    // Zona activa: se asigna en Start (escena con una sola zona) o en runtime cuando el jugador
    // entra en un CameraBoundsZone (escenas con múltiples zonas para salas irregulares).
    private CameraBoundsZone _boundsZone;
    public  CameraBoundsZone ActiveBoundsZone => _boundsZone;

    // Límites escalonados: alternativa a CameraBoundsZone para techos irregulares.
    // Prioridad: SteppedCameraBounds > CameraBoundsZone > boundsMin/Max manual.
    private SteppedCameraBounds _steppedBounds;

    // Llamado por CameraBoundsZone cuando el jugador entra/sale de una zona.
    public void SetActiveBoundsZone(CameraBoundsZone zone) => _boundsZone = zone;

    [Header("Offset del objetivo (solo modo Follow)")]
    [Tooltip("Desplaza el punto que sigue la cámara. Y>0 = Kael aparece más abajo en pantalla.")]
    public Vector2 targetOffset = new Vector2(0f, 3f);

    [Header("Tamaño de cámara")]
    [Tooltip("Si > 0, fuerza este tamaño al iniciar el juego (útil en FollowBounded). " +
             "Déjalo en 0 para usar el valor del componente Camera sin tocarlo.")]
    [SerializeField] private float manualOrthoSize = 0f;

    [Tooltip("Solo FitRoom: si está activo, calcula el orthographicSize para encajar el fondo " +
             "exacto (ignora manualOrthoSize). Desactívalo para respetar el tamaño manual.")]
    [SerializeField] private bool autoFitOrthoSize = true;

    [Header("Suavizado (solo modo Follow)")]
    [SerializeField] private float smoothTime = 0.18f;

    private Camera _cam;
    private Vector3 _velocity;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake() => _cam = GetComponent<Camera>();

    void Start()
    {
        // Auto-asignar target si no está asignado en el inspector
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        // Detectar SteppedCameraBounds primero; si no existe, buscar CameraBoundsZone
        _steppedBounds = FindObjectOfType<SteppedCameraBounds>();
        _boundsZone    = FindObjectOfType<CameraBoundsZone>();

        // Aplicar tamaño manual antes de FitToRoom (FitRoom+autoFit puede sobreescribirlo)
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
                // Cámara estática: no hace nada en LateUpdate
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
    // Calcula el orthographicSize exacto para que el fondo ocupe 100% de pantalla,
    // luego centra la cámara. Se ejecuta una vez en Start.
    private void FitToRoom()
    {
        if (roomBackground == null)
        {
            Debug.LogWarning("[CameraFollow] roomBackground no asignado — cámara sin cambios.");
            return;
        }

        var bounds = roomBackground.bounds;

        if (autoFitOrthoSize)
        {
            // Ajustar altura: orthoSize = mitad del alto del sprite
            _cam.orthographicSize = bounds.size.y / 2f;

            // Ajustar si el aspect ratio deja barras negras laterales
            float neededOrthoForWidth = (bounds.size.x / 2f) / _cam.aspect;
            if (neededOrthoForWidth > _cam.orthographicSize)
                _cam.orthographicSize = neededOrthoForWidth;
        }

        // Centrar en el sprite
        Vector3 center = bounds.center;
        center.z = transform.position.z;
        transform.position = center;

    }

    // ── FollowClamped ─────────────────────────────────────────────────────────
    // Sigue al jugador pero impide que la cámara muestre fuera del fondo.
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
    // Sigue al jugador pero clampa a los límites de la sala.
    // Prioridad: SteppedCameraBounds > CameraBoundsZone > boundsMin/Max manual.
    private void FollowBounded()
    {
        if (target == null) return;

        float hv = _cam.orthographicSize;
        float wv = hv * _cam.aspect;

        float xMin, xMax, yMin, yMax;

        if (_steppedBounds != null)
        {
            // Límites de Y dinámicos según la X actual del jugador
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

    // ── API pública (para cambiar modo en runtime, ej: cutscenes) ────────────
    public void SetMode(CameraMode newMode) => mode = newMode;

    public void SnapToTarget()
    {
        if (target == null) return;
        _velocity = Vector3.zero;
        var p = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = p;
    }
}
