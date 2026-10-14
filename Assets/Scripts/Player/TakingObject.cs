using UnityEngine;

public class TakingObject : MonoBehaviour
{
  [Header("Позиция игрока.")]
  public Transform PlayerPosition;
  [Header("Aктивная камера.")]
  public Camera Camera;
  [Header("Кнопка активации мыши.")]
  public KeyCode KeyMouseActivate;
  [Header("Расстояние для подбора объектов.")]
  public float TakingDistance;
  [Header("Инвентарь игрока.")]
  public Inventory Inventory;
  [Header("Сбросник объектов.")]
  public DropObject DropObject;
  public bool TakingObjectActivate { get; set; }
  private void ActiveteMouse()
  {
    if (Input.GetKeyDown(KeyMouseActivate))
    {
      Cursor.lockState = CursorLockMode.None;
      TakingObjectActivate = true;
    }
    else
    {
      if (Input.GetKeyUp(KeyMouseActivate))
      {
        Cursor.lockState = CursorLockMode.Locked;
        TakingObjectActivate = false;
      }
    }
  }

  private void SelectObjectMouseClick()
  {
    if (Cursor.lockState == CursorLockMode.None)
    {
      if (!Input.GetMouseButtonDown(0))
      {
        return;
      }

      Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
      RaycastHit hit;

      if (Physics.Raycast(ray, out hit))
      {
        if (hit.collider.tag == "GroundedObject")
        {
          if (Vector3.Distance(PlayerPosition.position, hit.collider.transform.position) <= TakingDistance)
          {
            if (hit.collider.gameObject.TryGetComponent<Weapon>(out var w))
            {
              if (Inventory.RightHand.WeaponType!=WeaponType.None)
              {
                DropObject.DropWeapon();
              }
              Inventory.RightHand.SetWeaponValue(w);
              Destroy(w.gameObject);//Удаляется w после выполнения блока кода
              //берем в правую руку, сбрасывается инструмент на руке
              Inventory.WeaponModelController.ActivateWeapon(Inventory.RightHand.WeaponType);
            }
            if (hit.collider.gameObject.TryGetComponent<ResourcesObject>(out var r))
            {
              Inventory.AddObjectToList(r);
              

            }
            if (hit.collider.gameObject.TryGetComponent<SecondWeapon>(out var sw))
            {
              if (Inventory.LeftHand.SecondWeaponType != SecondWeaponType.None)
              {
                DropObject.DropSecondWeapon();
              }
              Inventory.LeftHand.SetSecondWeaponValue(sw);
              Destroy(sw.gameObject);//Удаляется w после выполнения блока кода
              //берем в правую руку, сбрасывается инструмент на руке
              Inventory.SecondWeaponModelController.ActivateSecondWeapon(Inventory.LeftHand.SecondWeaponType);
            }
          }
        }
      }
    }
  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    TakingObjectActivate = false;
  }

  // Update is called once per frame
  void Update()
  {
    ActiveteMouse();
    SelectObjectMouseClick();
    if (Inventory.RightHand != null)
      print("Inventory.RightHand");
    else
      print("NULL Inventory.RightHand");
  }
}
