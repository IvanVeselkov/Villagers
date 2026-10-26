using Mirror;
using System.Collections;
using UnityEngine;

public class AttackManager : NetworkBehaviour
{
  public NetPlayerInformation NetPlayerInfo;
  private CombatBase CombatBase;
  private bool isAttacking;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    CombatBase = GetComponent<CombatBase>();
  }

  // Update is called once per frame
  void Update()
  {
    if (!isLocalPlayer) return;
    if (isAttacking) return;
    if (Input.GetMouseButtonDown(0))
    {
      StartCoroutine(AttackRoutine());
    }
  }

  IEnumerator AttackRoutine()
  {
    isAttacking = true;

    // Блокируем движение
    NetPlayerInfo.NetMovement?.SetMoveStop(true);

    // Проигрываем анимацию удара
    NetPlayerInfo.AnimatorChanger?.DoAttack();

    // Отправляем атаку на сервер
    CombatBase.RequestAttack();

    // Ждём окончания анимации удара
    float duration = NetPlayerInfo.AnimatorChanger.AttackDuration;
    if (duration <= 0) duration = 0.5f; // страховка на случай если не заполнил
    yield return new WaitForSeconds(duration);

    // Разблокируем
    NetPlayerInfo.NetMovement?.SetMoveStop(false);
    isAttacking = false;
  }
}

