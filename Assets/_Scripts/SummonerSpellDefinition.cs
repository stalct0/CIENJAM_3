using UnityEngine;

[CreateAssetMenu(menuName = "Game/Summoner Spell")]
public class SummonerSpellDefinition : ScriptableObject
{
    public SummonerSpellType type = SummonerSpellType.None;

    [Header("Cooldown")]
    public float cooldownSeconds = 45f;

    [Header("Flash")]
    public float flashRangeMultiplier = 1.2f; // E 이동 거리 * 1.2
    public float flashDuration = 0.02f;       // 연출용(실제는 즉시)

    [Header("Ghost")]
    public float ghostDuration = 6f;
    public float moveSpeedBonusPercent = 0.5f; // +50%

    [Header("Exhaust")]
    public float exhaustDuration = 3f;
    public float exhaustMoveSpeedMultiplier = 0.5f; // 50%
    public float exhaustDamageMultiplier = 0.5f;    // 50%

    [Header("Barrier")]
    public float barrierDuration = 3f;
    public int barrierAmount = 16;
}