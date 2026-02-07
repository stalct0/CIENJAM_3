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
