using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill Logic/Dash")]
public class DashLogic : SkillLogic
{
    public float distance = 3f;
    public float duration = 0.5f;
    public bool backward = true; // "뒤로"면 true

    // Runner에 상태를 저장해야 해서: runtime 상태를 runner에 임시로 저장하는 방식 사용
    // LoL식 즉발이므로 Runner에 1개만 실행된다는 가정이 있어 안전합니다.
    private Vector3 startPos;
    private Vector3 endPos;
    private float t;

    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
        Vector3 mouse = runner.GetMouseGroundPoint();

        // 마우스 방향
        Vector3 dir = mouse - runner.transform.position;
        dir.y = 0f;
        dir = dir.sqrMagnitude < 0.0001f ? runner.transform.forward : dir.normalized;

        if (backward) dir = -dir;

        startPos = runner.transform.position;
        endPos = startPos + dir * distance;
        t = 0f;

        // 즉시 바라보는 방향 세팅(원하면)
        runner.FaceTo(runner.transform.position + (-dir), 99999f);

        // NavMeshAgent가 위치 업데이트 간섭할 수 있어서 잠시 꺼두는 방식도 가능하지만,
        // 여기서는 agent.isStopped만으로 최소 처리합니다.
    }

    public override void OnTick(SkillRunner runner, SkillDefinition def, float dt)
    {
        t += dt;
        float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

        var p = Vector3.Lerp(startPos, endPos, a);
        p.y = runner.transform.position.y;   // 높이 고정(원하는 동작이면 유지)
        runner.transform.position = p;       // 한 번만 적용
    }

    public override void OnAnimEnd(SkillRunner runner, SkillDefinition def)
    {
        // End 이벤트가 오면 종료. (SkillRunner가 캐스팅 해제)
    }
}