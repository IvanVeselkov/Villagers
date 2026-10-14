using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDropMenu : MonoBehaviour
{
  public RawImage RawImage;
  public Slider CountItemSlider;
  public TMP_Text ItemDescriptiom;

  public GameObject ResourceObjectPrefab;

  private ResourcesObject SelectedResourcesObject;
  public void Inicialize(InventorySlot InventorySlot)
  {
    RawImage = InventorySlot.SlotTexture;
    ItemDescriptiom.text = InventorySlot.SlotResource.Description;
    CountItemSlider.maxValue= InventorySlot.SlotResource.Count;
    SelectedResourcesObject = new ResourcesObject(InventorySlot.SlotResource);
  }

  public void DropObject()
  {
    GameObject res = Instantiate(ResourceObjectPrefab,transform.position,Quaternion.identity);

    res.GetComponent<ResourcesObject>().Description = SelectedResourcesObject.Description;
    res.GetComponent<ResourcesObject>().Count = (int)CountItemSlider.value;
    res.GetComponent<ResourcesObject>().ResoursesType = SelectedResourcesObject.ResoursesType;
    res.GetComponent<ResourcesObject>().MaxCount=SelectedResourcesObject.MaxCount;
  }
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }
}
