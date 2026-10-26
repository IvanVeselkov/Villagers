using UnityEngine;
using UnityEngine.UI;

public class HPSliderUpdater : MonoBehaviour
{
  public Damageable Damageable;
  public Slider Slider;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void FixedUpdate()
  {
    Slider.value = Damageable.Health;
  }
}
