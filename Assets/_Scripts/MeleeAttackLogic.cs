using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Skill Logic/Melee Attack")]
public class MeleeAttackLogic : SkillLogic
{
    public float range = 2.0f;
    public float radius = 1.2f;
    public float angleDeg = 90f;
    public int damage = 10;
    public LayerMask targetMask;

    
    [Header("Optional")]
    public bool checkLineOfSight = false;         // 벽 뒤 타격 방지
    public LayerMask obstacleMask;                // 벽/장애물 레이어

    // NonAlloc 버퍼 (ScriptableObject는 공유되므로 크기는 넉넉히)
    const int MAX_HITS = 32;
    readonly Collider[] _buffer = new Collider[MAX_HITS];

    // 한 번 공격에서 중복 타격 방지용 (공유 SO라 runner별로 관리가 필요하면 runner쪽에 두는게 더 안전)
    readonly HashSet<int> _hitSet = new HashSet<int>();
    
    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        _hitSet.Clear();

        Vector3 origin = runner.transform.position + Vector3.up * 1.0f;
        Vector3 forward = runner.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // 중심점을 앞으로 당겨서 구역이 "전방"에 더 몰리게
        Vector3 center = origin + forward * (range * 0.5f);

        int count = Physics.OverlapSphereNonAlloc(center, radius, _buffer, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = _buffer[i];
            if (c == null) continue;

            // 자기 자신 제외 (runner의 콜라이더가 타겟 마스크에 들어가 있으면 맞을 수 있음)
            if (c.transform.root == runner.transform.root) continue;

            // 실제 피격 대상(Health 같은)이 어디 달렸는지 찾기
            // 보통은 root나 상위에 달려있게 설계합니다.
            IDamageable dmg = c.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            // 중복 피격 방지 (대상 오브젝트 기준)
            int id = (dmg as Component).gameObject.GetInstanceID();
            if (!_hitSet.Add(id)) continue;

            // 각도 체크
            Vector3 to = ( (Component)dmg ).transform.position - runner.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float a = Vector3.Angle(forward, to.normalized);
            if (a > angleDeg * 0.5f) continue;

            // 거리 체크(선택): OverlapSphere만으로도 대충 되지만, range를 확실히 쓰고 싶으면
            if (to.magnitude > range + radius) continue;

            // 벽 뒤 타격 방지(선택)
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

            // 데미지 적용
            var info = new DamageInfo
            {
                attacker = runner.gameObject,
                amount = damage,
                hitPoint = c.ClosestPoint(origin),
                hitDir = to.normalized,
                skill = def
            };

            dmg.TakeDamage(info);
        }
    }
}