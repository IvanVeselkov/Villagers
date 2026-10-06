using UnityEngine;
using UnityEngine.AI;

public class NPCMovement : MonoBehaviour
{
  private NavMeshAgent npcAgent;
  public Transform movePoint;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    npcAgent = GetComponent<NavMeshAgent>();
  }

  // Update is called once per frame
  void FixedUpdate()
  {
    npcAgent.SetDestination(movePoint.position);
  }
}
