using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
  private InputAction Move;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    Move = InputSystem.actions.FindAction("Move");
    
  }

  // Update is called once per frame
  void Update()
  {
    Debug.Log(Move.ReadValue<Vector2>());

  }
}
