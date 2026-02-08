using UnityEngine;

[RequireComponent(typeof(CrystalHealth))]
public class CrystalObjective : MonoBehaviour
{
    public float ultGainOnDestroy = 34f;
    public bool destroySelfOnTrigger = false; // 보통 false 추천 (CrystalHealth가 파괴 담당)

    private void Awake()
    {
        var health = GetComponent<CrystalHealth>();
        if (health != null)
        {
            // 중복 등록 방지(안전)
            health.onDestroyedBy.RemoveListener(OnDestroyedBy);
            health.onDestroyedBy.AddListener(OnDestroyedBy);
        }
        else
        {
            Debug.LogError("[CrystalObjective] CrystalHealth가 없습니다.");
        }
    }

    public void OnDestroyedBy(GameObject attacker)
    {
        Debug.Log($"[CrystalObjective] Destroyed. attacker={(attacker ? attacker.name : "NULL")}");

        if (attacker == null) return;

        var ult = attacker.GetComponentInParent<UltGauge>();
        if (ult == null) ult = attacker.GetComponent<UltGauge>();

        if (ult == null)
        {
            Debug.LogWarning("[CrystalObjective] attacker에서 UltGauge를 못 찾았습니다.");
            return;
        }

        float before = ult.GaugePercent;
        ult.AddPercent(ultGainOnDestroy);
        Debug.Log($"[CrystalObjective] Ult +{ultGainOnDestroy}% ({before:F1}% -> {ult.GaugePercent:F1}%)");

        if (destroySelfOnTrigger)
            Destroy(gameObject);
    }
}