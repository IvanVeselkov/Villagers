using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class SecondWeaponShoot : MonoBehaviour
{
  public float sensaGor = 9.0f;//
  public float sensaVert = 9.0f;//
  public float minimumVert = -45.0f;
  public float maximumVert = 45.0f;

  [Header("Ствол орудия")]
  public Transform GunTransform;//
  [Header("Снаряд")]
  public GameObject Bullet;
  [Header("Точка спавна снарядов")]
  public Transform Barrel;
  private float Power = 0f;
  private float _rotationX = 0;

  // Start is called once before the first execution of Update after the MonoBehaviour is 
  // Update is called once per frame
  void Update()
  {
    this.transform.Rotate(0, Input.GetAxis("Mouse X")*sensaGor, 0);//

    _rotationX -= Input.GetAxis("Mouse Y") * sensaVert;
    _rotationX = Mathf.Clamp(_rotationX, minimumVert, maximumVert);
    GunTransform.localEulerAngles = new Vector3(_rotationX, 0, 0);


    Shoot();
  }

  private void Shoot()
  {
    if (Input.GetMouseButton(1))
    {
      //что то включается
      if (Input.GetMouseButton(0))
      {
        //мощность
        Power += UnityEngine.Time.deltaTime * 10;
      }

      if (Input.GetMouseButtonUp(0))
      {
        //Выстрел
        GameObject b = Instantiate(Bullet, Barrel.position, Barrel.rotation);
        b.GetComponent<Rigidbody>().AddForce(Power * Barrel.forward * 100);
        Power = 0;
      }
    }
  }
}
