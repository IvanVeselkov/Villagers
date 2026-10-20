using UnityEngine;
using Mirror;
public class NetPlayerInformation : NetworkBehaviour
{
  public AnimatorChanger AnimatorChanger;
  public NetMovement NetMovement;
  public HealthSystem HealthSystem;
  public AttackManager AttackManager;

  public void Awake()
  {
    AnimatorChanger.NetPlayerInfo = this;
    NetMovement.NetPlayerInfo = this;
    HealthSystem.NetPlayerInfo = this;
    AttackManager.NetPlayerInfo = this;
  }
}
