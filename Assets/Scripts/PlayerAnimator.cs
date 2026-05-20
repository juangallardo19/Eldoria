using UnityEngine;

// Observer — lee el estado del PlayerController cada frame y actualiza los parámetros
// del Animator. Separado del controlador para respetar el principio de responsabilidad única.
//
// Parámetros requeridos en el Animator Controller:
//   Speed       (Float)   — velocidad horizontal absoluta
//   IsGrounded  (Bool)
//   IsRunning   (Bool)    — modo correr activo (Shift toggle) + moviéndose en suelo
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

    // Hashes de parámetros — más eficiente que strings en Update
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

    // Llamar desde sistemas de daño
    public void TriggerHurt() => anim.SetTrigger(PHurt);
    public void TriggerDie()  => anim.SetTrigger(PDie);
}
