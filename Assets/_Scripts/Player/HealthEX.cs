using UnityEngine;

public class HealthEX : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int hp = 100;

    [Header("Invincibility")]
    public float invincibleTime = 0.0f;
    private float invUntil;

    [Header("Knockback")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackDuration = 0.12f;
    [SerializeField] private bool lockInputDuringKnockback = true;
    [SerializeField] private Animator animator;
    [SerializeField] private int shield;
    
    private KnockbackController kb;

    private void Awake()
    {
        kb = GetComponent<KnockbackController>();
        if (!kb) kb = GetComponentInParent<KnockbackController>();
        if (!kb) kb = GetComponentInChildren<KnockbackController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(DamageInfo info)
    {
        if (Time.time < invUntil) return;

        if (animator && hp > 0)
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }
        int dmg = info.amount;
        // 공격자가 탈진 상태면 피해 감소(주는 피해 감소 구현)
        if (info.attacker != null)
        {
            var ex = info.attacker.GetComponentInParent<ExhaustDebuff>();
            if (ex != null && ex.IsActive)
                dmg = Mathf.RoundToInt(dmg * ex.DamageMultiplier);
        }

        // 보호막 먼저 소모
        if (shield > 0)
        {
            int use = Mathf.Min(shield, dmg);
            shield -= use;
            dmg -= use;

            // (*체력 피해를 받지 않으면 경직 X) -> 여기서 dmg==0이면 넉백/경직 처리 안 하면 됨
            if (dmg <= 0) return;
        }
        
        hp -= info.amount;
        invUntil = Time.time + invincibleTime;

        // ✅ 넉백 적용 (스킬은 끊지 않고, 입력만 잠그는 구조)
        if (enableKnockback && kb != null)
        {
            Vector3 from;

            // 공격자가 있으면 그 위치 기준으로 밀기
            if (info.attacker != null)
            {
                from = info.attacker.transform.position;
            }
            else
            {
                // attacker가 없으면 hitDir(공격자->피격자 방향)을 이용해 "가짜 from" 생성
                // ApplyKnockback는 (transform.position - from) 방향으로 밀기 때문에
                // from을 피격자 위치에서 hitDir만큼 뒤로 두면 같은 결과가 납니다.
                Vector3 dir = info.hitDir;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
                dir.Normalize();
                from = transform.position - dir;
            }

            kb.ApplyKnockback(from, knockbackDistance, knockbackDuration, lockInputDuringKnockback);
        }

        Debug.Log($"{name} took {info.amount} from {(info.attacker ? info.attacker.name : "NULL")}. HP={hp}");

        if (hp <= 0)
        {
            hp = 0;
            if (animator)
            {
                animator.SetBool("Dead", true);
            }
        }
    }
    public void AddShield(int amount) => shield += Mathf.Max(0, amount);

    public void RemoveShield(int amount)
    {
        shield -= Mathf.Max(0, amount);
        if (shield < 0) shield = 0;
    }
}