using UnityEngine;
using Mirror;
public class HitBox : NetworkBehaviour, IDamageable
{
  public float TakenDamage;

  public void DealDamage(float damage, GameObject attacker)
  {
    if (!isServer) return;
    if (!IsAlive()) return;

    TakeDamage(damage);
  }

  public float GetHealth()
  {
    throw new System.NotImplementedException();
  }

  public bool IsAlive()
  {
    throw new System.NotImplementedException();
  }

  [Client]
  public void TakeDamage(float Damage)
  {
    TakenDamage=Damage;
  }

  
}
