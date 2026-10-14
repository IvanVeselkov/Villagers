using System;
using System.Collections.Generic;
using UnityEngine;

public class SecondWeaponModelController : MonoBehaviour
{
  [Header("Луки.")]
  public List<GameObject> Bows = new List<GameObject>();
  public int LevelBows { get; set; }
  [Header("Посохи.")]
  public List<GameObject> Staffs = new List<GameObject>();
  public int LevelStaffs { get; set; }
  [Header("Щиты.")]
  public List<GameObject> Shields = new List<GameObject>();
  public int LevelShields { get; set; }
  public void SetBowActiveStatus(bool b)
  {
    Bows[LevelBows].SetActive(b);
  }
  public void SetStaffActiveStatus(bool b)
  {
    Staffs[LevelStaffs].SetActive(b);
  }
  public void SetShieldActiveStatus(bool b)
  {
    Shields[LevelShields].SetActive(b);
  }

  public void DisableAll()
  {
    SetBowActiveStatus(false);
    SetStaffActiveStatus(false);
    SetShieldActiveStatus(false);
  }

  public void ActivateSecondWeapon(SecondWeaponType type)
  {
    DisableAll();
    switch (type)
    {
      case SecondWeaponType.Bow:
        SetBowActiveStatus(true);
        break;
      case SecondWeaponType.Shield:
        SetShieldActiveStatus(true);
        break;
      case SecondWeaponType.Staff:
        SetStaffActiveStatus(true);
        break;
    }

  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    LevelBows = 0;
    LevelStaffs = 0;
    LevelShields = 0;
  }

}
