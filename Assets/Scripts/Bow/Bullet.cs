using UnityEngine;

public class Bullet : MonoBehaviour
{
  public float Damage;
  void Awake()
  {
    Destroy(this.gameObject,5f);
  }
  private void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.tag == "Enemy")
    {
      Damageable d;
      if(d=collision.gameObject.GetComponent<Damageable>())
      {
        d.DealDamage(Damage);
      }
      Destroy(this.gameObject);
    }
  }
}
