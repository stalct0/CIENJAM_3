using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SkillRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Skills")]
    [SerializeField] private List<SkillDefinition> skills;

    private Dictionary<SkillSlot, SkillDefinition> skillMap;
    private Dictionary<SkillSlot, float> cooldownEnd;

    private bool isCasting;
    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        skillMap = new Dictionary<SkillSlot, SkillDefinition>();
        cooldownEnd = new Dictionary<SkillSlot, float>();

        foreach (var s in skills)
        {
            skillMap[s.slot] = s;
            cooldownEnd[s.slot] = 0f;
        }
    }

    public void TryCast(SkillSlot slot)
    {
        if (isCasting) return;
        if (!skillMap.ContainsKey(slot)) return;
        if (Time.time < cooldownEnd[slot]) return;

        SkillDefinition skill = skillMap[slot];

        // 쿨타임 시작
        cooldownEnd[slot] = Time.time + skill.cooldown;

        // 이동 잠금
        if (skill.lockMovement && agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        isCasting = true;

        // 애니메이션 실행
        animator.ResetTrigger(skill.animatorTrigger);
        animator.SetTrigger(skill.animatorTrigger);
    }

    public void OnSkillHit()
    {
        // 여기서 데미지 / 판정 / 이펙트
        Debug.Log("Skill!");
    }

    public void OnSkillEnd()
    {
        isCasting = false;

        if (agent)
            agent.isStopped = false;
    }
}