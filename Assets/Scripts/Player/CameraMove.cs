using UnityEngine;
using Mirror;
public class CameraMove :NetworkBehaviour
{
  public Vector3 targerDelta;
  [SerializeField] private Transform target;
  public float rotSpeed = 1.5f;
  private float _rotY;
  private float _rotX;
  private Vector3 _offset;
  void Start()
  {
    _rotY = transform.eulerAngles.y;
    _rotX = transform.eulerAngles.x;
    _offset = target.position - transform.position;
  }
  void LateUpdate()
  {
    if (isOwned)
    {
      float horInput = Input.GetAxis("Horizontal");
      if (horInput != 0)
      {
        _rotY += horInput * rotSpeed;
      }
      else
      {
        _rotY += Input.GetAxis("Mouse X") * rotSpeed * 3;
      }

      _rotX += (-Input.GetAxis("Mouse Y")) * rotSpeed * 3;

      Quaternion rotation = Quaternion.Euler(_rotX, _rotY, 0);
      transform.position = target.position - (rotation * _offset);
      Vector3 lookPos = target.position + targerDelta;
      transform.LookAt(lookPos);
    }
    else
    {
      this.gameObject.SetActive(false);
    }
  }
}
