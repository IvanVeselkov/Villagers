using System.Collections;
using UnityEngine;

public class Time : MonoBehaviour
{
  public int Hour = 0;
  public int GetTime()
  {
    return Hour;
  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  IEnumerator Start()
  {
    yield return new WaitForSeconds(120);
    Hour++;
    if(Hour==24)
    {
      Hour = 0;
    }
    Debug.Log("Day Time: "+ Hour);
    StartCoroutine(Start());
  }

  // Update is called once per frame
  void Update()
  {

  }
}
