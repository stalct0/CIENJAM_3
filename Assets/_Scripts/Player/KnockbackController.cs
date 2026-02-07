using UnityEngine;
using UnityEngine.AI;

public class KnockbackController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    [Header("Behavior")]
    [Tooltip("넉백 동안 NavMeshAgent를 꺼서 NavMesh 구속을 제거합니다. (권장)")]
    [SerializeField] private bool disableAgentDuringKnockback = true;

    [Tooltip("넉백 종료 시 NavMesh 재부착을 위해 SamplePosition을 시도할 반경")]
    [SerializeField] private float reattachSampleRadius = 2.0f;

    [Tooltip("SamplePosition 탐색에 사용할 NavMesh area mask (기본: AllAreas)")]
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    public bool IsLocked { get; private set; } // 입력/이동 락(기존 의미 유지)
    public bool IsKnockbackActive { get; private set; } // 넉백이 실제로 진행 중인지

    private float endTime;
    private Vector3 velocity; // world units/sec
    
    private bool suppressReattach;

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsKnockbackActive) return;

        float now = Time.time;
        float dt = Time.deltaTime;

        // 넉백 이동 적용
        Vector3 disp = velocity * dt;

        // ✅ NavMesh 구속 제거 상태면 Transform으로 직접 이동
        if (disableAgentDuringKnockback && agent && !agent.enabled)
        {
            transform.position += disp;
        }
        else
        {
            // 기존 방식: agent.Move (NavMesh에 붙들려 잘 안 밀릴 수 있음)
            if (agent)
            {
                agent.isStopped = true;
                agent.Move(disp);
            }
            else
            {
                transform.position += disp;
            }
        }

        // 점점 감속(선택)
        velocity = Vector3.Lerp(velocity, Vector3.zero, 10f * dt);

        if (now >= endTime)
        {
            EndKnockback();
        }
    }

    public void ApplyKnockback(Vector3 from, float distance, float duration, bool lockInput = true)
    {
        if (duration <= 0.01f) duration = 0.01f;

        Vector3 dir = (transform.position - from);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        float speed = distance / duration;
        velocity = dir * speed;

        endTime = Time.time + duration;

        IsLocked = lockInput;
        IsKnockbackActive = true;

        // ✅ 넉백 동안 NavMeshAgent 비활성화 (바깥으로 확실히 밀리게)
        if (disableAgentDuringKnockback && agent && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    private void EndKnockback()
    {
        IsKnockbackActive = false;
        IsLocked = false;
        velocity = Vector3.zero;

        // ✅ 종료 시 NavMeshAgent 재부착
        if (disableAgentDuringKnockback && agent)
        {
            if (!agent.enabled)
            {
                if (suppressReattach)
                    return;
                
                agent.enabled = true;

                // 현재 위치가 NavMesh 밖일 수 있으니 SamplePosition으로 가장 가까운 점 찾기
                if (NavMesh.SamplePosition(transform.position, out var hit, reattachSampleRadius, navMeshAreaMask))
                {
                    agent.Warp(hit.position);
                }

                agent.isStopped = false;
            }
        }
    }
    
    public void ForceStopNoReattach()
    {
        // 넉백 자체를 즉시 끝내고, 재부착만 금지
        IsKnockbackActive = false;
        IsLocked = false;
        velocity = Vector3.zero;
        suppressReattach = true;

        // agent는 여기서 켜지지 않음 (FallSystem이 끄고 떨어뜨릴 거니까)
    }
    
}