using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private FallSystem fall;   // 낙사 시스템(있다면)

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fall = GetComponent<FallSystem>();
    }
    
    public void MoveTo(Vector3 worldPos)
    {
        if (!agent) return;
        if (!agent.enabled) return;          // 넉백/낙사 중
        if (!agent.isOnNavMesh) return;      // NavMesh 밖

        // ✅ 낙사 중이면 이동 금지 (강력 추천)
        if (fall != null && fall.isFalling) return;
        
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
