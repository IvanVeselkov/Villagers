using UnityEngine;

public enum JobState
{
  Sleep,
  Work,
  Eat,
  Shop
}

public class NPCFarmer : MonoBehaviour
{
  public Transform homePosition;
  public Transform workPosition;
  public Transform tavernPosition;
  public Transform marketPosition;

  public Time GlobalTime;
  public NPCMovement movement;
  public JobState jobState;
  public void DoYourJob()
  { 
    switch(jobState)
    {
      case JobState.Sleep:
        movement.movePoint = homePosition;
        break;
      case JobState.Work:
        movement.movePoint = workPosition;
        break;
      case JobState.Eat:
        movement.movePoint = tavernPosition;
        break;
      case JobState.Shop:
        movement.movePoint = marketPosition;
        break;
    }

  }
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    jobState = JobState.Sleep;
  }

  // Update is called once per frame
  void Update()
  {
    DoYourJob();
  }
}
