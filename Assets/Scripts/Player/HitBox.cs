using UnityEngine;

public class HitBox : MonoBehaviour
{
  public float TakenDamege;

  public void TakeDamage(float Damage)
  {
    TakenDamege=Damage;
  }
}
