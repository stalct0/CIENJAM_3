using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SkillRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Skills (assign ScriptableObjects)")]
    [SerializeField] private List<SkillDefinition> skills;

    private readonly Dictionary<SkillSlot, SkillDefinition> map = new();
    private readonly Dictionary<SkillSlot, float> cdEnd = new();

    private SkillDefinition current;
    public bool IsCasting => current != null;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        foreach (var s in skills)
        {
            if (!s) continue;
            map[s.slot] = s;
            cdEnd[s.slot] = 0f;
        }
    }

    private void Update()
    {
        if (current && current.logic)
            current.logic.OnTick(this, current, Time.deltaTime);
    }

    public bool TryCast(SkillSlot slot)
    {
        if (IsCasting) return false;
        if (!map.TryGetValue(slot, out var def)) return false;
        if (!def.logic) return false;
        if (Time.time < cdEnd[slot]) return false;

        // 쿨 시작
        cdEnd[slot] = Time.time + def.cooldown;

        // 이동 잠금
        if (def.lockMovement && agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        current = def;

        // 로직 시작(대시/타겟 방향 계산 등)
        def.logic.OnStart(this, def);

        // 애니 트리거
        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }

        return true;
    }

    // === 애니 이벤트에서 호출 ===
    public void AnimEvent_Hit()
    {
        if (current?.logic == null) return;
        current.logic.OnAnimHit(this, current);
    }

    public void AnimEvent_End()
    {
        if (current?.logic == null) return;
        current.logic.OnAnimEnd(this, current);

        // 이동 잠금 해제
        if (current.lockMovement && agent)
            agent.isStopped = false;

        current = null;
    }

    // === 유틸: 마우스 방향(바닥 히트) ===
    public Vector3 GetMouseGroundPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 2000f))
            return hit.point;

        return transform.position + transform.forward * 2f;
    }

    // === 유틸: 회전 ===
    public void FaceTo(Vector3 worldPoint, float maxDegPerSec = 99999f)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxDegPerSec * Time.deltaTime);
    }

    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
}