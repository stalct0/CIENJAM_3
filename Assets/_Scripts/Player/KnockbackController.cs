using UnityEngine;
using UnityEngine.AI;

public class KnockbackController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    public bool IsLocked { get; private set; }          // 입력/이동 락
    public bool IsKnockbackActive { get; private set; } // 넉백 진행 여부

    private float endTime;
    private Vector3 velocity; // world units/sec

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsKnockbackActive) return;

        float now = Time.time;
        float dt = Time.deltaTime;

        Vector3 disp = velocity * dt;

        if (agent)
        {
            agent.isStopped = true;
            agent.Move(disp);
        }
        else
        {
            transform.position += disp;
        }

        // 감속(원하면 값 조절)
        velocity = Vector3.Lerp(velocity, Vector3.zero, 10f * dt);

        if (now >= endTime)
            EndKnockback();
    }

    public void ApplyKnockback(Vector3 from, float distance, float duration, bool lockInput = true)
    {
        duration = Mathf.Max(0.01f, duration);

        Vector3 dir = (transform.position - from);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        float speed = distance / duration;
        velocity = dir * speed;

        endTime = Time.time + duration;

        IsLocked = lockInput;
        IsKnockbackActive = true;
    }

    private void EndKnockback()
    {
        IsKnockbackActive = false;
        IsLocked = false;
        velocity = Vector3.zero;

        if (agent)
        {
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
    }
}