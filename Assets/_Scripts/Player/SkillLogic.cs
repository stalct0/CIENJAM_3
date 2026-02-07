using UnityEngine;

public abstract class SkillLogic : ScriptableObject
{
    // 스킬 시작 시 1회 호출
    public abstract void OnStart(SkillRunner runner, SkillDefinition def);

    // 스킬 진행 중 매 프레임 호출(즉발형은 안 써도 됨)
    public virtual void OnTick(SkillRunner runner, SkillDefinition def, float dt) { }

    // 애니메이션 이벤트(Hit 타이밍)에서 호출
    public virtual void OnAnimHit(SkillRunner runner, SkillDefinition def) { }

    // 애니메이션 이벤트(End 타이밍)에서 호출
    public virtual void OnAnimEnd(SkillRunner runner, SkillDefinition def) { }
    public virtual void OnAnimStart(SkillRunner runner, SkillDefinition def) {}
    public virtual void OnCustomEvent(SkillRunner runner, SkillDefinition def, string evt) { }
}