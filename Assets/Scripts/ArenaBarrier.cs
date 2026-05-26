using UnityEngine;

// Marcador para paredes de arena del boss (pattern: Command).
// BossObsesion.WakeUpSequence habilita todos los ArenaBarrier al iniciar la pelea.
// BossObsesion.DefeatedSequence los deshabilita al terminar.
// El GO debe estar INACTIVO por defecto en escena (el jugador puede pasar libremente antes del boss).
[DisallowMultipleComponent]
public class ArenaBarrier : MonoBehaviour { }
