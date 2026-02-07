using UnityEngine;

public struct DamageInfo
{
    // 원본 레퍼런스(있으면 편함). 멀티에서는 이것만 믿지 말고 아래 식별자도 같이 사용.
    public GameObject attacker;

    // ✅ 멀티 규칙용 식별자
    public ulong attackerOwnerId;
    public uint attackerEntityId;
    public TeamId attackerTeam;

    // 피해 정보
    public int amount;
    public Vector3 hitPoint;
    public Vector3 hitDir;

    // 스킬/공격원 (선택)
    public SkillDefinition skill;
}