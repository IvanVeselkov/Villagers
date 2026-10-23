using Mirror;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class HealthSystem : NetworkBehaviour, IDamageable
{
  public NetPlayerInformation NetPlayerInfo;
  public float Health = 100f;
  public TMP_Text healthText;

  public List<HitBox> HitBoxs = new List<HitBox>();

  [SyncVar(hook = nameof(SyncHealth))] //Р·Р°РґР°РµРј РјРµС‚РѕРґ, РєРѕС‚РѕСЂС‹Р№ Р±СѓРґРµС‚ РІС‹РїРѕР»РЅСЏС‚СЊСЃСЏ РїСЂРё СЃРёРЅС…СЂРѕРЅРёР·Р°С†РёРё РїРµСЂРµРјРµРЅРЅРѕР№
  float _SyncHealth;

  //РјРµС‚РѕРґ РЅРµ РІС‹РїРѕР»РЅРёС‚СЃСЏ, РµСЃР»Рё СЃС‚Р°СЂРѕРµ Р·РЅР°С‡РµРЅРёРµ СЂР°РІРЅРѕ РЅРѕРІРѕРјСѓ
  void SyncHealth(float oldValue, float newValue) //РѕР±СЏР·Р°С‚РµР»СЊРЅРѕ РґРµР»Р°РµРј РґРІР° Р·РЅР°С‡РµРЅРёСЏ - СЃС‚Р°СЂРѕРµ Рё РЅРѕРІРѕРµ. 
  {
    Health = newValue;
  }

  [Server] //РѕР±РѕР·РЅР°С‡Р°РµРј, С‡С‚Рѕ СЌС‚РѕС‚ РјРµС‚РѕРґ Р±СѓРґРµС‚ РІС‹Р·С‹РІР°С‚СЊСЃСЏ Рё РІС‹РїРѕР»РЅСЏС‚СЊСЃСЏ С‚РѕР»СЊРєРѕ РЅР° СЃРµСЂРІРµСЂРµ
  public void ChangeHealthValue(float newValue)
  {
    _SyncHealth = newValue;
  }

  [Command] //РѕР±РѕР·РЅР°С‡Р°РµРј, С‡С‚Рѕ СЌС‚РѕС‚ РјРµС‚РѕРґ РґРѕР»Р¶РµРЅ Р±СѓРґРµС‚ РІС‹РїРѕР»РЅСЏС‚СЊСЃСЏ РЅР° СЃРµСЂРІРµСЂРµ РїРѕ Р·Р°РїСЂРѕСЃСѓ РєР»РёРµРЅС‚Р°
  public void CmdChangeHealth(float newValue) //РѕР±СЏР·Р°С‚РµР»СЊРЅРѕ СЃС‚Р°РІРёРј Cmd РІ РЅР°С‡Р°Р»Рµ РЅР°Р·РІР°РЅРёСЏ РјРµС‚РѕРґР°
  {
    ChangeHealthValue(newValue); //РїРµСЂРµС…РѕРґРёРј Рє РЅРµРїРѕСЃСЂРµРґСЃС‚РІРµРЅРЅРѕРјСѓ РёР·РјРµРЅРµРЅРёСЋ РїРµСЂРµРјРµРЅРЅРѕР№
  }

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }
  public void TakeDamage(float Damage)
  {
    if (isServer) //РµСЃР»Рё РјС‹ СЏРІР»СЏРµРјСЃСЏ СЃРµСЂРІРµСЂРѕРј, С‚Рѕ РїРµСЂРµС…РѕРґРёРј Рє РЅРµРїРѕСЃСЂРµРґСЃС‚РІРµРЅРЅРѕРјСѓ РёР·РјРµРЅРµРЅРёСЋ РїРµСЂРµРјРµРЅРЅРѕР№
      ChangeHealthValue(Health - Damage);
    else
      CmdChangeHealth(Health - Damage); //РІ РїСЂРѕС‚РёРІРЅРѕРј СЃР»СѓС‡Р°Рµ РґРµР»Р°РµРј РЅР° СЃРµСЂРІРµСЂ Р·Р°РїСЂРѕСЃ РѕР± РёР·РјРµРЅРµРЅРёРё РїРµСЂРµРјРµРЅРЅРѕР№
  }
  // Update is called once per frame
  void Update()
  {
    if (isOwned) //РїСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё Сѓ РЅР°СЃ РїСЂР°РІР° РёР·РјРµРЅСЏС‚СЊ СЌС‚РѕС‚ РѕР±СЉРµРєС‚
    {
      healthText.text = Health.ToString();
    }


  }

  private void FixedUpdate()
  {
    if (isOwned)
    {
      for (int i = 0; i < HitBoxs.Count; i++)
      {
        if (HitBoxs[i].TakenDamege != 0)
        {
          TakeDamage(HitBoxs[i].TakenDamege);
          HitBoxs[i].TakenDamege = 0;
        }
      }
    }
  }



  public bool IsAlive()
  {
    return Health > 0f;
  }

  public float GetHealth()
  {
    return Health;
  }

  [Server]
  public void DealDamage(float damage, GameObject attacker)
  {
    if (!isServer) return;
    if (!IsAlive()) return;
    ChangeHealthValue(Health - damage);
  }
}