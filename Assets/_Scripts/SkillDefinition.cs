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
    public string customSocketName; // attachTo=Custom일 때

    public Vector3 localPos;
    public Vector3 localEuler;

    public float lifeTime = 1.5f; // 0이면 “프리팹 자체 파티클 duration대로”
    public bool follow = false;   // Attach형일 때 true면 부모로 붙임
}

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
    
    [Header("Facing (Pre-rotate before cast)")]
    public bool requireFacing = true;          
    public float turnSpeedDegPerSec = 720f;     // 초당 회전각
    public float facingToleranceDeg = 6f;       // 이 각도 안으로 들어오면 캐스팅 시작
    public bool lockMovementWhileTurning = true; // 선회 중 이동/agent 정지
    
    [Header("Weapon Pose Override (optional)")]
    public bool overrideWeaponPose = false;
    public Vector3 weaponLocalPosOffset;
    public Vector3 weaponLocalEulerOffset;
    public VfxTiming weaponPoseApplyTiming = VfxTiming.OnStart; // OnStart(혹은 AnimStart)에서 적용
    
    [Header("Move While Cast")]
    public CastMoveConfig castMove;
    
    [Header("VFX")]
    public List<VfxSpawn> vfx = new List<VfxSpawn>();
}