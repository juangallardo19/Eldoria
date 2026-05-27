using UnityEngine;

// Pattern: Observer — bridge between BossObsesion and the Animator.
// Centralises state names and clip durations so BossObsesion doesn't depend on loose strings.
// Durations calculated as: frame count / fps (clips are created by SetupBoss1).
[RequireComponent(typeof(Animator))]
public class BossObsesionAnimator : MonoBehaviour
{
    private Animator _anim;

    // ── Clip durations (frame count / fps) ────────────────────────────────────
    public float SleepDuration       =>  0.5f;   // loop, irrelevant
    public float WakeDuration        =>  1.5f;   // 12f / 8fps
    public float IdleDuration        =>  3.0f;   // 24f / 8fps (loop)
    public float MoveDuration        =>  2.0f;   // 24f / 12fps (loop)
    public float TurnDuration        =>  0.75f;  // ~6f / 8fps
    public float MeleeDuration       =>  1.92f;  // 23f / 12fps
    public float RangeDuration       =>  2.1f;   // 21f / 10fps
    public float SpinChargeDuration  =>  1.2f;   // 12f / 10fps
    public float SpinEndDuration     =>  0.375f; // 3f  /  8fps
    public float BoomerangDuration   =>  1.5f;   // ~15f / 10fps
    public float BuffDuration        =>  1.625f; // 13f /  8fps
    public float SuperDuration       =>  1.8f;   // 18f / 10fps
    public float DeathDuration       =>  2.625f; // 21f /  8fps

    void Awake() => _anim = GetComponent<Animator>();

    // ── Play methods ─────────────────────────────────────────────────────────
    public void PlaySleep()       => Play("Sleep");
    public void PlayWake()        => Play("Wake");
    public void PlayIdle()        => Play("Idle");
    public void PlayMove()        => Play("Move");
    public void PlayTurnRight()   => Play("TurnRight");
    public void PlayTurnLeft()    => Play("TurnLeft");
    public void PlayMelee()       => Play("Melee");
    public void PlayRange()       => Play("Range");
    public void PlaySpinCharge()  => Play("SpinCharge");
    public void PlaySpinEnd()     => Play("SpinEnd");
    public void PlayBoomerang()   => Play("Boomerang");
    public void PlayBuff()        => Play("Buff");
    public void PlaySuper()       => Play("Super");
    public void PlayDeath()       => Play("Death");

    private void Play(string stateName)
    {
        if (_anim != null)
            _anim.Play(stateName, 0, 0f);
    }

    // Pause/resume animations (used for buff flash)
    public void Pause()  { if (_anim) _anim.speed = 0f; }
    public void Resume() { if (_anim) _anim.speed = 1f; }
}
