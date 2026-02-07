using UnityEngine;

[CreateAssetMenu(menuName="Game/Skill Logic/W Guard Stance")]
public class WGuardStanceLogic : SkillLogic
{
    public override void OnStart(SkillRunner runner, SkillDefinition def)
    {
        var defense = runner.GetComponent<DefenseController>();
        if (defense) defense.guardActive = true;
    }

    public override void OnAnimHit(SkillRunner runner, SkillDefinition def)
    {
        // “후 공격” 시점에 방어 풀기
        var defense = runner.GetComponent<DefenseController>();
        if (defense) defense.guardActive = false;

        // 여기서 실제 공격 로직(또는 다른 로직 호출) 들어가면 됨
    }

    public override void OnAnimEnd(SkillRunner runner, SkillDefinition def)
    {
        // 안전장치: 끝나면 무조건 OFF
        var defense = runner.GetComponent<DefenseController>();
        if (defense) defense.guardActive = false;
    }
}