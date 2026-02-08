using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GhostBuff : MonoBehaviour
{
    private Coroutine co;
    private NavMeshAgent agent;
    private float baseSpeed;

    private GameObject vfx;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent) baseSpeed = agent.speed;
    }

    public void Apply(float duration, float bonusPercent, GameObject vfxPrefab, Vector3 vfxOffset)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, bonusPercent, vfxPrefab, vfxOffset));
    }

    private IEnumerator Co(float duration, float bonusPercent, GameObject vfxPrefab, Vector3 vfxOffset)
    {
        // ✅ VFX (없으면 생성)
        EnsureVFX(vfxPrefab, vfxOffset);

        if (agent)
        {
            // 중첩 방지: baseSpeed 기준으로 설정
            agent.speed = baseSpeed * (1f + bonusPercent);
        }

        yield return new WaitForSeconds(duration);

        if (agent) agent.speed = baseSpeed;
        CleanupVFX();
        co = null;
    }

    private void EnsureVFX(GameObject prefab, Vector3 offset)
    {
        if (vfx) return;              // 재시전 시 기존 유지
        if (!prefab) return;

        vfx = Instantiate(prefab, transform);
        vfx.transform.localPosition = offset;
        vfx.transform.localRotation = Quaternion.identity;
    }

    private void CleanupVFX()
    {
        if (!vfx) return;
        Destroy(vfx);
        vfx = null;
    }
}