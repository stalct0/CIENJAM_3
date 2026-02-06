using UnityEngine;

public abstract class SkillLogic : ScriptableObject
{
    public abstract void StartCast(SkillRunner runner, SkillDefinition def);
    public virtual void Tick(SkillRunner runner, SkillDefinition def, float dt) { }
    public virtual void OnAnimHit(SkillRunner runner, SkillDefinition def) { }
    public virtual void EndCast(SkillRunner runner, SkillDefinition def) { }
}