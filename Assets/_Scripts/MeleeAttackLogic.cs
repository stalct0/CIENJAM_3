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

    [Header("Damage")]
    public int damage = 10;

    [Header("Masks")]
    public LayerMask targetMask;

    [Header("Optional")]
    public bool checkLineOfSight = false;
    public LayerMask obstacleMask;

    const int MAX_HITS = 32;
    readonly Collider[] _buffer = new Collider[MAX_HITS];
    readonly HashSet<int> _hitSet = new HashSet<int>();

    // 리플렉션 캐시 (있으면 사용, 없으면 1.0 처리)
    static PropertyInfo _piHitScale;
    static PropertyInfo _piDamageScale;

    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
        // 필요 없으면 비워도 됨
    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        if (runner == null) return;

        _hitSet.Clear();

        Vector3 origin = runner.transform.position + Vector3.up * 1.0f;
        Vector3 forward = runner.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        // ✅ 차지 기반 판정 스케일 (없으면 1.0)
        float hitScale = GetRunnerScale(runner, ref _piHitScale, "CurrentHitScale", 1f);
        hitScale = Mathf.Max(0.01f, hitScale);

        // ✅ 차지 기반 데미지 스케일 (없으면 1.0)
        float damageScale = GetRunnerScale(runner, ref _piDamageScale, "CurrentDamageScale", 1f);
        damageScale = Mathf.Max(0.01f, damageScale);

        // 차지 비례로 반경 증가
        float scaledRadius = radius * hitScale;

        // 전방 중심(기본은 range 그대로, radius만 커짐)
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
            Collider c = _buffer[i];
            if (c == null) continue;

            // 자기 자신 제외
            if (c.transform.root == runner.transform.root) continue;

            IDamageable dmg = c.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            int id = ((Component)dmg).gameObject.GetInstanceID();
            if (!_hitSet.Add(id)) continue;

            Vector3 to = ((Component)dmg).transform.position - runner.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            // 부채꼴 각도 체크
            float a = Vector3.Angle(forward, to.normalized);
            if (a > angleDeg * 0.5f) continue;

            // 거리 체크 (radius 커진 만큼 여유 반영)
            if (to.magnitude > range + scaledRadius) continue;

            // 시야 가림 체크(옵션)
            if (checkLineOfSight)
            {
                Vector3 targetPoint = c.ClosestPoint(origin);
                Vector3 dir = (targetPoint - origin);
                float dist = dir.magnitude;

                if (dist > 0.001f)
                {
                    dir /= dist;
                    if (Physics.Raycast(origin, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                        continue;
                }
            }

            // ✅ 차지 비례 데미지 적용
            int finalDamage = runner.GetCurrentDamage(def);
            
            var info = new DamageInfo
            {
                attacker = runner.gameObject,
                amount = finalDamage,
                hitPoint = c.ClosestPoint(origin),
                hitDir = to.normalized,
                skill = def
            };

            dmg.TakeDamage(info);
        }
    }

    private static float GetRunnerScale(SkillRunner runner, ref PropertyInfo cache, string propName, float fallback)
    {
        if (runner == null) return fallback;

        if (cache == null)
        {
            cache = runner.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (cache == null) return fallback;

        object v = cache.GetValue(runner, null);
        if (v is float f) return f;

        return fallback;
    }
}