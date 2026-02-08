using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class SummonerSpellRunner : MonoBehaviour
{
    [SerializeField] SummonerSpellDefinition flash;
    [SerializeField] SummonerSpellDefinition ghost;
    [SerializeField] SummonerSpellDefinition exhaust;
    [SerializeField] SummonerSpellDefinition barrier;

    [Header("Slots (assigned before game start; for now drag in inspector)")]
    public SummonerSpellDefinition spellD;
    public SummonerSpellDefinition spellF;

    [Header("Refs")]
    public Camera cam;
    public LayerMask groundMask;

    [Header("Lock conditions")]
    public KnockbackController knockback; // IsLocked면 사용 불가

    [Header("Targeting for Exhaust")]
    public float exhaustRange = 6f;
    public LayerMask exhaustTargetMask;

    [Header("Flash distance")]
    public float flashDistance = 5f; // E 이동거리 기준(임시)

    public readonly Dictionary<SummonerSlot, float> cdEnd = new();

    // ✅ InputHandler가 넣어주는 aim point
    private bool hasAimPoint;
    private Vector3 aimWorldPoint;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!knockback) knockback = GetComponent<KnockbackController>();

        switch (SpellHolder.spellD)
        {
            case SummonerSpellType.Flash:
                spellD = flash;
                break;
            case SummonerSpellType.Ghost:
                spellD = ghost;
                break;
            case SummonerSpellType.Exhaust:
                spellD = exhaust;
                break;
            case SummonerSpellType.Barrier:
                spellD = barrier;
                break;
            default:
                spellD = null;
                break;
        }

        switch (SpellHolder.spellF)
        {
            case SummonerSpellType.Flash:
                spellF = flash;
                break;
            case SummonerSpellType.Ghost:
                spellF = ghost;
                break;
            case SummonerSpellType.Exhaust:
                spellF = exhaust;
                break;
            case SummonerSpellType.Barrier:
                spellF = barrier;
                break;
            default:
                spellF = null;
                break;
        }

        cdEnd[SummonerSlot.D] = 0f;
        cdEnd[SummonerSlot.F] = 0f;
    }

    public void SetAimPoint(bool has, Vector3 p)
    {
        hasAimPoint = has;
        aimWorldPoint = p;
    }

    public bool TryCast(SummonerSlot slot)
    {
        if (knockback != null && knockback.IsLocked) return false;

        var def = GetDef(slot);
        if (def == null || def.type == SummonerSpellType.None) return false;

        if (Time.time < cdEnd[slot]) return false;

        bool ok = def.type switch
        {
            SummonerSpellType.Flash   => CastFlash(def),
            SummonerSpellType.Ghost   => CastGhost(def),
            SummonerSpellType.Exhaust => CastExhaust(def),
            SummonerSpellType.Barrier => CastBarrier(def),
            _ => false
        };

        if (ok)
            cdEnd[slot] = Time.time + def.cooldownSeconds;

        return ok;
    }

    private SummonerSpellDefinition GetDef(SummonerSlot slot)
        => slot == SummonerSlot.D ? spellD : spellF;

    // =========================
    // Flash: aim point within range
    // =========================
    private bool CastFlash(SummonerSpellDefinition def)
    {
        if (!hasAimPoint) return false;

        // ✅ 점멸 사용한 자리에서 1회 이펙트
        SpawnOneShotVFX(def.vfxOneShotPrefab, transform.position + def.vfxOffset);

        Vector3 p = aimWorldPoint;
        float maxRange = flashDistance * def.flashRangeMultiplier;

        Vector3 from = transform.position;
        Vector3 to = p;
        to.y = from.y;

        Vector3 delta = to - from;
        delta.y = 0f;

        if (delta.sqrMagnitude < 0.01f) return false;

        if (delta.magnitude > maxRange)
            to = from + delta.normalized * maxRange;

        var agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            bool ok = agent.Warp(to);
            agent.ResetPath();
            Debug.Log($"[Flash] Warp ok={ok} finalPos={transform.position}");
        }
        else
        {
            transform.position = to;
            Debug.Log($"[Flash] SetPos finalPos={transform.position}");
        }

        return true;
    }

    // =========================
    // Ghost: move speed +50% for 6s
    // =========================
    private bool CastGhost(SummonerSpellDefinition def)
    {
        var ghost = GetComponent<GhostBuff>();
        if (!ghost) ghost = gameObject.AddComponent<GhostBuff>();

        ghost.Apply(def.ghostDuration, def.moveSpeedBonusPercent, def.vfxFollowPrefab, def.vfxOffset);
        return true;
    }

    // =========================
    // Exhaust: choose a target in range (closest IDamageable)
    // =========================
    private bool CastExhaust(SummonerSpellDefinition def)
    {
        var target = FindExhaustTarget();
        if (!target) return false;

        var ex = target.GetComponent<ExhaustDebuff>();
        if (!ex) ex = target.gameObject.AddComponent<ExhaustDebuff>();

        ex.Apply(def.exhaustDuration, def.exhaustMoveSpeedMultiplier, def.exhaustDamageMultiplier, def.vfxFollowPrefab, def.vfxOffset);
        return true;
    }

    private Transform FindExhaustTarget()
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position,
            exhaustRange,
            exhaustTargetMask,
            QueryTriggerInteraction.Ignore
        );

        if (cols == null || cols.Length == 0) return null;

        Transform best = null;
        float bestDist = float.MaxValue;

        // ✅ 루트 중복(여러 콜라이더) 방지
        HashSet<Transform> seenRoots = new HashSet<Transform>();

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (!c) continue;

            var root = c.transform.root;
            if (!root || root == transform.root) continue;
            if (!seenRoots.Add(root)) continue;

            // ✅ "적" 판정: IDamageable이 root쪽에 있으면 OK (구조에 맞게 InParent/Children 선택)
            var dmg = root.GetComponentInChildren<IDamageable>();
            if (dmg == null) continue;

            float d = Vector3.Distance(transform.position, root.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = root;
            }
        }

        return best;
    }

    // =========================
    // Barrier: shield +16 for 3s
    // =========================
    private bool CastBarrier(SummonerSpellDefinition def)
    {
        var shield = GetComponent<ShieldBuff>();
        if (!shield) shield = gameObject.AddComponent<ShieldBuff>();

        shield.Apply(def.barrierDuration, def.barrierAmount, def.vfxFollowPrefab, def.vfxOffset);
        return true;
    }
    
    private void SpawnOneShotVFX(GameObject prefab, Vector3 worldPos)
    {
        if (!prefab) return;
        Instantiate(prefab, worldPos, Quaternion.identity);
    }

    private GameObject SpawnFollowVFX(GameObject prefab, Transform target, Vector3 localOffset)
    {
        if (!prefab || !target) return null;
        var go = Instantiate(prefab, target);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }
}