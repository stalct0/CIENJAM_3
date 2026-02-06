using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public SkillSlot slot;

    [Header("Cooldown")]
    public float cooldown = 1f;

    [Header("Animation")]
    [Tooltip("Animator Trigger 이름. 예: Attack_LMB, Skill_Q, Skill_E ...")]
    public string animatorTrigger;

    [Header("Behaviour")]
    public bool lockMovement = true;

    [Header("Logic")]
    [Tooltip("이 스킬이 실제로 무엇을 하는지(로직). ScriptableObject로 연결")]
    public SkillLogic logic;
}