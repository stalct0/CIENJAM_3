using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[CreateAssetMenu(menuName = "Game/Skill Logic/Melee Attack")]
public class MeleeAttackLogic : SkillLogic
{
    [Header("Base hit shape")]
    public float range = 2.0f;
    public float radius = 1.2f;
    public float angleDeg = 90f;

    [Header("Base damage (before charge scaling)")]
    public int damage = 10;

    [Header("Target mask (physics candidate filter)")]
    public LayerMask targetMask;

    [Header("Optional line-of-sight")]
    public bool checkLineOfSight = false;
    public LayerMask obstacleMask;

    private const int MAX_HITS = 32;
    private readonly Collider[] _buffer = new Collider[MAX_HITS];
    private readonly HashSet<int> _unique = new HashSet<int>();

    // Reflection cache (SkillRunner에 CurrentCharge01이 없을 수도 있으니 안전 처리)
    private static PropertyInfo _piCharge01;

    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
        // 필요하면 사용 (대부분 비워둬도 됨)
    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        if (runner == null) return;

        _unique.Clear();

        Vector3 origin = runner.transform.position + Vector3.up * 1.0f;

        Vector3 forward = runner.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        // ==============================
        // Charge01 (0~1) 읽기
        // ==============================
        float c01 = GetRunnerFloat(runner, ref _piCharge01, "CurrentCharge01", 0f);
        c01 = Mathf.Clamp01(c01);

        // ==============================
        // Hit/VFX/Damage scaling
        // SkillDefinition의 chargeScale을 사용
        // ==============================
        float hitScale = 1f;
        float damageScale = 1f;

        if (def != null && def.isChargeSkill && def.chargeScale != null)
        {
            // curve가 있으면 curve로 조정 (SkillDefinition에 이미 있음)
            float t = c01;
            if (def.chargeScale.curve != null)
                t = Mathf.Clamp01(def.chargeScale.curve.Evaluate(c01));

            hitScale = Mathf.Lerp(def.chargeScale.minHitScale, def.chargeScale.maxHitScale, t);
            damageScale = Mathf.Lerp(def.chargeScale.minDamageScale, def.chargeScale.maxDamageScale, t);
        }

        hitScale = Mathf.Max(0.01f, hitScale);
        damageScale = Mathf.Max(0.01f, damageScale);

        float scaledRadius = radius * hitScale;

        // OverlapSphere 중심: 전방에 살짝 치우치게(원래 코드 스타일 유지)
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

            // 자기 자신 제외
            if (col.transform.root == runner.transform.root) continue;

            // 데미지 받을 대상 찾기
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            // 동일 대상 중복 타격 방지(한 Hit 이벤트에서 1회)
            int key = ((Component)target).gameObject.GetInstanceID();
            if (!_unique.Add(key)) continue;

            // 부채꼴 판정
            Vector3 to = ((Component)target).transform.position - runner.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float ang = Vector3.Angle(forward, to.normalized);
            if (ang > angleDeg * 0.5f) continue;

            // 거리 판정 (반경 보정 포함)
            if (to.magnitude > range + scaledRadius) continue;

            // (옵션) 시야 가림 판정
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

            // ==============================
            // Final Damage (charge scaled)
            // ==============================
            int baseDmg = runner.GetCurrentDamage(def); // def.useChargeDamage면 min~max로 계산됨
            int finalDamage = Mathf.RoundToInt(baseDmg * damageScale);
            CombatIdentity attackerId = runner.GetComponentInParent<CombatIdentity>();
            if (attackerId == null) attackerId = runner.GetComponentInChildren<CombatIdentity>();
            
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
                
                // ✅ R은 가드 절반 관통
                guardBypass = def != null ? def.guardBypass : GuardBypassType.None,
                guardBypassFactor = def != null ? def.guardBypassFactor : 1f,

                // (선택) 스킬별 넉백이 있으면 여기 설정
                hasKnockback = false,
            };

            target.TakeDamage(info);
        }
    }

    private static float GetRunnerFloat(SkillRunner runner, ref PropertyInfo cache, string propName, float fallback)
    {
        if (runner == null) return fallback;

        if (cache == null)
        {
            cache = runner.GetType().GetProperty(
                propName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        if (cache == null) return fallback;

        object v = cache.GetValue(runner, null);
        return v is float f ? f : fallback;
    }
}