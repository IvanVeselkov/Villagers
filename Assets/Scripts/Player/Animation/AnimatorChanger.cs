using UnityEngine;
using Mirror;
using System.Collections;

public class AnimatorChanger : NetworkBehaviour
{
  [SyncVar(hook = nameof(animatorController))]
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

  [Client]
  public void DoAttack()
  {
    StartCoroutine(IAttack());
  }

  IEnumerator IAttack()
  {
    animatorController.SetTrigger("Damage");
    yield return new WaitForSeconds(0.5f);
  }

  [Client]
  private void DoDead()
  {
    animatorController.SetTrigger("Dead");
    isDead = true;
  }

  [Client]
  private void DoMove(float x,float y)
  {
    animatorController.SetFloat("speed", Mathf.Abs(x) + Mathf.Abs(y), 0.1f, Time.deltaTime);
  }
  // Update is called once per frame
  void Update()
  {
    if (isLocalPlayer)
    {
      if (!Dead)
      {
        if (!Attack)
        {
          float x = Input.GetAxis("Horizontal");
          float y = Input.GetAxis("Vertical");

          DoMove(x, y);
        }
      }
      else
      {
        if (!isDead)
        {
          DoDead();
        }
      }
    }
  }
}
