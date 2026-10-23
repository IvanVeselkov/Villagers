using UnityEngine;
using Mirror;
using System.Collections;

public class AttackManager : NetworkBehaviour
{
  public NetPlayerInformation NetPlayerInfo;
  public GameObject AttackCollider;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    AttackCollider.SetActive(false);

  }

  IEnumerator Attack()
  {
    AttackCollider.SetActive(true);
    NetPlayerInfo.AnimatorChanger.DoAttack();
    yield return new WaitForSeconds(NetPlayerInfo.AnimatorChanger.AttackDuration);
    AttackCollider.SetActive(false);
  }
  // Update is called once per frame
  void Update()
  {
    if (Input.GetMouseButtonDown(0)&&AttackCollider.activeSelf==false)
    {
      StartCoroutine(Attack());
    }
  }
}

// Замечание: для работы с новым CombatBase лучше использовать CombatBase.RequestAttack()
// и не включать AttackCollider напрямую со всех клиентов
