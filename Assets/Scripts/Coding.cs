using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public enum WeekDay
{
  Monday,
  Thuesday,
  Wednesday,
  Thourday,
  Friday,
  Sunday,
  Saturday

}

public class Car
{
  private string carModel;
  public string CarModel
  {
    get { return carModel; }
    set { carModel = value; }
  }
}
public class Coding : MonoBehaviour
{
  public UnityEvent testEvent;
  public WeekDay WeekDay { get; set; }
  Vector3 v;
  GameObject g;
  Transform t;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

    testEvent.AddListener(Create);
    testEvent.Invoke();
  }

  // Update is called once per frame
  void Update()
  {

  }

  void Create()
  {
    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
  }
}
