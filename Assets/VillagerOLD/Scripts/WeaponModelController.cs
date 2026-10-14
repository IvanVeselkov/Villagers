using System.Collections.Generic;
using UnityEngine;

public class WeaponModelController : MonoBehaviour
{
  [Header("Топоры.")]
  public List<GameObject> Axes = new List<GameObject>();
  public int LevelAxe { get; set; }

  [Header("Кирки.")]
  public List<GameObject> PixAxes = new List<GameObject>();
  public int LevelPixAxes { get; set; }

  [Header("Оружие.")]
  public List<GameObject> Weapons = new List<GameObject>();
  public int LevelWeapons { get; set; }

  public void SetAxeActiveStatus(bool b)
  {
    Axes[LevelAxe].SetActive(b);
  }
  public void SetWeaponActiveStatus(bool b)
  {
    Weapons[LevelWeapons].SetActive(b);
  }
  public void SetPixAxeActiveStatus(bool b)
  {
    PixAxes[LevelPixAxes].SetActive(b);
  }

  public void DisableAll()
  {
    SetPixAxeActiveStatus(false);
    SetWeaponActiveStatus(false);
    SetAxeActiveStatus(false);
  }

  public void ActivateWeapon(WeaponType type)
  {
    DisableAll();
    switch (type)
    {
      case WeaponType.Axe:
        SetAxeActiveStatus(true);
        break;
      case WeaponType.PixAxe:
        SetPixAxeActiveStatus(true);
        break;
      case WeaponType.Sword:
        SetWeaponActiveStatus(true);
        break;
    }

  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    LevelAxe = 0;
    LevelPixAxes = 0;
    LevelWeapons = 0;
  }

}
