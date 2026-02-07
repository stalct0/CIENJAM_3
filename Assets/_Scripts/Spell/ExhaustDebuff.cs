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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent) baseSpeed = agent.speed;
    }

    public void Apply(float duration, float moveSpeedMultiplier, float damageMultiplier)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, moveSpeedMultiplier, damageMultiplier));
    }

    private IEnumerator Co(float duration, float moveSpeedMultiplier, float damageMultiplier)
    {
        IsActive = true;
        DamageMultiplier = Mathf.Clamp(damageMultiplier, 0.01f, 1f);

        if (agent)
        {
            agent.speed = baseSpeed * Mathf.Clamp(moveSpeedMultiplier, 0.01f, 1f);
        }

        yield return new WaitForSeconds(duration);

        if (agent) agent.speed = baseSpeed;
        DamageMultiplier = 1f;
        IsActive = false;
        co = null;
    }
}