using System;
using UnityEngine;

public class DropObject : MonoBehaviour
{
  [Header("Спавн точка для предметов.")]
  public Transform DropSpawnPoint;
  [Header("Префаб оружие.")]
  public GameObject dropWeapon;
  [Header("Префаб дополнительное оружие.")]
  public GameObject dropSecondWeapon;
  [Header("Префаб ресурсый объект")]
  public GameObject dropResourseObject;
  [Header("Инвентарь.")]
  public Inventory Inventory;

  [Header("Кнопка выброса.")]
  public KeyCode DropCode;



  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    if (Input.GetKey(DropCode))
    {
      if (Input.GetMouseButtonDown(0))
      {
        if(Inventory.RightHand!=null)
          DropWeapon();
      }
      if(Input.GetMouseButtonDown(1))
      {
        if (Inventory.LeftHand != null)
          DropSecondWeapon();
      }
    }
  }

  public void DropWeapon()
  {
    if(Inventory.RightHand.WeaponType==WeaponType.None)
    {
      return;
    }
    Weapon w = new Weapon(Inventory.RightHand);
    var gw = Instantiate(dropWeapon, DropSpawnPoint.position, Quaternion.identity);
    gw.GetComponent<Weapon>().SetWeaponValue(w);
    gw.GetComponent<WeaponModelController>().ActivateWeapon(w.WeaponType);
    //Визуальное отображение еще надо настроить
    Inventory.RightHand.SetWeaponValue(new Weapon());
    Inventory.WeaponModelController.DisableAll();
  }


  public void DropSecondWeapon()
  {
    if (Inventory.LeftHand.SecondWeaponType == SecondWeaponType.None)
    {
      return;
    }
    SecondWeapon sw = new SecondWeapon(Inventory.LeftHand);
    var gsw = Instantiate(dropSecondWeapon, DropSpawnPoint.position, Quaternion.identity);
    gsw.GetComponent<SecondWeapon>().SetSecondWeaponValue(sw);
    gsw.GetComponent<SecondWeaponModelController>().ActivateSecondWeapon(sw.SecondWeaponType);
    //Визуальное отображение еще надо настроить
    Inventory.LeftHand.SetSecondWeaponValue(new SecondWeapon());
    Inventory.SecondWeaponModelController.DisableAll();
  }

  public void DropResourcesObject(int InventoryIndex)
  {
    ResourcesObject nrs = new ResourcesObject(Inventory.InventoryObject[InventoryIndex]);
    var grs = Instantiate(dropResourseObject, DropSpawnPoint.position, Quaternion.identity);
    grs.GetComponent<ResourcesObject>().SetResourcesObject(nrs);
    ////Inventory.InventoryObject.Remove();
    if (Inventory.InventoryObject[InventoryIndex].Count <= 0)
    {
      Inventory.InventoryObject.Remove(Inventory.InventoryObject[InventoryIndex]);
    }
    //Визуальное отображение еще надо настроить через инвентарь
    
  }
}
