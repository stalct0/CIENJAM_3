using UnityEngine;

public class HealthEX : MonoBehaviour, IDamageable
{
    public int hp = 100;
    public float invincibleTime = 0.0f;

    float invUntil;

    public void TakeDamage(DamageInfo info)
    {
        if (Time.time < invUntil) return;

        hp -= info.amount;
        invUntil = Time.time + invincibleTime;

        Debug.Log($"{name} took {info.amount} from {info.attacker.name}. HP={hp}");
        if (hp <= 0) Destroy(gameObject);
    }
}