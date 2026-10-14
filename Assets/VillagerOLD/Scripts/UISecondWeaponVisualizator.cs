using UnityEngine;
using UnityEngine.UI;

public class UISecondWeaponVisualizator : MonoBehaviour
{
  [Header("Основное оружие")]
  public SecondWeapon SecondWeapon;

  [Header("Список")]
  public Texture[] SecondWeaponTextures = new Texture[0];

  [Header("Стол главного оружия")]
  public RawImage UISecondWeapon;

  private void UIVisualizatorUpdate()
  {
    switch (SecondWeapon.SecondWeaponType)
    {
      case SecondWeaponType.None:
        UISecondWeapon.texture = null; break;
      case SecondWeaponType.Staff:
        UISecondWeapon.texture = SecondWeaponTextures[0];
        break;
      case SecondWeaponType.Shield:
        UISecondWeapon.texture = SecondWeaponTextures[1];
        break;
      case SecondWeaponType.Bow:
        UISecondWeapon.texture = SecondWeaponTextures[2];
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
