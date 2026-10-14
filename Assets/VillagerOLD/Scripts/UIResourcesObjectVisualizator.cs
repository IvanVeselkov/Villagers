using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourcesObjectVisualizator : MonoBehaviour
{
  [Header("Инвентарь")]
  public Inventory inventory;

  [Header("Список")]
  public Texture WoodTexture;
  public Texture FoodTexture;
  public Texture IronRudaTexture;
  public Texture IronTexture;

  [Header("Слоты инвенторя")]
  public List<GameObject> InventorySlots = new List<GameObject>();

  private void UIVisualizatorUpdate()
  {
    for (int i = 0; i < inventory.InventoryObject.Count; i++)
    {
      InventorySlot IS = InventorySlots[i].GetComponent<InventorySlot>();
      switch (inventory.InventoryObject[i].ResoursesType)
      {
        case ResoursesType.None:
          IS.SlotTexture.texture = null;
          IS.SlotLabel.text = " "; 
          break;
        case ResoursesType.Wood:
          IS.SlotTexture.texture = WoodTexture;
          IS.SlotLabel.text = string.Format("x{0}", inventory.InventoryObject[i].Count);
          break;
        case ResoursesType.Food:
          IS.SlotTexture.texture = FoodTexture;
          IS.SlotLabel.text = string.Format("x{0}", inventory.InventoryObject[i].Count);

          break;
        case ResoursesType.IronRuda:
          IS.SlotTexture.texture = IronRudaTexture;
          IS.SlotLabel.text = string.Format("x{0}", inventory.InventoryObject[i].Count);

          break;
        case ResoursesType.Iron:
          IS.SlotTexture.texture = IronTexture;
          IS.SlotLabel.text = string.Format("x{0}", inventory.InventoryObject[i].Count);

          break;
      }
    }
  }
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    UIVisualizatorUpdate();
  }
}
