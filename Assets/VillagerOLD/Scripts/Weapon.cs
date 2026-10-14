using System;
using UnityEngine;

[System.Serializable]
public class Weapon : MonoBehaviour
{
  [Header("Урон.")]
  public float Damage;
  [Header("Перезарядка.")]
  public float CoolDownAttack;
  [Header("Тип оружия.")]
  public WeaponType WeaponType;
  [Header("Уровень оружия.")]
  public int WeaponLevel;

  public Weapon()
  {
    Damage = 0;
    CoolDownAttack = 0;
    WeaponType=WeaponType.None;
    WeaponLevel = 0;
  }
  public Weapon(WeaponType WeaponType)
  {
    this.WeaponType = WeaponType;
    Damage = 0;
    CoolDownAttack = 0;
    WeaponLevel = 0;
  }

  public Weapon(Weapon w)
  {
    WeaponType = w.WeaponType;
    WeaponLevel = w.WeaponLevel;
    Damage = w.Damage;
    CoolDownAttack = w.CoolDownAttack;
  }
  public void SetWeaponValue(Weapon w)
  {
    WeaponType = w.WeaponType;
    WeaponLevel = w.WeaponLevel;
    Damage = w.Damage;
    CoolDownAttack = w.CoolDownAttack;
  }



}
