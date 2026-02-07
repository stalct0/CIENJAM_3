using UnityEngine;
using UnityEngine.AI;

public class KnockbackController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    public bool IsLocked { get; private set; } // 이동/스킬 입력 락

    private float endTime;
    private Vector3 velocity; // world units/sec

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsLocked) return;

        float now = Time.time;
        if (now >= endTime)
        {
            IsLocked = false;
            velocity = Vector3.zero;
            return;
        }

        // 넉백 이동 적용
        float dt = Time.deltaTime;
        Vector3 disp = velocity * dt;

        // nav agent로 강제 이동
        if (agent)
        {
            // 경로 이동은 막되, Move는 가능
            agent.isStopped = true;
            agent.Move(disp);
        }
        else
        {
            transform.position += disp;
        }

        // 점점 감속(선택)
        velocity = Vector3.Lerp(velocity, Vector3.zero, 10f * dt);
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
    }
}