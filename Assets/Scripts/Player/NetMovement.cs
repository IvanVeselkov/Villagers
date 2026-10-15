using UnityEngine;
using Mirror;

public class NetMovement : NetworkBehaviour
{

  public float rotSpeed = 0.0f;
  [SerializeField] private Transform target;
  public float moveSpeed = 9.0f;
  public float jumpSpeed = 15.0f;
  public float gravity = -9.8f;
  public float terminalVelocity = -10.0f;
  public float minFall = -1.5f;
  private float _vertSpeed;
  private CharacterController _charController;
  void Start()
  {
    _charController = GetComponent<CharacterController>();
    _vertSpeed = minFall;
    Cursor.lockState = CursorLockMode.Locked;
  }
  void Update()
  {
    if (isOwned)
    {
      Vector3 movement = Vector3.zero;
      float horInput = Input.GetAxis("Horizontal");
      float vertInput = Input.GetAxis("Vertical");
      if (horInput != 0 || vertInput != 0)
      {
        movement.x = horInput * moveSpeed;
        movement.z = vertInput * moveSpeed;
        movement = Vector3.ClampMagnitude(movement, moveSpeed);
        Quaternion tmp = target.rotation;
        target.eulerAngles = new Vector3(0, target.eulerAngles.y, 0);
        movement = target.TransformDirection(movement);
        target.rotation = tmp;
        transform.rotation = Quaternion.LookRotation(movement);
        Quaternion direction = Quaternion.LookRotation(movement);
        transform.rotation = Quaternion.Lerp(transform.rotation, direction, rotSpeed * Time.deltaTime);
      }
      if (_charController.isGrounded)
      {
        if (Input.GetButtonDown("Jump"))
        {
          _vertSpeed = jumpSpeed;
        }
        else
        {
          _vertSpeed = minFall;
        }
      }
      else
      {
        _vertSpeed += gravity * 5 * Time.deltaTime;
        if (_vertSpeed < terminalVelocity)
        {
          _vertSpeed = terminalVelocity;
        }
      }
      movement.y = _vertSpeed;
      movement *= Time.deltaTime;
      _charController.Move(movement);
    }
  }
}