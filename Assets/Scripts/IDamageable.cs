// Interfaz que deben implementar todos los objetos que reciben daño (enemigos, destructibles).
// Patrón: Strategy — desacopla al atacante del tipo concreto de objetivo.
public interface IDamageable
{
    void TakeDamage(int damage);
}
