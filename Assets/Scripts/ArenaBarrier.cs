using UnityEngine;

// Marker for boss arena walls (pattern: Command).
// BossObsesion.WakeUpSequence enables all ArenaBarriers when the fight starts.
// BossObsesion.DefeatedSequence disables them when the fight ends.
// The GO must be INACTIVE by default in the scene (player passes freely before the boss).
[DisallowMultipleComponent]
public class ArenaBarrier : MonoBehaviour { }
