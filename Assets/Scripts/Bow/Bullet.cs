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
      //Нанеси урон
    }
  }
}
