using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class FallSystem : MonoBehaviour
{
    [Header("Rule")]
    public bool requireKnockbackToStartFall = false; // 넉백일 때만 낙하 전환

    [Header("Refs")]
    public NavMeshAgent agent;
    public KnockbackController knockback;
    public HealthEX health;
    public Rigidbody rb;

    [Header("Respawn optional")]
    public bool respawnInsteadOfDeath = false;
    public Transform respawnPoint;
    
    [SerializeField] private float outPushDistance = 0.6f; // 바깥으로 살짝
    [SerializeField] private float outPushUp = 0.05f;      // 바닥/벽 끼임 방지용 Y 보정(아주 작게)
    
    public Transform towerCenter;
    
    public bool isFalling = false;

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!knockback) knockback = GetComponent<KnockbackController>();
        if (!health) health = GetComponentInChildren<HealthEX>();
        if (!rb) rb = GetComponent<Rigidbody>();

        // 평상시: NavMesh 이동, Rigidbody는 비활성(키네마틱)
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsZone(other, "Killzone"))
        {
            Debug.Log("Killzone");
            TryEnterFalling(other);
            return;
        }

        if (IsZone(other, "Falldeath"))
        {
            if (isFalling)
                ResolveFallDeath();
            return;
        }
    }

    private bool IsZone(Collider other, string tag)
    {
        if (other.CompareTag(tag)) return true;
        var t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    private void TryEnterFalling(Collider zone)
    {
        if (isFalling) return;

        if (requireKnockbackToStartFall)
        {
            if (knockback == null || !knockback.IsLocked)
                return;
        }
        
        Vector3 center = towerCenter != null
            ? towerCenter.position
            : zone.transform.root.position; // 보험용

        Vector3 dir = transform.position - center;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        dir.Normalize();

    // 바깥으로 살짝 튕김
        transform.position += dir * outPushDistance + Vector3.up * outPushUp;
        
        // 낙하 시작: 넉백이 agent 재부착하지 못하게 정리
        if (knockback != null)
        {
            // 넉백 속도/락은 끊어도 되고, 유지해도 되는데
            // 핵심은 "재부착"을 못 하게 하는 것
            knockback.ForceStopNoReattach();
        }
        
        isFalling = true;

        // NavMesh 끄기
        if (agent)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        // Rigidbody 낙하 시작
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void ResolveFallDeath()
    {
        // 여기서 즉사/리스폰 처리
        if (respawnInsteadOfDeath && respawnPoint != null)
        {
            // RB 정리
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            transform.position = respawnPoint.position;

            // Agent 복구
            if (agent)
            {
                agent.enabled = true;
                agent.Warp(respawnPoint.position);
                agent.isStopped = false;
            }

            isFalling = false;
        }
        else
        {
            if (health != null) health.hp = 0;
        }
    }
}