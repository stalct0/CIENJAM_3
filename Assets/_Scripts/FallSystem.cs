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
    
    [SerializeField] private float outPushSpeed = 4.0f;  // 바깥으로 튕기는 속도
    [SerializeField] private float downPushSpeed = 2.5f; // 아래로 떨어지게 보정
    [SerializeField] private float maxHorizontalSpeed = 6.0f;
    
    public Transform towerCenter;
    
    public bool isFalling = false;

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!knockback) knockback = GetComponent<KnockbackController>();
        if (!health) health = GetComponentInChildren<HealthEX>();
        if (!rb) rb = GetComponent<Rigidbody>();
        
        if (!towerCenter)
        {
            GameObject tc = GameObject.FindGameObjectWithTag("TowerCenter");
            if (tc)
                towerCenter = tc.transform;
            else
                Debug.LogWarning("[FallSystem] TowerCenter 태그 오브젝트를 찾지 못했습니다.");
        }
        
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

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // ✅ 바깥 방향 계산 (타워 중심 기준)
            Vector3 center = towerCenter ? towerCenter.position : zone.transform.root.position;
            Vector3 outDir = (transform.position - center);
            outDir.y = 0f;

            if (outDir.sqrMagnitude < 0.0001f)
                outDir = transform.forward;
            outDir.Normalize();

            // ✅ 순간이동 대신 "속도"로 튕김
            Vector3 v = rb.linearVelocity;

            // 기존 속도와 섞고 싶으면 유지, 싫으면 덮어써도 됨
            Vector3 horizontal = outDir * outPushSpeed;

            // 너무 과하면 clamp
            if (horizontal.magnitude > maxHorizontalSpeed)
                horizontal = horizontal.normalized * maxHorizontalSpeed;

            v.x = horizontal.x;
            v.z = horizontal.z;

            // 아래로도 살짝 눌러서 확실히 낙하가 시작되게
            v.y = Mathf.Min(v.y, -downPushSpeed);

            rb.linearVelocity = v;
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