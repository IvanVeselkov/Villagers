using UnityEngine;
using Mirror;
using System.Collections;

public class AnimatorChanger : NetworkBehaviour
{
  private Animator animatorController;
  public NetPlayerInformation NetPlayerInfo;

  public bool Attack;
  public float AttackDuration;
  public bool Dead;
  private bool isDead;
  // Start is called before the first frame update
  void Start()
  {
    animatorController = GetComponent<Animator>();
  }

  public void DoAttack()
  {
    StartCoroutine(IAttack());
  }

  IEnumerator IAttack()
  {
    animatorController.SetTrigger("Damage");
    yield return new WaitForSeconds(0.5f);
  }
  // Update is called once per frame
  void Update()
  {
    if (isOwned)
    {
      if (!Dead)
      {
        if (!Attack)
        {
          float x = Input.GetAxis("Horizontal");
          float y = Input.GetAxis("Vertical");

          animatorController.SetFloat("speed", Mathf.Abs(x) + Mathf.Abs(y), 0.1f, Time.deltaTime);
        }
        else
        {
          animatorController.SetTrigger("Damage");
        }
      }
      else
      {
        if (!isDead)
        {
          animatorController.SetTrigger("Dead");
          isDead = true;
        }
      }
    }
  }
}
