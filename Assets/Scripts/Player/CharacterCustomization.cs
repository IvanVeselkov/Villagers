using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{
  public GameObject[] Heads = new GameObject[0];
  void Awake()
  {
    int r = Random.Range(0, Heads.Length);
    for (int i = 0; i < Heads.Length; i++)
    {
      Heads[i].SetActive(false);
    }
    Heads[r].SetActive(true);
  }
}
