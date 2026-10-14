using TMPro;
using UnityEngine;

public class InfoCanvas : MonoBehaviour
{
  public string ObjectLabel;
  public TMP_Text LabelTextField;
  public GameObject TakeField;

  public Transform LookAtObject;

  public Weapon weapon;
  public SecondWeapon secondWeapon;
  public ResourcesObject ResourcesObject;

  [Header("Кнопка активации мыши.")]
  public KeyCode mouseActivetionCode;
  private Transform playerTransform;
  [Header("Дистанция активации объектов.")]
  public float ActivateDistance;
  public void SetActiveTakeField(bool b)
  {
    TakeField.SetActive(b);
  }

  public void AutomationActivateObject()
  {
    if (Vector3.Distance(this.transform.position, playerTransform.position) < ActivateDistance)
    {
      if (Input.GetKey(mouseActivetionCode))
      {
        SetActiveTakeField(true);
      }
    }
    else
    {
      SetActiveTakeField(false);
    }
  }

  // Start is called before the first frame update
  void Awake()
  {
    SetActiveTakeField(false);
    LabelTextField.text = ObjectLabel;
    LookAtObject = GameObject.FindGameObjectWithTag("MainCamera").transform;
    playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
  }

  private void AddDescription()
  {
    if (weapon != null)
    {
      ObjectLabel = weapon.WeaponType.ToString();

    }
    if (secondWeapon != null)
    {
      ObjectLabel = secondWeapon.SecondWeaponType.ToString();
    }
    if (ResourcesObject != null)
    {
      ObjectLabel = ResourcesObject.ResoursesType.ToString()+"("+ResourcesObject.Count.ToString()+")";
    }
    LabelTextField.text = ObjectLabel;
  }
  private void FixedUpdate()
  {
    AutomationActivateObject();
    AddDescription();
    if (LookAtObject != null)
    {
      this.transform.LookAt(LookAtObject.position);
    }
  }
}
