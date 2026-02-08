using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName="Game/Skill Logic/W Guard And Attack")]
public class WGuardAndAttackLogic : SkillLogic
{
    [Header("Attack shape (same as melee)")]
    public float range = 2.0f;
    public float radius = 1.2f;
    public float angleDeg = 90f;
    public LayerMask targetMask;

    [Header("Optional LOS")]
    public bool checkLineOfSight = false;
    public LayerMask obstacleMask;
    
    private const int MAX_HITS = 32;
    private readonly Collider[] _buffer = new Collider[MAX_HITS];
    private readonly HashSet<int> _unique = new HashSet<int>();

    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
    }

    public override void OnCustomEvent(SkillRunner runner, SkillDefinition def, string evt)
    {
        // 가드 시작
        Debug.Log("[W] OnCustomEvent " + evt + " on " + runner.name);
        var defense = runner.GetComponent<DefenseController>();
        if (!defense) return;
        
        switch (evt)
        {
            case "GuardStart":
                defense.guardActive = true;
                break;

            case "GuardEnd":
                defense.guardActive = false;
                break;
        }
    }

    public override void OnAnimStart(SkillRunner runner, SkillDefinition def)
    {

    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        if (!runner) return;

        _unique.Clear();

        Vector3 origin = runner.transform.position + Vector3.up * 1.0f;

        Vector3 forward = runner.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        float scaledRadius = Mathf.Max(0.01f, radius);
        Vector3 center = origin + forward * (range * 0.5f);

        int count = Physics.OverlapSphereNonAlloc(
            center,
            scaledRadius,
            _buffer,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = _buffer[i];
            if (!col) continue;

            if (col.transform.root == runner.transform.root) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            int key = ((Component)target).gameObject.GetInstanceID();
            if (!_unique.Add(key)) continue;

            Vector3 to = ((Component)target).transform.position - runner.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float ang = Vector3.Angle(forward, to.normalized);
            if (ang > angleDeg * 0.5f) continue;

            if (to.magnitude > range + scaledRadius) continue;

            if (checkLineOfSight)
            {
                Vector3 hitPointTmp = col.ClosestPoint(origin);
                Vector3 dir = hitPointTmp - origin;
                float dist = dir.magnitude;

                if (dist > 0.001f)
                {
                    dir /= dist;
                    if (Physics.Raycast(origin, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                        continue;
                }
            }

            CombatIdentity attackerId = runner.GetComponentInParent<CombatIdentity>();
            if (attackerId == null) attackerId = runner.GetComponentInChildren<CombatIdentity>();

            // 데미지: W는 차지 스킬이 아니니 보통 고정
            int finalDamage = def.baseDamage;

            var info = new DamageInfo
            {
                attacker = runner.gameObject,
                attackerOwnerId = attackerId != null ? attackerId.OwnerId : 0,
                attackerTeam = attackerId != null ? attackerId.Team : TeamId.None,
                attackerEntityId = attackerId != null ? attackerId.EntityId : 0,

                amount = finalDamage,
                hitPoint = col.ClosestPoint(origin),
                hitDir = to.normalized,
                skill = def,

                // W 공격은 가드 관통 없음(기본). 필요하면 여기서 설정
                guardBypass = GuardBypassType.None,
                guardBypassFactor = 1f
            };

            target.TakeDamage(info);
        }
    }

    public override void OnAnimEnd(SkillRunner runner, SkillDefinition def)
    {
        // 종료 시 가드 강제 OFF (남아있으면 버그남)
        var defense = runner.GetComponent<DefenseController>();
        if (defense) defense.guardActive = false;
    }
}