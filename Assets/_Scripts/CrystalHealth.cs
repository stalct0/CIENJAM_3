using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CrystalHealth : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int maxHp = 16;
    public int hp = 16;

    [Header("Destroy")]
    public bool destroyOnZero = true;
    public float destroyDelay = 0f;

    [Header("Debug")]
    public bool logDamage = true;

    // ✅ "누가 부쉈는지"를 외부(예: CrystalObjective)가 받기 위한 이벤트
    // CrystalHealth는 보상/게이지 등을 몰라도 됨 (분리)
    [System.Serializable]
    public class DestroyedEvent : UnityEvent<GameObject> { }
    public DestroyedEvent onDestroyedBy;

    private bool dead;

    private void Awake()
    {
        if (maxHp <= 0) maxHp = 1;
        hp = Mathf.Clamp(hp, 0, maxHp);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (dead) return;

        var rule = GetComponent<CrystalAccessRule>();
        if (rule != null && !rule.CanBeDamagedBy(info.attackerTeam))
            return; 
        
        int dmg = Mathf.Max(0, info.amount);
        if (dmg <= 0) return;

        int prev = hp;
        hp = Mathf.Max(0, hp - dmg);

        if (logDamage)
        {
            string atkName = info.attacker ? info.attacker.name : "NULL";
            Debug.Log($"[CrystalHealth] {name} took {dmg} from {atkName}. HP {prev}->{hp}");
        }

        if (hp <= 0)
        {
            dead = true;

            // ✅ 외부에 "누가 부쉈는지" 알림 (보상은 외부가 처리)
            onDestroyedBy?.Invoke(info.attacker);

            if (destroyOnZero)
            {
                if (destroyDelay <= 0f) Destroy(gameObject);
                else Destroy(gameObject, destroyDelay);
            }
        }
    }

    // 편의 함수
    public void Heal(int amount)
    {
        if (dead) return;
        hp = Mathf.Clamp(hp + Mathf.Max(0, amount), 0, maxHp);
    }

    public void ResetHP()
    {
        dead = false;
        hp = maxHp;
    }
}