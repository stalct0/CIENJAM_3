using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GhostBuff : MonoBehaviour
{
    private Coroutine co;
    private NavMeshAgent agent;
    private float baseSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent) baseSpeed = agent.speed;
    }

    public void Apply(float duration, float bonusPercent)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, bonusPercent));
    }

    private IEnumerator Co(float duration, float bonusPercent)
    {
        if (agent)
        {
            // 중첩 방지: baseSpeed 기준으로 설정
            agent.speed = baseSpeed * (1f + bonusPercent);
        }

        yield return new WaitForSeconds(duration);

        if (agent) agent.speed = baseSpeed;
        co = null;
    }
}