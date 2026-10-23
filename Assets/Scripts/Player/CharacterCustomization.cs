using UnityEngine;
using Mirror;
public class CharacterCustomization : NetworkBehaviour
{
  public GameObject[] Heads = new GameObject[0];

  [SyncVar(hook = nameof(OnHeadChanged))]
  public int headIndex = -1;

  public override void OnStartServer()
  {
    base.OnStartServer();
    if(Heads.Length>0&&headIndex==-1)
    {
      headIndex=Random.Range(0, Heads.Length);
    }
  }

  public override void OnStartClient()
  {
    base.OnStartClient();
    ApplyHead(headIndex);
  }

  void OnHeadChanged(int oldValue,int newValue)
  {
    ApplyHead(newValue);
  }

  void ApplyHead(int index)
  {
    if (Heads.Length == 0) return;
    for (int i = 0; i < Heads.Length; i++)
    {
      Heads[i].SetActive(i == index);
    }
  }
}
