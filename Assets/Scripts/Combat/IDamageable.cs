public interface IDamageable
{
    void DealDamage(float damage, UnityEngine.GameObject attacker);
    float GetHealth();
    bool IsAlive();
}
