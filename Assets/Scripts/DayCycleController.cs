using UnityEngine;

// Ciclo día/noche basado en tiempo total de partida (persistente entre sesiones).
// Patrón: State Machine — 4 fases (Night→Dawn→Day→Dusk→Night) en bucle.
//         Crossfade de alpha entre fases. El estado al cargar refleja el tiempo acumulado.
// Dependencia: GameSaveController.TotalPlayTime (savedPlayTime + sesión actual).
public class DayCycleController : MonoBehaviour
{
    public enum TimeOfDay { Night, Dawn, Day, Dusk }

    [Header("Fondos (asignar en inspector o se buscan por nombre)")]
    public SpriteRenderer bgNight;
    public SpriteRenderer bgDawn;
    public SpriteRenderer bgDay;
    public SpriteRenderer bgDusk;

    [Header("Duración de cada fase (segundos)")]
    public float phaseDuration = 300f; // 5 minutos por fase, 20 min ciclo completo

    [Header("Duración crossfade (segundos)")]
    public float fadeDuration = 4f;

    private TimeOfDay _shown  = TimeOfDay.Night;
    private TimeOfDay _target = TimeOfDay.Night;
    private float     _fadeT;
    private bool      _fading;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        AutoFindChildren();
        // Snap inmediato al estado correcto según el tiempo de partida guardado.
        _shown = StateFromTime(GetTotalPlayTime());
        InitAlphas();
    }

    void Update()
    {
        TimeOfDay desired = StateFromTime(GetTotalPlayTime());
        if (desired != _shown && !_fading)
            BeginFade(desired);

        if (_fading)
            TickFade();
    }

    // ── Tiempo ───────────────────────────────────────────────────────────────
    private float GetTotalPlayTime()
    {
        // En partida real usa el tiempo guardado + sesión actual.
        if (GameSaveController.Instance != null)
            return GameSaveController.Instance.TotalPlayTime;
        // Fallback en editor o sin slot activo: tiempo de sesión Unity.
        return Time.time;
    }

    private TimeOfDay StateFromTime(float totalSeconds)
    {
        int phase = Mathf.FloorToInt(totalSeconds / Mathf.Max(phaseDuration, 1f)) % 4;
        return (TimeOfDay)phase;
    }

    // ── Init ──────────────────────────────────────────────────────────────────
    private void AutoFindChildren()
    {
        if (bgNight == null) { var t = transform.Find("BG_Noche");     if (t) bgNight = t.GetComponent<SpriteRenderer>(); }
        if (bgDawn  == null) { var t = transform.Find("BG_Amanecer");  if (t) bgDawn  = t.GetComponent<SpriteRenderer>(); }
        if (bgDay   == null) { var t = transform.Find("BG_Dia");       if (t) bgDay   = t.GetComponent<SpriteRenderer>(); }
        if (bgDusk  == null) { var t = transform.Find("BG_Anochecer"); if (t) bgDusk  = t.GetComponent<SpriteRenderer>(); }
    }

    private void InitAlphas()
    {
        SetAlpha(bgNight, _shown == TimeOfDay.Night ? 1f : 0f);
        SetAlpha(bgDawn,  _shown == TimeOfDay.Dawn  ? 1f : 0f);
        SetAlpha(bgDay,   _shown == TimeOfDay.Day   ? 1f : 0f);
        SetAlpha(bgDusk,  _shown == TimeOfDay.Dusk  ? 1f : 0f);
    }

    // ── State Machine ─────────────────────────────────────────────────────────
    private void BeginFade(TimeOfDay to)
    {
        _target = to;
        _fadeT  = 0f;
        _fading = true;
    }

    private void TickFade()
    {
        _fadeT = Mathf.MoveTowards(_fadeT, 1f, Time.deltaTime / Mathf.Max(fadeDuration, 0.01f));

        SetAlpha(SR(_shown),  1f - _fadeT);
        SetAlpha(SR(_target), _fadeT);

        foreach (TimeOfDay tod in System.Enum.GetValues(typeof(TimeOfDay)))
            if (tod != _shown && tod != _target)
                SetAlpha(SR(tod), 0f);

        if (_fadeT >= 1f)
        {
            _shown  = _target;
            _fading = false;
        }
    }

    // ── API pública ───────────────────────────────────────────────────────────
    // Avanza al siguiente estado (Night→Dawn→Day→Dusk→Night).
    // Llamar desde Santuario de Ara al descansar — efecto solo visual esta sesión.
    public void AdvanceToNextState()
    {
        TimeOfDay next = (TimeOfDay)(((int)_shown + 1) % 4);
        if (!_fading) BeginFade(next);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private SpriteRenderer SR(TimeOfDay tod)
    {
        return tod switch
        {
            TimeOfDay.Dawn => bgDawn,
            TimeOfDay.Day  => bgDay,
            TimeOfDay.Dusk => bgDusk,
            _              => bgNight,
        };
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a      = a;
        sr.color = c;
    }
}
