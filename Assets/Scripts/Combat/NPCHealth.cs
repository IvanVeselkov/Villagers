using UnityEngine;
using Mirror;

public class NPCHealth : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float currentHealth = 100f;
    public float maxHealth = 100f;
    
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
    
    public float GetHealth()
    {
        return currentHealth;
    }
    
    [Server]
    public void DealDamage(float damage, GameObject attacker)
    {
        if (!isServer) return;
        if (!IsAlive()) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath(attacker);
        }
    }
    
    [Server]
    void OnDeath(GameObject attacker)
    {
        // Можно добавить логику смерти NPC
        // Пока просто скрываем
        gameObject.SetActive(false);
    }
    
    void OnHealthChanged(float oldValue, float newValue)
    {
        currentHealth = newValue;
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth = maxHealth;
    }
}
