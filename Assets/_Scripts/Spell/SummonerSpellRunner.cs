using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class SummonerSpellRunner : MonoBehaviour
{
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

    private readonly Dictionary<SummonerSlot, float> cdEnd = new();

    // ✅ InputHandler가 넣어주는 aim point
    private bool hasAimPoint;
    private Vector3 aimWorldPoint;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!knockback) knockback = GetComponent<KnockbackController>();

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
        Debug.Log($"[Flash] hasAim={hasAimPoint} aim={aimWorldPoint}");

        if (!hasAimPoint) return false;

        Vector3 p = aimWorldPoint;

        float maxRange = flashDistance * def.flashRangeMultiplier;

        Vector3 from = transform.position;
        Vector3 to = p;
        to.y = from.y;

        Vector3 delta = to - from;
        delta.y = 0f;

        Debug.Log($"[Flash] from={from} to={to} dist={delta.magnitude} maxRange={maxRange}");

        if (delta.sqrMagnitude < 0.01f) return false;

        if (delta.magnitude > maxRange)
            to = from + delta.normalized * maxRange;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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

        ghost.Apply(def.ghostDuration, def.moveSpeedBonusPercent);
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

        ex.Apply(def.exhaustDuration, def.exhaustMoveSpeedMultiplier, def.exhaustDamageMultiplier);
        return true;
    }

    private Transform FindExhaustTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, exhaustRange, exhaustTargetMask, QueryTriggerInteraction.Ignore);
        if (cols == null || cols.Length == 0) return null;

        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (!c) continue;
            if (c.transform.root == transform.root) continue;

            var dmg = c.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }

        return best ? best.root : null;
    }

    // =========================
    // Barrier: shield +16 for 3s
    // =========================
    private bool CastBarrier(SummonerSpellDefinition def)
    {
        var shield = GetComponent<ShieldBuff>();
        if (!shield) shield = gameObject.AddComponent<ShieldBuff>();

        shield.Apply(def.barrierDuration, def.barrierAmount);
        return true;
    }
}