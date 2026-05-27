using System;
using UnityEngine;

// Patrón Command — pared invisible que desaparece al detectar la acción requerida del jugador.
// Retardo de 0.6s antes de activarse para evitar que inputs ya mantenidos disparen el gate.
// La suscripción a eventos se hace DESPUÉS de que expira el timer (no en OnEnable),
// así no hay race condition entre el timer y el evento.
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
        // MoveBoth se detecta por polling: timer largo evita que una dirección ya presionada lo active.
        // El resto son event-based: timer corto solo previene triggers del mismo frame.
        _activationTimer = requiredAction == GateAction.MoveBoth ? 0.6f : 0.15f;
    }

    void Update()
    {
        // Periodo de gracia: decrementar timer, aún no escuchar
        if (_activationTimer > 0f)
        {
            _activationTimer -= Time.deltaTime;
            return;
        }

        // Timer expirado — suscribir eventos una sola vez (para gates event-based)
        if (!_subscribed && requiredAction != GateAction.MoveBoth)
        {
            _subscribed = true;
            Subscribe();
        }

        // Gate de movimiento: detectar input directo con teclas configuradas
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
        // Siempre desuscribir al desactivarse (es seguro aunque no se haya suscrito)
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
