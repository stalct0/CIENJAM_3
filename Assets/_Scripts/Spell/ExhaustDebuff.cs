using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ExhaustDebuff : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;

    private Coroutine co;
    private NavMeshAgent agent;
    private float baseSpeed;

    private GameObject vfx;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent) baseSpeed = agent.speed;
    }

    public void Apply(float duration, float moveSpeedMultiplier, float damageMultiplier,
        GameObject vfxPrefab, Vector3 vfxOffset)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, moveSpeedMultiplier, damageMultiplier, vfxPrefab, vfxOffset));
    }

    private IEnumerator Co(float duration, float moveSpeedMultiplier, float damageMultiplier,
        GameObject vfxPrefab, Vector3 vfxOffset)
    {
        IsActive = true;
        DamageMultiplier = Mathf.Clamp(damageMultiplier, 0.01f, 1f);

        // ✅ VFX
        EnsureVFX(vfxPrefab, vfxOffset);

        if (agent)
        {
            agent.speed = baseSpeed * Mathf.Clamp(moveSpeedMultiplier, 0.01f, 1f);
        }

        yield return new WaitForSeconds(duration);

        if (agent) agent.speed = baseSpeed;
        DamageMultiplier = 1f;
        IsActive = false;

        CleanupVFX();
        co = null;
    }

    private void EnsureVFX(GameObject prefab, Vector3 offset)
    {
        if (vfx) return;
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