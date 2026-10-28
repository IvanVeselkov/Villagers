using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace Villager
{
  public class CatController : NetworkBehaviour
  {
    public Transform MovePoint;
    private NavMeshAgent _navMeshAgent;
    private Vector3 GenerateRandomTransformPoint()
    {
      return new Vector3(Random.Range(-10,10),Random.Range(-10,10),Random.Range(-10,10));
    }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      _navMeshAgent = GetComponent<NavMeshAgent>();
    }

        // Update is called once per frame
    void Update()
    {
      _navMeshAgent.SetDestination(GenerateRandomTransformPoint());
    }
  }
}
