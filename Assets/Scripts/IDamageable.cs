// Interface all damageable objects must implement (enemies, destructibles).
// Pattern: Strategy — decouples the attacker from the concrete target type.
public interface IDamageable
{
    void TakeDamage(int damage);
}
