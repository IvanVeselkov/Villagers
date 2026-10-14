using UnityEngine;

public class AnimatorChanger : MonoBehaviour
{
  private Animator animatorController;
  // Start is called before the first frame update
  void Start()
  {
    animatorController = GetComponent<Animator>();
  }

  // Update is called once per frame
  void Update()
  {
    float x = Input.GetAxis("Horizontal");
    float y = Input.GetAxis("Vertical");

    animatorController.SetFloat("speed",Mathf.Abs(x)+Mathf.Abs(y),0.1f,Time.deltaTime);
  }
}
