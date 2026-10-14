using UnityEngine;
using UnityEngine.UI;

public class UIWeaponVisualizator : MonoBehaviour
{
  [Header("Основное оружие")]
  public Weapon MainWeapon;

  [Header("Список")]
  public Texture[] MainWeaponTextures = new Texture[0];

  [Header("Слот главного оружия")]
  public RawImage UIMainWeapon;

  private void UIVisualizatorUpdate()
  {
    switch (MainWeapon.WeaponType)
    {
      case WeaponType.None:
        UIMainWeapon.texture = null; break;
      case WeaponType.Sword:
        UIMainWeapon.texture = MainWeaponTextures[0];
        break;
      case WeaponType.PixAxe:
        UIMainWeapon.texture = MainWeaponTextures[1];
        break;
      case WeaponType.Axe:
        UIMainWeapon.texture = MainWeaponTextures[2];
        break;
    }
  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {

  }

  // Update is called once per frame
  void Update()
  {
    UIVisualizatorUpdate();
  }
}
