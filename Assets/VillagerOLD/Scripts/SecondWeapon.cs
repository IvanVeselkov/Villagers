using Unity.VisualScripting;
using UnityEngine;
public class SecondWeapon : MonoBehaviour
{
  [Header("Урон/Защита.")]
  public float SecondWeaponValue;
  [Header("Дистанция атаки.")]
  public float SecondWeaponDistance;
  public SecondWeaponType SecondWeaponType;
  public int SecondWeaponLevel;
  public SecondWeapon(SecondWeapon secondWeapon)
  {
    SecondWeaponValue = secondWeapon.SecondWeaponValue;
    SecondWeaponType = secondWeapon.SecondWeaponType;
    SecondWeaponDistance = secondWeapon.SecondWeaponDistance;
    SecondWeaponLevel = secondWeapon.SecondWeaponLevel;
  }
  public SecondWeapon()
  {
    SecondWeaponValue = 0;
    SecondWeaponType = SecondWeaponType.None;
    SecondWeaponDistance = 0;
    SecondWeaponLevel = 0;
  }
  public void SetSecondWeaponValue(SecondWeapon secondWeapon)
  {
    SecondWeaponType = secondWeapon.SecondWeaponType;
    SecondWeaponLevel = secondWeapon.SecondWeaponLevel;
  }
}