using UnityEngine;

// Observer — reads PlayerController state each frame and updates the Animator.
// Separated from the controller to respect the single-responsibility principle.
//
// Animator parameters:
//   Speed       (Float)
//   IsGrounded  (Bool)
//   IsRunning   (Bool)
//   IsJumping   (Bool)
//   IsFalling   (Bool)
//   IsDashing   (Bool)
//   IsWallSlide (Bool)
//   Hurt        (Trigger)
//   Die         (Trigger)
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator         anim;
    private PlayerController ctrl;

    private static readonly int PSpeed       = Animator.StringToHash("Speed");
    private static readonly int PIsGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int PIsRunning   = Animator.StringToHash("IsRunning");
    private static readonly int PIsJumping   = Animator.StringToHash("IsJumping");
    private static readonly int PIsFalling   = Animator.StringToHash("IsFalling");
    private static readonly int PIsDashing   = Animator.StringToHash("IsDashing");
    private static readonly int PIsWallSlide = Animator.StringToHash("IsWallSlide");
    private static readonly int PHurt        = Animator.StringToHash("Hurt");
    private static readonly int PDie         = Animator.StringToHash("Die");

    void Awake()
    {
        anim = GetComponent<Animator>();
        ctrl = GetComponent<PlayerController>();
    }

    void Update()
    {
        anim.SetFloat(PSpeed,       ctrl.SpeedX);
        anim.SetBool (PIsGrounded,  ctrl.IsGrounded);
        anim.SetBool (PIsRunning,   ctrl.IsRunning);
        anim.SetBool (PIsJumping,   ctrl.IsJumping);
        anim.SetBool (PIsFalling,   ctrl.IsFalling);
        anim.SetBool (PIsDashing,   ctrl.IsDashing);
        anim.SetBool (PIsWallSlide, ctrl.IsWallSliding);
    }

    public void TriggerHurt() => anim.SetTrigger(PHurt);
    public void TriggerDie()  => anim.SetTrigger(PDie);

    // Forces return to Idle state — call after death/absorption sequences
    // to prevent Kael getting stuck on the last frame of Die.
    public void ResetToIdle()
    {
        anim.ResetTrigger(PDie);
        anim.ResetTrigger(PHurt);
        anim.Play("Idle", 0, 0f);
    }
}
