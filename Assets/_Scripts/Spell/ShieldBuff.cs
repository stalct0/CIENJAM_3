using UnityEngine;
using System.Collections;

public class ShieldBuff : MonoBehaviour
{
    private Coroutine co;
    private HealthEX hp;

    private GameObject vfx;

    private void Awake()
    {
        hp = GetComponent<HealthEX>();
    }

    public void Apply(float duration, int amount, GameObject vfxPrefab, Vector3 vfxOffset)
    {
        if (!hp) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(duration, amount, vfxPrefab, vfxOffset));
    }

    private IEnumerator Co(float duration, int amount, GameObject vfxPrefab, Vector3 vfxOffset)
    {
        // ✅ VFX
        EnsureVFX(vfxPrefab, vfxOffset);

        hp.AddShield(amount);

        yield return new WaitForSeconds(duration);

        hp.RemoveShield(amount);

        CleanupVFX();
        co = null;
    }

    private void EnsureVFX(GameObject prefab, Vector3 offset)
    {
        if (vfx) return;
        if (!prefab) return;

        vfx = Instantiate(prefab, transform);
        vfx.transform.localPosition = offset;
        vfx.transform.localRotation = Quaternion.identity;
    }

    private void CleanupVFX()
    {
        if (!vfx) return;
        Destroy(vfx);
        vfx = null;
    }
}