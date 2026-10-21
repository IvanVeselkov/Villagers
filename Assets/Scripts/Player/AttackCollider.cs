using System.Collections.Generic;
using UnityEngine;

public class AttackCollider : MonoBehaviour
{
  public float Damage = 10f;



  private void OnTriggerEnter(Collider other)
  {
    if (other.tag == "Damageable")
    {

      other.GetComponent<HitBox>().TakeDamage(Damage);

    }
  }
}
