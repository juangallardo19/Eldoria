using UnityEngine;

// Ciclo día/noche basado en distancia total recorrida por el jugador.
// Patrón: State Machine — 4 estados (Night→Dawn→Day→Dusk→Night) en bucle.
//         Crossfade de alpha cuando el estado cambia.
public class DayCycleController : MonoBehaviour
{
    public enum TimeOfDay { Night, Dawn, Day, Dusk }

    [Header("Fondos (asignar en inspector)")]
    public SpriteRenderer bgNight;
    public SpriteRenderer bgDawn;
    public SpriteRenderer bgDay;
    public SpriteRenderer bgDusk;

    [Header("Distancia para cada cambio (unidades)")]
    public float dawnAt   =  3000f;
    public float dayAt    =  7500f;
    public float duskAt   = 14000f;
    public float cycleEnd = 20000f;  // vuelta a noche (~41 min a walkSpeed=8)

    [Header("Duración crossfade (segundos)")]
    public float fadeDuration = 4f;

    private TimeOfDay _shown  = TimeOfDay.Night;
    private TimeOfDay _target = TimeOfDay.Night;
    private float     _fadeT;
    private bool      _fading;

    private float   _distTotal;
    private Vector2 _prevPos;
    private bool    _playerReady;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        AutoFindChildren();
        TryFindPlayer();
        InitAlphas();
    }

    void Update()
    {
        AccumulateDistance();

        TimeOfDay desired = StateFromDist(_distTotal);
        if (desired != _shown && !_fading)
            BeginFade(desired);

        if (_fading)
            TickFade();
    }

    // ── Init ──────────────────────────────────────────────────────────────────
    // Si los campos no están asignados en el Inspector, busca hijos por nombre convencional.
    private void AutoFindChildren()
    {
        if (bgNight == null) { var t = transform.Find("BG_Noche");    if (t) bgNight = t.GetComponent<SpriteRenderer>(); }
        if (bgDawn  == null) { var t = transform.Find("BG_Amanecer"); if (t) bgDawn  = t.GetComponent<SpriteRenderer>(); }
        if (bgDay   == null) { var t = transform.Find("BG_Dia");      if (t) bgDay   = t.GetComponent<SpriteRenderer>(); }
        if (bgDusk  == null) { var t = transform.Find("BG_Anochecer");if (t) bgDusk  = t.GetComponent<SpriteRenderer>(); }
    }

    private void InitAlphas()
    {
        SetAlpha(bgNight,  1f);
        SetAlpha(bgDawn,   0f);
        SetAlpha(bgDay,    0f);
        SetAlpha(bgDusk,   0f);
    }

    // ── Distance tracking ────────────────────────────────────────────────────
    private void TryFindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        _prevPos     = p.transform.position;
        _playerReady = true;
    }

    private void AccumulateDistance()
    {
        if (!_playerReady) { TryFindPlayer(); return; }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        Vector2 cur   = p.transform.position;
        _distTotal   += Vector2.Distance(cur, _prevPos);
        _prevPos      = cur;
    }

    // ── State Machine ─────────────────────────────────────────────────────────
    private TimeOfDay StateFromDist(float dist)
    {
        float t = dist % cycleEnd;
        if (t < dawnAt) return TimeOfDay.Night;
        if (t < dayAt)  return TimeOfDay.Dawn;
        if (t < duskAt) return TimeOfDay.Day;
        return TimeOfDay.Dusk;
    }

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

        // Ocultar cualquier estado que no participe en la transición
        foreach (TimeOfDay tod in System.Enum.GetValues(typeof(TimeOfDay)))
            if (tod != _shown && tod != _target)
                SetAlpha(SR(tod), 0f);

        if (_fadeT >= 1f)
        {
            _shown  = _target;
            _fading = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private SpriteRenderer SR(TimeOfDay tod)
    {
        switch (tod)
        {
            case TimeOfDay.Night: return bgNight;
            case TimeOfDay.Dawn:  return bgDawn;
            case TimeOfDay.Day:   return bgDay;
            case TimeOfDay.Dusk:  return bgDusk;
            default:              return bgNight;
        }
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a      = a;
        sr.color = c;
    }
}
