using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill Logic/Melee Attack")]
public class MeleeAttackLogic : SkillLogic
{
    public float range = 2.0f;
    public float radius = 1.2f;
    public float angleDeg = 90f;
    public int damage = 10;
    public LayerMask targetMask;

    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
        // 공격 시작 시 마우스 방향으로 즉시 회전
        Vector3 mouse = runner.GetMouseGroundPoint();
        runner.FaceTo(mouse, 99999f);
    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        Vector3 origin = runner.transform.position + Vector3.up * 1.0f;

        // 전방 기준 구역
        Vector3 forward = runner.transform.forward;

        Collider[] hits = Physics.OverlapSphere(origin + forward * (range * 0.5f), radius, targetMask);

        foreach (var c in hits)
        {
            Vector3 to = c.transform.position - runner.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float a = Vector3.Angle(forward, to.normalized);
            if (a <= angleDeg * 0.5f)
            {
                // 여기서 실제 데미지 시스템으로 연결
                Debug.Log($"Hit {c.name} for {damage}");
            }
        }
    }

    public override void OnAnimEnd(SkillRunner runner, SkillDefinition def)
    {
        // 종료는 runner가 처리
    }
}