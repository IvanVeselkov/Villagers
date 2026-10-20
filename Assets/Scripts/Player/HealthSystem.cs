using Mirror;
using TMPro;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
  public NetPlayerInformation NetPlayerInfo;
  public float Health = 100f;
  public TMP_Text healthText;

  [SyncVar(hook = nameof(SyncHealth))] //задаем метод, который будет выполняться при синхронизации переменной
  float _SyncHealth;

  //метод не выполнится, если старое значение равно новому
  void SyncHealth(float oldValue, float newValue) //обязательно делаем два значения - старое и новое. 
  {
    Health = newValue;
  }

  [Server] //обозначаем, что этот метод будет вызываться и выполняться только на сервере
  public void ChangeHealthValue(float newValue)
  {
    _SyncHealth = newValue;
  }

  [Command] //обозначаем, что этот метод должен будет выполняться на сервере по запросу клиента
  public void CmdChangeHealth(float newValue) //обязательно ставим Cmd в начале названия метода
  {
    ChangeHealthValue(newValue); //переходим к непосредственному изменению переменной
  }

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }
  public void TakeDamage(float Damage)
  {
    if (isServer) //если мы являемся сервером, то переходим к непосредственному изменению переменной
      ChangeHealthValue(Health - Damage);
    else
      CmdChangeHealth(Health - Damage); //в противном случае делаем на сервер запрос об изменении переменной
  }
  // Update is called once per frame
  void Update()
  {
    if (isOwned) //проверяем, есть ли у нас права изменять этот объект
    {
      healthText.text = Health.ToString();
    }
    
  }
}
