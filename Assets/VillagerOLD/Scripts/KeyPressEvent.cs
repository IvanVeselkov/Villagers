using UnityEngine;
using UnityEngine.Events;

public class KeyPressEvent : MonoBehaviour
{
  public KeyCode keyCode;

  public UnityEvent DoSomethingEventBy;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    if (Input.GetKeyDown(keyCode))
    {
      DoSomethingEventBy.Invoke();
    }
  }
}
