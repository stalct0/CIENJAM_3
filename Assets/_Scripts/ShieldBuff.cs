using UnityEngine;
using System.Collections;

public class ShieldBuff : MonoBehaviour
{
    private Coroutine co;
    private HealthEX hp;

    private void Awake()
    {
        hp = GetComponent<HealthEX>();
    }

    public void Apply(float duration, int amount)
    {
        if (!hp) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, amount));
    }

    private IEnumerator Co(float duration, int amount)
    {
        hp.AddShield(amount);

        yield return new WaitForSeconds(duration);

        hp.RemoveShield(amount);
        co = null;
    }
}