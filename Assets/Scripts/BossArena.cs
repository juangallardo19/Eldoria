using UnityEngine;

// Pattern: Observer — locks the player inside the arena when the boss wakes.
// Subscribes to BossObsesion.OnPhaseChanged: on Phase1, activates the physical walls
// and deactivates SceneBoundary so the player cannot escape.
// On boss death (OnBossDead), reverts everything.
// References are assigned in Inspector or searched by name in Start() if empty.
public class BossArena : MonoBehaviour
{
    [Header("Arena physics walls")]
    [SerializeField] private GameObject wallLeft;
    [SerializeField] private GameObject wallRight;

    [Header("Scene boundaries")]
    [SerializeField] private GameObject boundaryLeft;
    [SerializeField] private GameObject boundaryRight;

    void Start()
    {
        if (wallLeft      == null) wallLeft      = GameObject.Find("ArenaWall_Left");
        if (wallRight     == null) wallRight     = GameObject.Find("ArenaWall_Right");
        if (boundaryLeft  == null) boundaryLeft  = GameObject.Find("SceneBoundary_Left");
        if (boundaryRight == null) boundaryRight = GameObject.Find("SceneBoundary_Right");

        // Initial state: walls off, boundaries active
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
