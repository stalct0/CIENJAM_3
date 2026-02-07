using UnityEngine;

public class UltGauge : MonoBehaviour
{
    [Range(0f, 100f)]
    [SerializeField] private float gauge = 0f;

    public float Gauge01 => gauge / 100f;
    public float GaugePercent => gauge;
    public bool IsFull => gauge >= 100f;

    // 초과분 버림(100에서 클램프)
    public void AddPercent(float percent)
    {
        if (percent <= 0f) return;
        if (IsFull) return; // 100%면 추가 획득 버림

        gauge = Mathf.Min(100f, gauge + percent);
        // Debug.Log($"[Ult] +{percent:F2}% => {gauge:F2}%");
    }

    public void SetToZero()
    {
        gauge = 0f;
    }

    /// <summary>
    /// 궁극기 사용 시도. 가득 차 있으면 소비하고 true.
    /// </summary>
    public bool TryConsumeFull()
    {
        if (!IsFull) return false;
        gauge = 0f;
        return true;
    }
}