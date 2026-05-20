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

    [Header("Límites manuales (solo FollowBounded)")]
    public Vector2 boundsMin = new Vector2(-30f, -12f);
    public Vector2 boundsMax = new Vector2( 30f,   8f);

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

        // Ajustar altura: orthoSize = mitad del alto del sprite
        _cam.orthographicSize = bounds.size.y / 2f;

        // Ajustar si el aspect ratio deja barras negras laterales
        // (el sprite es más ancho de lo que cabe a este orthoSize)
        float neededOrthoForWidth = (bounds.size.x / 2f) / _cam.aspect;
        if (neededOrthoForWidth > _cam.orthographicSize)
            _cam.orthographicSize = neededOrthoForWidth;

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

            float tx = Mathf.Clamp(target.position.x, minX, maxX);
            float ty = Mathf.Clamp(target.position.y, minY, maxY);
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
    // Sigue al jugador pero clampa con límites manuales (boundsMin/boundsMax).
    // Ideal para salas anchas con parallax donde el roomBackground no es confiable.
    private void FollowBounded()
    {
        if (target == null) return;

        float hv = _cam.orthographicSize;
        float wv = hv * _cam.aspect;

        float tx = Mathf.Clamp(target.position.x, boundsMin.x + wv, boundsMax.x - wv);
        float ty = Mathf.Clamp(target.position.y, boundsMin.y + hv, boundsMax.y - hv);
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
