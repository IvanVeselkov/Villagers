using UnityEngine;

[System.Serializable]
public class ResourcesObject : MonoBehaviour
{
  public string Description;
  public ResoursesType ResoursesType;
  public int Count;
  public int MaxCount = 20;

  private bool isFull;
  public bool IsFull { set { if (Count == MaxCount) { isFull = true; } else { if (Count < MaxCount) { isFull = false; } } } get { if (Count == MaxCount) { return true; } else { if (Count < MaxCount) { return false; } return false; } } }

  public ResourcesObject(ResoursesType resoursesType)
  {
    ResoursesType = resoursesType;
  }
  public ResourcesObject()
  {
    ResoursesType = ResoursesType.None;
  }
  public ResourcesObject(ResourcesObject obj)
  {
    ResoursesType = obj.ResoursesType;
    Description = obj.Description;
    Count = obj.Count;
  }

  public void SetResourcesObject(ResourcesObject obj)
  {
    ResoursesType = obj.ResoursesType;
    Description = obj.Description;
    Count = obj.Count;
  }

  public bool AddCount(int count)
  {
    if (!IsFull)
    {
      Count += count;
      return true;
    }
    return false;
  }

}


