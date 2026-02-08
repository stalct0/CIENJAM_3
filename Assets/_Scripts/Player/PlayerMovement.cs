using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    public void MoveTo(Vector3 worldPos)
    {
        if (!agent) return;
        if (!agent.enabled) return;          // 넉백/낙사 중
        if (!agent.isOnNavMesh) return;      // NavMesh 밖
        
        agent.SetDestination(worldPos);
    }
    
    public void CancelMove()
    {
        if (!agent) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.isStopped = false;
    }
}
