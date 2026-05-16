using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 6f;
    [SerializeField] private float jumpForce    = 14f;
    [SerializeField] private float dashForce    = 22f;
    [SerializeField] private float dashDuration = 0.14f;
    [SerializeField] private float floatGravity = 0.3f;
    [SerializeField] private float floatMaxTime = 3f;

    [Header("Abilities (unlocked por biomas)")]
    public bool hasDoubleJump   = false;  // Bioma 2 - Montañas
    public bool hasWallClimb    = false;  // Bioma 3 - Bastión
    public bool hasFloat        = false;  // Bioma 4 - Lago
    public bool hasTeleport     = false;  // Bioma 5 - Islas

    [Header("Ground / Wall Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float     groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool  isGrounded;
    private bool  usedDoubleJump;

    // Dash
    private bool  isDashing;
    private float dashTimer;

    // Float
    private bool  isFloating;
    private float floatTimer;

    // Wall climb
    private bool  isOnWall;

    private float facingDir = 1f;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            usedDoubleJump = false;
            isFloating     = false;
        }

        HandleMovement();
        HandleJump();
        HandleDash();
        HandleFloat();
    }

    private void HandleMovement()
    {
        KeyCode left  = KeyRebindUI.GetKey("MoveLeft",  KeyCode.A);
        KeyCode right = KeyRebindUI.GetKey("MoveRight", KeyCode.D);

        float h = 0f;
        if (Input.GetKey(left)  || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(right) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

        if (!isFloating)
            rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);

        if (h != 0f)
        {
            facingDir = Mathf.Sign(h);
            transform.localScale = new Vector3(facingDir, 1f, 1f);
        }
    }

    private void HandleJump()
    {
        KeyCode jumpKey = KeyRebindUI.GetKey("Jump", KeyCode.Space);

        if (!Input.GetKeyDown(jumpKey)) return;

        if (isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        else if (hasDoubleJump && !usedDoubleJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            usedDoubleJump = true;
        }
    }

    private void HandleDash()
    {
        KeyCode dashKey = KeyRebindUI.GetKey("Dash", KeyCode.LeftShift);

        if (!Input.GetKeyDown(dashKey)) return;

        isDashing  = true;
        dashTimer  = dashDuration;
        isFloating = false;
        rb.velocity = new Vector2(facingDir * dashForce, 0f);
        rb.gravityScale = 0f;

        // Restaurar gravedad al terminar el dash
        Invoke(nameof(RestoreGravity), dashDuration);
    }

    private void HandleFloat()
    {
        if (!hasFloat) return;

        KeyCode floatKey = KeyRebindUI.GetKey("Float", KeyCode.F);

        if (Input.GetKeyDown(floatKey) && !isGrounded)
        {
            isFloating  = true;
            floatTimer  = floatMaxTime;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.gravityScale = floatGravity;
        }

        if (isFloating)
        {
            floatTimer -= Time.deltaTime;
            if (floatTimer <= 0f || isGrounded || Input.GetKeyDown(floatKey))
            {
                isFloating = false;
                RestoreGravity();
            }
        }
    }

    private void RestoreGravity()
    {
        if (!isFloating)
            rb.gravityScale = 1f;
    }

    // Llamar desde otros sistemas para el Pulso (ataque)
    public KeyCode GetAttackKey()  => KeyRebindUI.GetKey("Attack",    KeyCode.J);
    public KeyCode GetInteractKey()=> KeyRebindUI.GetKey("Interact",  KeyCode.E);
    public KeyCode GetTeleportKey()=> KeyRebindUI.GetKey("Teleport",  KeyCode.V);
    public KeyCode GetMapKey()     => KeyRebindUI.GetKey("MapOpen",   KeyCode.M);
}
