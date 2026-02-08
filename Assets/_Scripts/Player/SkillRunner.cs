using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SkillRunner : MonoBehaviour
{
    private enum State
    {
        Idle,
        Turning,
        Charging,
        Casting
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Skills (assign ScriptableObjects)")]
    [SerializeField] private List<SkillDefinition> skills;

    private readonly Dictionary<SkillSlot, SkillDefinition> map = new();
    private readonly Dictionary<SkillSlot, float> cdEnd = new();

    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundMask;

    [Header("VFX sockets")]
    public Transform swordTip;
    public Transform swordBase;

    public WeaponEquipper weaponEquipper;

    // ===== Aim point from input =====
    private bool hasAimPoint;
    private Vector3 aimPoint;

    // ===== Runtime state =====
    private State state = State.Idle;

    private SkillDefinition current;         // Charging/Casting 중인 스킬
    private SkillSlot currentSlot;

    private SkillDefinition pending;         // Turning 중 대기 스킬
    private SkillSlot pendingSlot;
    private Vector3 pendingAimPoint;
    private bool pendingReleaseOnStart;      // Turning 중에 Up이 들어오면 true

    // ===== Charge =====
    private float chargeStartTime;
    private Vector3 chargeAimPoint;
    private float charge01;                  // 0~1 (차지 중)
    private float releasedCharge01;          // Release 시점에 고정된 값

    public bool IsCasting => state == State.Casting;
    public bool IsCharging => state == State.Charging;
    public bool IsBusy => state != State.Idle;   // Turning/Charging/Casting 전부 Busy

    public float CurrentCharge01 => (state == State.Charging) ? charge01 : releasedCharge01;
    public float CurrentHitScale => EvaluateHitScale(current, CurrentCharge01);
    public float CurrentVfxScale => EvaluateVfxScale(current, CurrentCharge01);
    public float CurrentDamageScale => EvaluateDamageScale(current, CurrentCharge01);

    // ===== Cast Move =====
    private Coroutine castMoveCo;
    private bool castMoveLock;
    private bool castMoveStartedThisCast;
    public bool CastMoveLock => castMoveLock;

    // ===== VFX pooling =====
    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, Transform> socketCache = new();
    private readonly Dictionary<GameObject, Vector3> prefabBaseScale = new();

    //Debug
    public SkillDefinition Debug_CurrentSkill => current;
    public bool Debug_HasCurrentSkill => current != null;
    
    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!cam) cam = Camera.main;

        foreach (var s in skills)
        {
            if (!s) continue;
            map[s.slot] = s;
            cdEnd[s.slot] = 0f;
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.Turning:
                TickTurning();
                return;

            case State.Charging:
                TickCharging();
                return;

            case State.Casting:
                if (current && current.logic)
                    current.logic.OnTick(this, current, Time.deltaTime);
                return;

            default:
                return;
        }
    }

    // =========================================================
    // Input API
    // =========================================================
    public void SetAimPoint(bool has, Vector3 worldPoint)
    {
        hasAimPoint = has;
        aimPoint = worldPoint;
    }

    public Vector3 GetMouseGroundPoint()
    {
        if (hasAimPoint) return aimPoint;
        return transform.position + transform.forward * 2f;
    }

    public bool TryPress(SkillSlot slot)
    {
        // 상태가 Idle이 아닐 때는 새 스킬 시작 금지 (단순하게 유지)
        if (state != State.Idle) return false;

        if (!map.TryGetValue(slot, out var def) || def == null || def.logic == null)
            return false;

        // 쿨타임 체크: 차지든 일반이든 "시작 자체"를 막음
        if (Time.time < cdEnd[slot]) return false;
        
        if (slot == SkillSlot.R)
        {
            var ult = GetComponentInParent<UltGauge>();
            if (ult == null) ult = GetComponent<UltGauge>();

            if (ult == null || !ult.IsFull)
                return false; // 100% 아니면 R 못 씀
            
        }
        
        pendingAimPoint = GetMouseGroundPoint();

        // 선회 필요하면 Turning으로 들어감
        if (def.requireFacing)
        {
            float angle = YawAngleTo(pendingAimPoint);
            if (angle > def.facingToleranceDeg)
            {
                BeginTurning(slot, def, pendingAimPoint);
                return true;
            }
        }

        // 바로 실행
        if (def.isChargeSkill) BeginCharge(slot, def, pendingAimPoint, releaseImmediately: false);
        else BeginCast(slot, def);

        return true;
    }

    public bool TryRelease(SkillSlot slot)
    {
        // 차지 중이면 즉시 릴리즈
        if (state == State.Charging && current != null && current.isChargeSkill && currentSlot == slot)
        {
            EndChargeAndFire();
            return true;
        }

        // Turning 중(아직 차지 시작 전) Up이 들어오면 "차지 시작하자마자 즉시 릴리즈" 예약
        if (state == State.Turning && pending != null && pending.isChargeSkill && pendingSlot == slot)
        {
            pendingReleaseOnStart = true;
            return true;
        }

        return false;
    }

    // =========================================================
    // Turning
    // =========================================================
    private void BeginTurning(SkillSlot slot, SkillDefinition def, Vector3 aim)
    {
        pendingSlot = slot;
        pending = def;
        pendingAimPoint = aim;
        pendingReleaseOnStart = false;

        state = State.Turning;

        if (def.lockMovementWhileTurning)
            StopAgentImmediate();
    }

    private void TickTurning()
    {
        if (pending == null)
        {
            state = State.Idle;
            return;
        }

        FaceTo(pendingAimPoint, pending.turnSpeedDegPerSec);

        float angle = YawAngleTo(pendingAimPoint);
        if (angle > pending.facingToleranceDeg) return;

        // 선회 완료 → 실제 시작
        var slot = pendingSlot;
        var def = pending;
        var aim = pendingAimPoint;
        bool releaseNow = pendingReleaseOnStart;

        pending = null;
        pendingReleaseOnStart = false;

        if (def.isChargeSkill) BeginCharge(slot, def, aim, releaseImmediately: releaseNow);
        else BeginCast(slot, def);
    }

    // =========================================================
    // Charge
    // =========================================================
    private void BeginCharge(SkillSlot slot, SkillDefinition def, Vector3 aim, bool releaseImmediately)
    {
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;

        current = def;
        currentSlot = slot;

        state = State.Charging;

        chargeAimPoint = aim;
        chargeStartTime = Time.time;
        charge01 = 0f;
        releasedCharge01 = 0f;

        if (def.lockMovement)
            StopAgentImmediate();

        var defense = GetComponent<DefenseController>();
        if (defense && def.slot == SkillSlot.R)
            defense.externalKnockbackImmune = true;
        
        // 차지 "시작" 애니
        if (!string.IsNullOrEmpty(def.chargeStartTrigger))
        {
            animator.ResetTrigger(def.chargeStartTrigger);
            animator.SetTrigger(def.chargeStartTrigger);
        }

        // 차지 시작 시 무기 포즈 적용(설정에 따라)
        if (weaponEquipper && def.overrideWeaponPose && def.weaponPoseApplyTiming == VfxTiming.OnStart)
            weaponEquipper.ApplyWeaponOffset(def.weaponLocalPosOffset, def.weaponLocalEulerOffset);

        // Down/Up 같은 프레임이면 즉시 발동(차지값 0으로)
        if (releaseImmediately)
        {
            EndChargeAndFire();
        }
    }

    private void TickCharging()
    {
        if (current == null)
        {
            state = State.Idle;
            return;
        }

        float maxT = Mathf.Max(0.01f, current.maxChargeTime);
        float raw = Mathf.Clamp01((Time.time - chargeStartTime) / maxT);

        if (current.chargeScale != null && current.chargeScale.curve != null)
            charge01 = Mathf.Clamp01(current.chargeScale.curve.Evaluate(raw));
        else
            charge01 = raw;

        if (current.requireFacing)
            FaceTo(chargeAimPoint, current.turnSpeedDegPerSec);

        if (current.autoReleaseOnMax && raw >= 1f)
            EndChargeAndFire();
    }

    private void EndChargeAndFire()
    {
        if (current == null) { state = State.Idle; return; }

        var def = current;
        var slot = currentSlot;
        
        if (slot == SkillSlot.R)
        {
            var ult = GetComponentInParent<UltGauge>();
            if (ult == null) ult = GetComponent<UltGauge>();
            if (ult == null || !ult.TryConsumeFull())
            {
                ResetToIdle(def);
                return;
            }
        }
        
        var defense = GetComponent<DefenseController>();
        if (defense && def.slot == SkillSlot.R)
            defense.externalKnockbackImmune = false;
        
        // 차지 시작 트리거는 끊어줌
        if (!string.IsNullOrEmpty(def.chargeStartTrigger))
            animator.ResetTrigger(def.chargeStartTrigger);

        // Release 시점 차지값 고정
        releasedCharge01 = charge01;

        // 쿨타임(혹시나) 방어: 발동만 막고 상태는 정리
        if (Time.time < cdEnd[slot])
        {
            ResetToIdle(def);
            return;
        }

        cdEnd[slot] = Time.time + def.cooldown;

        // 발동 로직 시작
        state = State.Casting;
        def.logic.OnStart(this, def);

        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }
        else
        {
            // 발동 애니 트리거가 없으면 End 이벤트도 안 올 수 있으니 즉시 정리
            ResetToIdle(def);
        }
    }

    // =========================================================
    // Normal cast
    // =========================================================
    private void BeginCast(SkillSlot slot, SkillDefinition def)
    {
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;

        current = def;
        currentSlot = slot;
        state = State.Casting;

        releasedCharge01 = 0f;
        charge01 = 0f;

        cdEnd[slot] = Time.time + def.cooldown;

        bool shouldStopAgent =
            def.lockMovement ||
            (def.castMove != null && def.castMove.blockNormalMove);

        if (shouldStopAgent)
            StopAgentImmediate();

        if (slot == SkillSlot.R)
        {
            var ult = GetComponentInParent<UltGauge>();
            if (ult == null) ult = GetComponent<UltGauge>();
            if (ult == null || !ult.TryConsumeFull())
                return; // 가득 아니면 발동 취소
        }
        
        def.logic.OnStart(this, def);

        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }

        if (weaponEquipper && def.overrideWeaponPose)
            weaponEquipper.ApplyWeaponOffset(def.weaponLocalPosOffset, def.weaponLocalEulerOffset);
    }

    // =========================================================
    // Anim Events (Animation Event에서 호출)
    // =========================================================
    public void AnimEvent_Start()
    {
        if (state != State.Casting) return;
        if (current?.logic == null) return;

        current.logic.OnAnimStart(this, current);
        
        PlayVfx(current, VfxTiming.OnStart);
    }

    public void AnimEvent_Move()
    {
        if (state != State.Casting) return;
        TryStartCastMove(current);
    }
    public void AnimEvent_GuardStart()
    {
        if (state != State.Casting) return;
        if (current?.logic == null) return;

        current.logic.OnCustomEvent(this, current, "GuardStart");
    }
    public void AnimEvent_GuardEnd()
    {
        if (state != State.Casting) return;
        if (current?.logic == null) return;

        // W 전용 로직이 이 이벤트를 받도록(캐스팅 타입 체크는 로직에서)
        current.logic.OnCustomEvent(this, current, "GuardEnd");
    }

    public void AnimEvent_Hit()
    {
        if (state != State.Casting) return;
        if (current?.logic == null) return;

        current.logic.OnAnimHit(this, current);
        PlayVfx(current, VfxTiming.OnHit);
    }

    public void AnimEvent_End()
    {
        if (state != State.Casting) return;
        if (current?.logic == null) return;

        current.logic.OnAnimEnd(this, current);
        PlayVfx(current, VfxTiming.OnEnd);

        ResetToIdle(current);
    }

    public void AnimEvent_Sfx()
    {    
        // 캐스팅 중이 아닐 때 호출되면 무시
        if (state != State.Casting) return;

        // current가 없으면 무시
        if (current == null) return;

        // AudioManager가 없으면 무시(혹은 로그)
        if (AudioManager3D.I == null) return;
        AudioManager3D.I.PlaySkillSfx(current, transform.position);
    }
    

    // =========================================================
    // Reset / Movement
    // =========================================================
    private void ResetToIdle(SkillDefinition def)
    {
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;
        
        var defense = GetComponent<DefenseController>();
        if (defense) defense.externalKnockbackImmune = false;

        if (agent)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (weaponEquipper && def != null && def.overrideWeaponPose)
            weaponEquipper.ResetWeaponOffset();

        current = null;
        releasedCharge01 = 0f;
        charge01 = 0f;

        state = State.Idle;
    }

    private void StopAgentImmediate()
    {
        if (!agent) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // =========================================================
    // Cast Move
    // =========================================================
    private void TryStartCastMove(SkillDefinition def)
    {
        if (def == null) return;
        if (def.castMove == null) return;
        if (castMoveStartedThisCast) return;

        castMoveStartedThisCast = true;
        castMoveCo = StartCoroutine(Co_CastMove(def.castMove));
    }

    private void StopCastMoveIfRunning()
    {
        if (castMoveCo != null)
        {
            StopCoroutine(castMoveCo);
            castMoveCo = null;
        }
        castMoveLock = false;
    }

    private Vector3 ResolveMoveDir(CastMoveConfig cfg)
    {
        switch (cfg.direction)
        {
            case SkillMoveDirection.Input:
            case SkillMoveDirection.TowardTarget:
            {
                Vector3 p = GetMouseGroundPoint();
                Vector3 dir = p - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return transform.forward;
                return dir.normalized;
            }
            case SkillMoveDirection.Forward:
            default:
                return transform.forward;
        }
    }
    public int GetCurrentDamage(SkillDefinition def)
    {
        if (def == null) return 0;

        // 차지 데미지 사용 안 하면 고정 데미지
        if (!def.useChargeDamage || !def.isChargeSkill)
            return def.baseDamage;

        float c01 = Mathf.Clamp01(CurrentCharge01);
        return Mathf.RoundToInt(
            Mathf.Lerp(def.minChargeDamage, def.maxChargeDamage, c01)
        );
    }
    
    private static float EvaluateDamageScale(SkillDefinition def, float c01)
    {
        if (def == null || def.chargeScale == null) return 1f;
        return Mathf.Lerp(def.chargeScale.minDamageScale, def.chargeScale.maxDamageScale, Mathf.Clamp01(c01));
    }
    
    private IEnumerator Co_CastMove(CastMoveConfig cfg)
    {
        if (cfg == null || !agent) yield break;

        float duration = Mathf.Max(0.01f, cfg.duration);
        float targetDist = Mathf.Max(0f, cfg.distance);

        castMoveLock = cfg.blockNormalMove;

        Vector3 fixedDir = ResolveMoveDir(cfg);

        float t = 0f;
        float moved = 0f;

        while (t < duration && moved < targetDist && state == State.Casting && current != null)
        {
            float dt = Time.deltaTime;
            t += dt;

            float p = Mathf.Clamp01(t / duration);
            float w = cfg.speedCurve != null ? Mathf.Max(0f, cfg.speedCurve.Evaluate(p)) : 1f;

            float step = (targetDist / duration) * w * dt;
            step = Mathf.Min(step, targetDist - moved);

            Vector3 dir = cfg.allowSteer ? ResolveMoveDir(cfg) : fixedDir;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

            if (cfg.stopOnHit)
            {
                Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.5f, agent.height * 0.5f);
                float radius = Mathf.Max(0.1f, agent.radius * 0.9f);
                if (Physics.SphereCast(origin, radius, dir, out _, step + 0.05f))
                    break;
            }

            agent.Move(dir * step);
            moved += step;

            yield return null;
        }

        castMoveLock = false;
        castMoveCo = null;
    }

    // =========================================================
    // Facing / Utils
    // =========================================================
    public void FaceTo(Vector3 worldPoint, float maxDegPerSec = 99999f)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxDegPerSec * Time.deltaTime);
    }

    private float YawAngleTo(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return 0f;
        return Vector3.Angle(transform.forward, dir.normalized);
    }

    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;

    // =========================================================
    // Charge scale helpers
    // =========================================================
    private static float EvaluateVfxScale(SkillDefinition def, float c01)
    {
        if (def == null || def.chargeScale == null) return 1f;
        return Mathf.Lerp(def.chargeScale.minVfxScale, def.chargeScale.maxVfxScale, Mathf.Clamp01(c01));
    }

    private static float EvaluateHitScale(SkillDefinition def, float c01)
    {
        if (def == null || def.chargeScale == null) return 1f;
        return Mathf.Lerp(def.chargeScale.minHitScale, def.chargeScale.maxHitScale, Mathf.Clamp01(c01));
    }

    // =========================================================
    // VFX pooling + Spawn 후 scale 적용(누적 방지)
    // =========================================================
    private void PlayVfx(SkillDefinition def, VfxTiming timing)
    {
        if (def == null || def.vfx == null) return;

        float vfxScaleMul = EvaluateVfxScale(def, CurrentCharge01);

        for (int i = 0; i < def.vfx.Count; i++)
        {
            var v = def.vfx[i];
            if (v == null || v.prefab == null) continue;
            if (v.timing != timing) continue;

            Transform socket = ResolveSocket(v.attachTo, v.customSocketName);

            Vector3 pos;
            Quaternion rot;

            if (socket != null)
            {
                rot = socket.rotation * Quaternion.Euler(v.localEuler);
                pos = socket.TransformPoint(v.localPos);
            }
            else
            {
                rot = transform.rotation * Quaternion.Euler(v.localEuler);
                pos = transform.TransformPoint(v.localPos);
            }

            GameObject inst = SpawnFromPool(v.prefab, pos, rot);

            if (v.follow && socket != null)
            {
                inst.transform.SetParent(socket, worldPositionStays: false);
                inst.transform.localPosition = v.localPos;
                inst.transform.localRotation = Quaternion.Euler(v.localEuler);
            }
            else
            {
                inst.transform.SetParent(null);
            }

            if (!prefabBaseScale.TryGetValue(v.prefab, out var baseScale))
            {
                baseScale = v.prefab.transform.localScale;
                prefabBaseScale[v.prefab] = baseScale;
            }

            if (def.isChargeSkill && def.chargeScale != null)
                inst.transform.localScale = baseScale * vfxScaleMul;
            else
                inst.transform.localScale = baseScale;

            float life = v.lifeTime;
            if (life <= 0f)
            {
                life = EstimateParticleLifetime(inst);
                if (life <= 0f) life = 1.5f;
            }

            StartCoroutine(ReturnAfter(v.prefab, inst, life));
        }
    }

    private Transform ResolveSocket(VfxAttach attach, string custom)
    {
        switch (attach)
        {
            case VfxAttach.PlayerRoot:
                return transform;
            case VfxAttach.SwordTip:
                return swordTip ? swordTip : transform;
            case VfxAttach.SwordBase:
                return swordBase ? swordBase : transform;
            case VfxAttach.Custom:
                if (string.IsNullOrEmpty(custom)) return transform;
                if (socketCache.TryGetValue(custom, out var cached) && cached) return cached;

                var found = transform.Find(custom);
                if (!found) found = FindDeepChild(transform, custom);

                socketCache[custom] = found ? found : transform;
                return socketCache[custom];
            case VfxAttach.None:
            default:
                return null;
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
        return null;
    }

    private GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }

        GameObject go;
        if (q.Count > 0)
        {
            go = q.Dequeue();
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(prefab, pos, rot);
        }

        return go;
    }

    private void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (!instance) return;

        instance.transform.SetParent(null);
        instance.SetActive(false);

        if (!pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefab] = q;
        }
        q.Enqueue(instance);
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float sec)
    {
        yield return new WaitForSeconds(sec);
        ReturnToPool(prefab, instance);
    }

    private static float EstimateParticleLifetime(GameObject inst)
    {
        var ps = inst.GetComponentInChildren<ParticleSystem>();
        if (!ps) return 0f;

        var main = ps.main;
        float duration = main.duration;
        float startLifetime = 0f;

        switch (main.startLifetime.mode)
        {
            case ParticleSystemCurveMode.Constant:
                startLifetime = main.startLifetime.constant;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                startLifetime = main.startLifetime.constantMax;
                break;
            default:
                startLifetime = 0.5f;
                break;
        }

        return Mathf.Max(0.1f, duration + startLifetime);
    }
    
    public float GetCooldownRemaining(SkillSlot slot)
    {
        if (!cdEnd.TryGetValue(slot, out float end)) return 0f;
        return Mathf.Max(0f, end - Time.time);
    }

    public float GetCooldownDuration(SkillSlot slot)
    {
        if (!map.TryGetValue(slot, out var def) || def == null) return 1f;
        return Mathf.Max(0.01f, def.cooldown);
    }
}