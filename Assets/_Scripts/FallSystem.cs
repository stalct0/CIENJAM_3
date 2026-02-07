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

    private bool isFalling = false;

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