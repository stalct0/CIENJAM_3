using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class SkillRunner : MonoBehaviour
{
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

    private bool hasAimPoint;
    private Vector3 aimPoint;

    private SkillDefinition current;
    private SkillDefinition pending;
    private SkillSlot pendingSlot;
    private bool isTurning;
    private Vector3 pendingAimPoint;

    public WeaponEquipper weaponEquipper;

    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, Transform> socketCache = new();

    public bool IsCasting => current != null;

    // ===== Cast-Move Runtime State =====
    private Coroutine castMoveCo;
    private bool castMoveLock;
    private bool castMoveStartedThisCast;
    public bool CastMoveLock => castMoveLock;

    // ===== Charge Runtime State =====
    private bool isCharging;
    private float chargeStartTime;
    private SkillSlot chargingSlot;     // 어떤 슬롯을 차징 중인지
    private Vector3 chargeAimPoint;     // 차징 시작 시 조준점 고정(원하면 고정)

    public bool IsCharging => isCharging;
    public bool IsBusy => IsCasting || isTurning || isCharging; // 하나로 묶기
    
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
        // 선회 처리(캐스트 시작 전에 정면 맞추기)
        if (isTurning && pending != null)
        {
            FaceTo(pendingAimPoint, pending.turnSpeedDegPerSec);

            float angle = YawAngleTo(pendingAimPoint);
            if (angle <= pending.facingToleranceDeg)
            {
                var slot = pendingSlot;
                var def = pending;

                isTurning = false;
                pending = null;

                // 선회 완료 후: 차징 스킬이면 차징 시작, 아니면 즉시 시전
                if (def.isChargeSkill)
                    StartChargeNow(slot, def);
                else
                    StartCastNow(slot, def);
            }
            return;
        }

        // ✅ 차징 중이면 시간 체크
        if (isCharging && current != null && current.isChargeSkill)
        {
            float elapsed = Time.time - chargeStartTime;

            // 차징 중에도 계속 조준점으로 회전하고 싶으면 켜세요(원치 않으면 주석)
            if (current.requireFacing)
                FaceTo(chargeAimPoint, current.turnSpeedDegPerSec);

            if (current.autoReleaseOnMax && elapsed >= Mathf.Max(0.01f, current.maxChargeTime))
            {
                ReleaseCharge(); // 최대 시간 자동 발동
                return;
            }

            // 차징 중에는 기존 로직 Tick은 원하면 호출/아니면 끄기
            // (원래 로직이 “시전 중”을 전제하면 오작동할 수 있으니 기본은 호출 안 하는 걸 추천)
            return;
        }

        // 캐스팅 중 로직 Tick (차징 중에는 위에서 return 처리)
        if (current && current.logic)
            current.logic.OnTick(this, current, Time.deltaTime);
    }

    public void SetAimPoint(bool has, Vector3 worldPoint)
    {
        hasAimPoint = has;
        aimPoint = worldPoint;
    }

    // =========================================================
    // 입력용 API (중요)
    // =========================================================

    // ✅ 키 다운: 일반 스킬은 즉시 시전, 차징 스킬은 차징 시작
    public bool TryPress(SkillSlot slot)
    {
        if (IsCasting || isTurning) return false;

        if (!map.TryGetValue(slot, out var def)) return false;
        if (!def.logic) return false;

        // 차징 시작은 쿨타임을 "발동 시점"에 체크하는 게 일반적이라서,
        // 여기서는 쿨타임 체크를 "Release"에서 합니다.
        // 다만 눌렀는데 쿨이면 차징 모션조차 안 되게 하고 싶으면 아래 체크를 활성화하세요.
        // if (Time.time < cdEnd[slot]) return false;

        pendingAimPoint = GetMouseGroundPoint();

        if (def.requireFacing)
        {
            float angle = YawAngleTo(pendingAimPoint);
            if (angle > def.facingToleranceDeg)
            {
                StartTurning(slot, def);
                return true;
            }
        }

        if (def.isChargeSkill)
        {
            StartChargeNow(slot, def);
            return true;
        }

        // 일반 스킬
        if (Time.time < cdEnd[slot]) return false;
        StartCastNow(slot, def);
        return true;
    }

    // ✅ 키 업: 차징 스킬이면 공격 발동
    public bool TryRelease(SkillSlot slot)
    {
        if (!isCharging) return false;
        if (current == null) return false;
        if (!current.isChargeSkill) return false;
        if (chargingSlot != slot) return false;

        ReleaseCharge();
        return true;
    }

    // 기존 코드 호환용 (기존에 TryCast만 쓰던 부분이 있으면 유지)
    public bool TryCast(SkillSlot slot) => TryPress(slot);

    // =========================================================

    private void StartTurning(SkillSlot slot, SkillDefinition def)
    {
        pendingSlot = slot;
        pending = def;
        isTurning = true;

        if (def.lockMovementWhileTurning)
            CancelMove();
    }

    // =========================================================
    // 차징 시작
    // =========================================================
    private void StartChargeNow(SkillSlot slot, SkillDefinition def)
    {
        CancelMove();

        // 새 캐스트 시작 시 이동 상태 초기화
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;

        // 차징 상태 세팅
        current = def;
        isCharging = true;
        chargingSlot = slot;
        chargeStartTime = Time.time;

        // 차징 시작 시점 조준점 고정(원하면 고정 안 하고 매 프레임 GetMouseGroundPoint() 쓰게 바꿔도 됨)
        chargeAimPoint = pendingAimPoint;

        // 이동 잠금(차징 대기중 못 움직이게 하고 싶으면 def.lockMovement 사용)
        if (def.lockMovement && agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 로직 훅: 차징 시작에 로직을 타게 하고 싶으면 여기서 호출
        // 현재 SkillLogic에 전용 훅이 없으니, 필요하면 SkillLogic에 OnChargeStart/OnChargeRelease 추가하는 걸 추천.
        // def.logic.OnStart(this, def);

        // 차징 애니 트리거
        if (!string.IsNullOrEmpty(def.chargeStartTrigger))
        {
            animator.ResetTrigger(def.chargeStartTrigger);
            animator.SetTrigger(def.chargeStartTrigger);
        }

        // 무기 포즈 오버라이드가 “차징 시작”에도 적용되게 하고 싶으면
        if (weaponEquipper && def.overrideWeaponPose && def.weaponPoseApplyTiming == VfxTiming.OnStart)
            weaponEquipper.ApplyWeaponOffset(def.weaponLocalPosOffset, def.weaponLocalEulerOffset);

        // VFX를 차징 시작에 뿌리고 싶으면(현재는 OnStart 이벤트에서만 뿌리게 돼 있으니 여기서 뿌릴 수도 있음)
        // PlayVfx(def, VfxTiming.OnStart);
    }

    // 차징 해제(공격 발동)
    private void ReleaseCharge()
    {
        if (current == null) return;

        var def = current;
        var slot = chargingSlot;

        // 차징 종료는 무조건
        isCharging = false;

        // 쿨이면 "발동은 안 하지만" 상태는 풀어야 함
        if (Time.time < cdEnd[slot])
        {
            ForceEndChargeState(def); // ✅ 추가
            return;
        }

        cdEnd[slot] = Time.time + def.cooldown;

        def.logic.OnStart(this, def);

        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }
    }
    private void ForceEndChargeState(SkillDefinition def)
    { 
        if (agent)
        {
            agent.isStopped = false;
            agent.ResetPath();       // ✅ 추가: 남은 경로 제거
            agent.velocity = Vector3.zero; // ✅ 추가: 잔속도 제거
        }

        if (weaponEquipper && def != null && def.overrideWeaponPose)
            weaponEquipper.ResetWeaponOffset();

        current = null;
        isCharging = false;          // ✅ 안전하게 다시 한 번
    }

    // =========================================================
    // 일반 스킬 시작
    // =========================================================
    private void StartCastNow(SkillSlot slot, SkillDefinition def)
    {
        CancelMove();

        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;

        cdEnd[slot] = Time.time + def.cooldown;

        bool shouldStopAgent =
            def.lockMovement ||
            (def.castMove != null && def.castMove.blockNormalMove);

        if (shouldStopAgent && agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        current = def;

        def.logic.OnStart(this, def);

        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }

        if (weaponEquipper && current.overrideWeaponPose)
            weaponEquipper.ApplyWeaponOffset(current.weaponLocalPosOffset, current.weaponLocalEulerOffset);
    }

    private void CancelMove()
    {
        if (!agent) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    // =========================================================
    // Anim Events
    // =========================================================
    public void AnimEvent_Start()
    {
        if (current?.logic == null) return;

        // 차징 모션에서도 이벤트가 날아올 수 있는데,
        // 차징 중에는 공격 로직을 타면 안 되므로 막습니다.
        if (isCharging && current.isChargeSkill) return;

        current.logic.OnAnimStart(this, current);
        PlayVfx(current, VfxTiming.OnStart);
    }

    public void AnimEvent_Move()
    {
        // 차징 중에는 이동 시작시키면 안 됨(원하면 차징 이동도 만들 수 있음)
        if (isCharging && current != null && current.isChargeSkill) return;
        TryStartCastMove(current);
    }

    public void AnimEvent_Hit()
    {
        if (current?.logic == null) return;
        if (isCharging && current.isChargeSkill) return;

        current.logic.OnAnimHit(this, current);
        PlayVfx(current, VfxTiming.OnHit);
    }

    public void AnimEvent_End()
    {
        if (current?.logic == null) return;

        // 차징 모션의 End 이벤트가 날아오면(루프가 아니거나) 상태만 유지하면 되는데,
        // 여기선 차징 모션은 End 이벤트를 안 쓰는 걸 전제로 합니다.
        // 만약 차징 클립에 End 이벤트가 있다면 "차징 종료"로 오작동할 수 있으니 제거하세요.
        if (isCharging && current.isChargeSkill) return;

        current.logic.OnAnimEnd(this, current);
        PlayVfx(current, VfxTiming.OnEnd);

        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;

        if (agent)
            agent.isStopped = false;

        if (weaponEquipper && current.overrideWeaponPose)
            weaponEquipper.ResetWeaponOffset();

        current = null;
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

    private IEnumerator Co_CastMove(CastMoveConfig cfg)
    {
        if (cfg == null || !agent) yield break;

        float duration = Mathf.Max(0.01f, cfg.duration);
        float targetDist = Mathf.Max(0f, cfg.distance);

        castMoveLock = cfg.blockNormalMove;

        Vector3 fixedDir = ResolveMoveDir(cfg);

        float t = 0f;
        float moved = 0f;

        while (t < duration && moved < targetDist && current != null)
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
                if (Physics.SphereCast(origin, radius, dir, out var hit, step + 0.05f))
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
    // Utils
    // =========================================================
    public Vector3 GetMouseGroundPoint()
    {
        if (hasAimPoint) return aimPoint;
        return transform.position + transform.forward * 2f;
    }

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

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle;
    }

    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;

    // =========================================================
    // VFX pooling (원본 그대로)
    // =========================================================
    private void PlayVfx(SkillDefinition def, VfxTiming timing)
    {
        if (def == null || def.vfx == null) return;

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
}