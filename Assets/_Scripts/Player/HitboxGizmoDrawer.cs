using UnityEngine;

public class HitboxGizmoDrawer : MonoBehaviour
{
    [Header("Refs")]
    public SkillRunner runner;

    [Header("Preview mode")]
    public bool drawOnlyWhenCasting = true;  // 캐스팅 중인 스킬만 표시
    public SkillDefinition previewSkill;     // drawOnlyWhenCasting=false일 때 이걸 표시

    [Header("Gizmo style")]
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    public Color arcColor   = new Color(1f, 0.8f, 0.2f, 0.9f);
    public int arcSegments = 24;

    private void Reset()
    {
        runner = GetComponentInParent<SkillRunner>();
        if (!runner) runner = GetComponent<SkillRunner>();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return; // 런타임 중만 보고 싶으면 유지
        if (!runner) runner = GetComponentInParent<SkillRunner>();

        SkillDefinition def = null;

        if (drawOnlyWhenCasting)
        {
            if (!runner || !runner.IsCasting) return;
            def = runner.Debug_CurrentSkill;
        }
        else
        {
            def = previewSkill != null ? previewSkill : (runner ? runner.Debug_CurrentSkill : null);
        }

        if (def == null || def.logic == null) return;

        // MeleeAttackLogic / WGuardAndAttackLogic 둘 다 "range/radius/angleDeg"를 갖는다고 가정
        if (def.logic is MeleeAttackLogic melee)
        {
            DrawFanAndSphere(melee.range, melee.radius, melee.angleDeg);
            return;
        }

        if (def.logic is WGuardAndAttackLogic w)
        {
            DrawFanAndSphere(w.range, w.radius, w.angleDeg);
            return;
        }

        // 다른 로직은 필요하면 여기서 케이스 추가
    }

    private void DrawFanAndSphere(float range, float radius, float angleDeg)
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        float r = Mathf.Max(0.01f, radius);
        float dist = Mathf.Max(0f, range);

        // Melee 판정과 동일하게 center = origin + forward*(range*0.5)
        Vector3 center = origin + forward * (dist * 0.5f);

        // Sphere(OverlapSphere) 시각화
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(center, r);

        // 부채꼴 시각화(각도선 + 호)
        Gizmos.color = arcColor;
        float half = angleDeg * 0.5f;

        Vector3 leftDir  = Quaternion.Euler(0f, -half, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f,  half, 0f) * forward;

        // 각도선(대략 range 길이로)
        Gizmos.DrawLine(origin, origin + leftDir * dist);
        Gizmos.DrawLine(origin, origin + rightDir * dist);

        // 호(arc)
        int seg = Mathf.Max(6, arcSegments);
        Vector3 prev = origin + leftDir * dist;
        for (int i = 1; i <= seg; i++)
        {
            float t = i / (float)seg;
            float yaw = Mathf.Lerp(-half, half, t);
            Vector3 d = Quaternion.Euler(0f, yaw, 0f) * forward;
            Vector3 p = origin + d * dist;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // (옵션) 중심선
        Gizmos.DrawLine(origin, origin + forward * dist);
    }
}