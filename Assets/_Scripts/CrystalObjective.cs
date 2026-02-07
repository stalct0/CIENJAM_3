using UnityEngine;

public class CrystalObjective : MonoBehaviour
{
    public float ultGainOnDestroy = 34f;

    public void OnDestroyedBy(GameObject attacker)
    {
        var ult = attacker.GetComponentInParent<UltGauge>();
        if (ult == null) ult = attacker.GetComponent<UltGauge>();
        if (ult != null) ult.AddPercent(ultGainOnDestroy);

        Destroy(gameObject);
    }
}
