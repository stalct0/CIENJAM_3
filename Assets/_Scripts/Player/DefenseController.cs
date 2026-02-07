using UnityEngine;

public class DefenseController : MonoBehaviour
{
    [Header("Guard")]
    public bool guardActive = false;
    [Range(0f, 180f)] public float guardHalfAngle = 60f; // 총 120도 가드
    public bool knockbackImmuneWhileGuard = true;

    [Header("External Knockback Immune (e.g., R charging)")]
    public bool externalKnockbackImmune = false;

    public bool IsKnockbackImmune =>
        (guardActive && knockbackImmuneWhileGuard) || externalKnockbackImmune;

    public bool IsAttackFromFront(Vector3 attackerPos)
    {
        Vector3 toAttacker = attackerPos - transform.position;
        toAttacker.y = 0f;
        if (toAttacker.sqrMagnitude < 0.0001f) return true;
        toAttacker.Normalize();

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        fwd.Normalize();

        float ang = Vector3.Angle(fwd, toAttacker); // 0: 정면
        return ang <= guardHalfAngle;
    }
}