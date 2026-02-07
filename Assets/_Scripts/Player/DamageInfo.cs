using UnityEngine;

public enum GuardBypassType
{
    None,           // 일반 공격: 가드에 완전히 막힘(전방이면)
    FullBypass,     // 가드 무시(완전 관통)
    PartialBypass   // 가드 부분 관통(예: 0.5배만 들어감)
}

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
    
    // ✅ 추가
    public GuardBypassType guardBypass;
    public float guardBypassFactor;   // PartialBypass일 때 (예: 0.5f)
    
    public bool hasKnockback;
    public float knockbackDistance;
    public float knockbackDuration;
    public bool lockInputDuringKnockback;
}