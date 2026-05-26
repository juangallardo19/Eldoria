using UnityEngine;

// Patrón Observer — bloquea al jugador dentro del arena cuando el boss despierta.
// Se suscribe a BossObsesion.OnPhaseChanged: al llegar a Phase1 activa los muros
// físicos y desactiva las SceneBoundary para que el jugador no pueda escapar.
// Al morir el boss (OnBossDead) revierte todo.
// Las referencias se asignan en Inspector o, si están vacías, se buscan por nombre en Start().
public class BossArena : MonoBehaviour
{
    [Header("Muros físicos del arena")]
    [SerializeField] private GameObject wallLeft;
    [SerializeField] private GameObject wallRight;

    [Header("Fronteras de escena")]
    [SerializeField] private GameObject boundaryLeft;
    [SerializeField] private GameObject boundaryRight;

    void Start()
    {
        if (wallLeft      == null) wallLeft      = GameObject.Find("ArenaWall_Left");
        if (wallRight     == null) wallRight     = GameObject.Find("ArenaWall_Right");
        if (boundaryLeft  == null) boundaryLeft  = GameObject.Find("SceneBoundary_Left");
        if (boundaryRight == null) boundaryRight = GameObject.Find("SceneBoundary_Right");

        // Estado inicial: muros apagados, fronteras activas
        SetLocked(false);
    }

    void OnEnable()
    {
        BossObsesion.OnPhaseChanged += HandlePhaseChanged;
        BossObsesion.OnBossDead    += HandleBossDead;
    }

    void OnDisable()
    {
        BossObsesion.OnPhaseChanged -= HandlePhaseChanged;
        BossObsesion.OnBossDead    -= HandleBossDead;
    }

    private void HandlePhaseChanged(BossObsesion.BossPhase phase)
    {
        if (phase == BossObsesion.BossPhase.Phase1)
            SetLocked(true);
    }

    private void HandleBossDead() => SetLocked(false);

    private void SetLocked(bool locked)
    {
        if (wallLeft      != null) wallLeft.SetActive(locked);
        if (wallRight     != null) wallRight.SetActive(locked);
        if (boundaryLeft  != null) boundaryLeft.SetActive(!locked);
        if (boundaryRight != null) boundaryRight.SetActive(!locked);
    }
}
