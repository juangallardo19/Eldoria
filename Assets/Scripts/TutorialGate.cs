using System;
using UnityEngine;

// Pattern: Command — invisible wall that disappears when the required player action is detected.
// 0.6s delay before activating to prevent already-held inputs from firing the gate.
// Event subscription happens AFTER the timer expires (not in OnEnable),
// so there is no race condition between the timer and the event.
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialGate : MonoBehaviour
{
    public enum GateAction { MoveBoth, Jump, HoldJump, Attack, DropThrough, Run, Combo3 }

    public GateAction requiredAction;
    public Action     onCleared;
    public bool       locked = false;

    bool  _cleared;
    bool  _subscribed;
    float _activationTimer;

    // Para MoveBoth: rastrear cada dirección por separado
    bool _movedLeft;
    bool _movedRight;

    void Start()
    {
        // MoveBoth is detected by polling: long timer prevents an already-held direction from activating it.
        // Others are event-based: short timer only prevents same-frame triggers.
        _activationTimer = requiredAction == GateAction.MoveBoth ? 0.6f : 0.15f;
    }

    void Update()
    {
        // Grace period: decrement timer, not yet listening
        if (_activationTimer > 0f)
        {
            _activationTimer -= Time.deltaTime;
            return;
        }

        // Timer expired — subscribe events once (for event-based gates)
        if (!_subscribed && requiredAction != GateAction.MoveBoth)
        {
            _subscribed = true;
            Subscribe();
        }

        // Movement gate: detect direct input with configured keys
        if (requiredAction == GateAction.MoveBoth)
        {
            var leftKey  = KeyRebindUI.GetKey("MoveLeft",  KeyCode.A);
            var rightKey = KeyRebindUI.GetKey("MoveRight", KeyCode.D);

            if (Input.GetKey(leftKey)  || Input.GetKey(KeyCode.LeftArrow))  _movedLeft  = true;
            if (Input.GetKey(rightKey) || Input.GetKey(KeyCode.RightArrow)) _movedRight = true;

            if (_movedLeft && _movedRight) Clear();
        }
    }

    void Subscribe()
    {
        switch (requiredAction)
        {
            case GateAction.Jump:        PlayerController.OnPlayerJumped   += Clear; break;
            case GateAction.HoldJump:    PlayerController.OnPlayerHeldJump += Clear; break;
            case GateAction.Attack:      PlayerController.OnPlayerAttacked += Clear; break;
            case GateAction.DropThrough: PlayerController.OnPlayerDropped  += Clear; break;
            case GateAction.Run:         PlayerController.OnPlayerRan      += Clear; break;
            case GateAction.Combo3:      PlayerCombat.OnCombo3Started      += Clear; break;
        }
    }

    void OnDisable()
    {
        // Always unsubscribe on disable (safe even if never subscribed)
        switch (requiredAction)
        {
            case GateAction.Jump:        PlayerController.OnPlayerJumped   -= Clear; break;
            case GateAction.HoldJump:    PlayerController.OnPlayerHeldJump -= Clear; break;
            case GateAction.Attack:      PlayerController.OnPlayerAttacked -= Clear; break;
            case GateAction.DropThrough: PlayerController.OnPlayerDropped  -= Clear; break;
            case GateAction.Run:         PlayerController.OnPlayerRan      -= Clear; break;
            case GateAction.Combo3:      PlayerCombat.OnCombo3Started      -= Clear; break;
        }
    }

    void Clear()
    {
        if (locked || _cleared) return;
        _cleared = true;
        onCleared?.Invoke();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Vector3 c = transform.TransformPoint(col.offset);
        Gizmos.DrawCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        Gizmos.DrawWireCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
    }
#endif
}
