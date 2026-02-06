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
    
    
    // === Simple VFX pooling ===
    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, Transform> socketCache = new();
    public bool IsCasting => current != null;
    
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
            // 선회 중에는 고정된 aimPoint로 회전
            FaceTo(pendingAimPoint, pending.turnSpeedDegPerSec);

            float angle = YawAngleTo(pendingAimPoint);
            if (angle <= pending.facingToleranceDeg)
            {
                // 선회 완료 -> 실제 캐스트 시작
                var slot = pendingSlot;
                var def = pending;

                isTurning = false;
                pending = null;

                StartCastNow(slot, def);
            }
            return; 
        }

        // 캐스팅 중 로직 Tick
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
            // 이미 거의 정면이면 바로 캐스트
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
            CancelMove(); // agent.ResetPath 포함
    }

    private void StartCastNow(SkillSlot slot, SkillDefinition def)
    {
        CancelMove();

        cdEnd[slot] = Time.time + def.cooldown;

        if (def.lockMovement && agent)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        current = def;
        
        def.logic.OnStart(this, def);

        // 애니 트리거
        if (!string.IsNullOrEmpty(def.animatorTrigger))
        {
            animator.ResetTrigger(def.animatorTrigger);
            animator.SetTrigger(def.animatorTrigger);
        }
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
        current.logic.OnAnimStart(this, current);   // 새 훅
        PlayVfx(current, VfxTiming.OnStart);
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
        // 이동 잠금 해제
        if (current.lockMovement && agent)
            agent.isStopped = false;

        current = null;
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
                // Auto lifetime from ParticleSystem if possible (fallback)
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

                // Allow "Child/GrandChild" path
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
        // Name search (slower) - used only if custom path fails
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

        // Approx lifetime: duration + max start lifetime
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