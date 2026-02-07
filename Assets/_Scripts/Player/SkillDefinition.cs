using UnityEngine;
using System.Collections.Generic;

public enum VfxTiming { OnStart, OnHit, OnEnd }
public enum VfxAttach { None, PlayerRoot, SwordTip, SwordBase, Custom }

[System.Serializable]
public class VfxSpawn
{
    public VfxTiming timing;
    public GameObject prefab;

    public VfxAttach attachTo = VfxAttach.None;
    public string customSocketName;

    public Vector3 localPos;
    public Vector3 localEuler;

    public float lifeTime = 1.5f;
    public bool follow = false;
}
[System.Serializable]
public class ChargeScaleConfig
{
    [Header("Charge 0~1")]
    public float minVfxScale = 1f;
    public float maxVfxScale = 2.5f;

    public float minHitScale = 1f;     // 히트박스 크기 배율
    public float maxHitScale = 2.0f;

    // 필요하면 곡선(초반 완만/후반 급증)
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    
    // ✅ 추가: 데미지 배율
    public float minDamageScale = 1f;
    public float maxDamageScale = 1f;
}

[CreateAssetMenu(menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public SkillSlot slot;

    [Header("Cooldown")]
    public float cooldown = 1f;
    
    [Header("Damage")]
    public int baseDamage = 10;   // 일반 스킬용

    [Header("Guard Bypass")]
    public GuardBypassType guardBypass = GuardBypassType.None;

    [Range(0f, 1f)]
    public float guardBypassFactor = 0.5f; // Partial일 때만 사용
    
    [Header("Charge Damage (Charge Skill Only)")]
    public bool useChargeDamage = false;
    public int minChargeDamage = 10;
    public int maxChargeDamage = 30;
    
    [Header("Animation")]
    [Tooltip("공격/발동 애니 Trigger 이름. 예: Skill_Q, Skill_R_Attack ...")]
    public string animatorTrigger;

    [Header("Behaviour")]
    public bool lockMovement = true;

    [Header("Logic")]
    public SkillLogic logic;

    [Header("Facing (Pre-rotate before cast)")]
    public bool requireFacing = true;
    public float turnSpeedDegPerSec = 720f;
    public float facingToleranceDeg = 6f;
    public bool lockMovementWhileTurning = true;

    [Header("Weapon Pose Override (optional)")]
    public bool overrideWeaponPose = false;
    public Vector3 weaponLocalPosOffset;
    public Vector3 weaponLocalEulerOffset;
    public VfxTiming weaponPoseApplyTiming = VfxTiming.OnStart;

    [Header("Move While Cast")]
    public CastMoveConfig castMove;

    // ✅ 차징 관련 옵션 추가
    [Header("Charge (Optional)")]
    public bool isChargeSkill = false;

    [Tooltip("차징 시작 모션(대기/차징) Trigger. 예: Skill_R_Charge")]
    public string chargeStartTrigger;

    [Tooltip("최대 차징 시간(초). 예: 3")]
    public float maxChargeTime = 3f;

    [Tooltip("최대 시간 도달 시 자동으로 Release(공격 발동)할지")]
    public bool autoReleaseOnMax = true;

    [Header("Charge Scaling")]
    public ChargeScaleConfig chargeScale;
    
    [Header("VFX")]
    public List<VfxSpawn> vfx = new List<VfxSpawn>();
}