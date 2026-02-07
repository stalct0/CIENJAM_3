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
    [SerializeField] private LayerMask groundMask; // 바닥 레이어로 지정

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

    // === Simple VFX pooling ===
    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, Transform> socketCache = new();

    public bool IsCasting => current != null;

    // ===== Cast-Move Runtime State =====
    private Coroutine castMoveCo;
    private bool castMoveLock;          // "시전 이동/잠금" 중인지
    private bool castMoveStartedThisCast;

    public bool CastMoveLock => castMoveLock;

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

                StartCastNow(slot, def);
            }
            return;
        }

        if (current && current.logic)
            current.logic.OnTick(this, current, Time.deltaTime);
    }

    public void SetAimPoint(bool has, Vector3 worldPoint)
    {
        hasAimPoint = has;
        aimPoint = worldPoint;
    }

    public bool TryCast(SkillSlot slot)
    {
        if (IsCasting || isTurning) return false;
        if (!map.TryGetValue(slot, out var def)) return false;
        if (!def.logic) return false;
        if (Time.time < cdEnd[slot]) return false;

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

        StartCastNow(slot, def);
        return true;
    }

    private void StartTurning(SkillSlot slot, SkillDefinition def)
    {
        pendingSlot = slot;
        pending = def;
        isTurning = true;

        if (def.lockMovementWhileTurning)
            CancelMove();
    }

    private void StartCastNow(SkillSlot slot, SkillDefinition def)
    {
        CancelMove();

        // 새 캐스트 시작 시 이동 상태 초기화
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;
        castMoveLock = false;

        cdEnd[slot] = Time.time + def.cooldown;

        // 기본적으로 lockMovement면 경로 이동 막음
        // castMove.blockNormalMove도 똑같이 경로 이동을 막는 의미라서 같이 반영
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

    public void AnimEvent_Start()
    {
        if (current?.logic == null) return;

        current.logic.OnAnimStart(this, current);
        PlayVfx(current, VfxTiming.OnStart);
    }

    public void AnimEvent_Move()
    {
        TryStartCastMove(current);
    }

    public void AnimEvent_Hit()
    {
        if (current?.logic == null) return;

        current.logic.OnAnimHit(this, current);
        PlayVfx(current, VfxTiming.OnHit);
    }

    public void AnimEvent_End()
    {
        if (current?.logic == null) return;

        current.logic.OnAnimEnd(this, current);
        PlayVfx(current, VfxTiming.OnEnd);

        // ✅ 시전 중 이동 종료/정리
        StopCastMoveIfRunning();
        castMoveStartedThisCast = false;

        // 이동 잠금 해제
        // (lockMovement 또는 castMove.blockNormalMove로 막았던 이동을 풀어줌)
        if (agent)
            agent.isStopped = false;

        if (weaponEquipper && current.overrideWeaponPose)
            weaponEquipper.ResetWeaponOffset();

        current = null;
    }

    // ===== Cast Move =====

    private void TryStartCastMove(SkillDefinition def)
    {
        if (def == null) return;
        if (def.castMove == null) return;
        if (castMoveStartedThisCast) return;

        castMoveStartedThisCast = true;

        // 코루틴 시작
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
        // NOTE:
        // 지금 SkillRunner에는 "입력 월드방향"이 없어서,
        // Input/TowardTarget은 임시로 "aimPoint(마우스 지점) 방향"으로 처리합니다.
        // 나중에 PlayerInputHandler에서 MoveWorldDir을 받아오면 Input은 그걸로 바꾸세요.

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

        // 이 동안 기본 경로 이동은 막고 agent.Move로만 이동시키는 용도
        castMoveLock = cfg.blockNormalMove;

        // 방향 고정 (allowSteer=false일 때)
        Vector3 fixedDir = ResolveMoveDir(cfg);

        float t = 0f;
        float moved = 0f;

        while (t < duration && moved < targetDist && current != null)
        {
            float dt = Time.deltaTime;
            t += dt;

            float p = Mathf.Clamp01(t / duration);
            float w = cfg.speedCurve != null ? Mathf.Max(0f, cfg.speedCurve.Evaluate(p)) : 1f;

            // curve 가중치로 step 계산
            float step = (targetDist / duration) * w * dt;
            step = Mathf.Min(step, targetDist - moved);

            Vector3 dir = cfg.allowSteer ? ResolveMoveDir(cfg) : fixedDir;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

            // 충돌 시 멈춤
            if (cfg.stopOnHit)
            {
                // 대충 agent 크기 기반으로 앞을 검사
                Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.5f, agent.height * 0.5f);
                float radius = Mathf.Max(0.1f, agent.radius * 0.9f);
                if (Physics.SphereCast(origin, radius, dir, out var hit, step + 0.05f))
                    break;
            }

            // agent가 isStopped=true여도 Move는 적용됩니다(경로 추적만 멈추는 개념)
            agent.Move(dir * step);
            moved += step;

            yield return null;
        }

        castMoveLock = false;
        castMoveCo = null;
    }

    // === 유틸: 마우스 방향(바닥 히트) ===
    public Vector3 GetMouseGroundPoint()
    {
        if (hasAimPoint) return aimPoint;
        return transform.position + transform.forward * 2f;
    }

    // === 유틸: 회전 ===
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

    // ===== VFX Pooling =====

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