using UnityEngine;

public enum SkillSlot { Q, W, E, R }

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public SkillLogic logic;

    public SkillSlot slot;

    [Header("Timing")]
    public float cooldown = 2f;

    [Header("Animation")]
    public string animatorTrigger; // "Skill_Q" 같은 이름

    [Header("Behaviour")]
    public bool lockMovement = true;
}