using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory : MonoBehaviour
{
  [Header("Правая рука.")]
  public Weapon RightHand;
  public WeaponModelController WeaponModelController;
  [Header("Левая рука.")]
  public SecondWeapon LeftHand;
  public SecondWeaponModelController SecondWeaponModelController;
  [Header("Инвентарь.")]
  public List<ResourcesObject> InventoryObject = new List<ResourcesObject>();

  [Header("Максимальное число предметов.")]
  public int MaxCountSlots;

  [Header("Визуальное отображение слотов интерфейса.")]
  public List<InventorySlot> InventorySlots = new List<InventorySlot>();
  public void AddObjectToList(ResourcesObject resourcesObject)
  {
    if (InventoryObject.Count > 0)
    {
      if (InventoryObject.Count < MaxCountSlots)
      {
        for (int i = 0; i < InventoryObject.Count; i++)
        {
          if (InventoryObject[i].ResoursesType == resourcesObject.ResoursesType)
          {
            if (!InventoryObject[i].IsFull)
            {
              int count = resourcesObject.Count;
              int FreeSpaceCount = InventoryObject[i].MaxCount - InventoryObject[i].Count;
              int ost = resourcesObject.Count - FreeSpaceCount;
              if (ost < 0)
              {
                ost = 0;
              }
              InventoryObject[i].AddCount(resourcesObject.Count - ost);
              resourcesObject.Count -= resourcesObject.Count - ost;
              if (resourcesObject.Count == 0)
              {
                Destroy(resourcesObject.gameObject);
                return;
              }
            }
          }
        }
      }
      else
      {
        return;
      }
    }
    AddInventoryObj(resourcesObject);

    return;
  }

  //Тут другое
  private void AddInventoryObj(ResourcesObject resourcesObject)
  {
    InventoryObject.Add(resourcesObject);
    resourcesObject.gameObject.transform.SetParent(this.gameObject.transform, false);
    resourcesObject.gameObject.transform.position = Vector3.zero;
    resourcesObject.gameObject.SetActive(false);
  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }
}

