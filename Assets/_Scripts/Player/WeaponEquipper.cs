using UnityEngine;

public class WeaponEquipper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkillRunner skillRunner; // 추가

    [Header("Weapon Prefab")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Attach Target")]
    [SerializeField] private HumanBodyBones hand = HumanBodyBones.RightHand;

    [Header("Local Offset (adjust in Inspector)")]
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Header("Options")]
    [SerializeField] private bool createSocketObject = true;
    [SerializeField] private string socketName = "WeaponSocket";

    [Header("VFX Socket Names (inside weapon prefab)")]
    [SerializeField] private string swordTipName = "VFX_SwordTip";
    [SerializeField] private string swordBaseName = "VFX_SwordBase";

    private Transform socket;
    private GameObject spawned;

    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;
    private Vector3 baseLocalScale;
    private bool baseCached;
    
    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        skillRunner = GetComponent<SkillRunner>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!skillRunner) skillRunner = GetComponent<SkillRunner>();

        Transform handT = animator.GetBoneTransform(hand);

        if (createSocketObject)
        {
            socket = handT.Find(socketName);
            if (!socket)
            {
                GameObject go = new GameObject(socketName);
                socket = go.transform;
                socket.SetParent(handT, false);
                socket.localPosition = Vector3.zero;
                socket.localRotation = Quaternion.identity;
                socket.localScale = Vector3.one;
            }
        }
        else
        {
            socket = handT;
        }
    }

    private void Start()
    {
        if (weaponPrefab) Equip(weaponPrefab);
    }

    public void Equip(GameObject prefab)
    {
        if (!socket || !prefab) return;

        if (spawned) Destroy(spawned);

        spawned = Instantiate(prefab, socket);
        spawned.transform.localPosition = localPosition;
        spawned.transform.localRotation = Quaternion.Euler(localEulerAngles);
        spawned.transform.localScale = localScale;
        CacheBasePose();
        
        if (skillRunner)
        {
            Transform tip = FindDeepChild(spawned.transform, swordTipName);
            Transform bas = FindDeepChild(spawned.transform, swordBaseName);

            skillRunner.swordTip = tip;
            skillRunner.swordBase = bas;
        }
    }

    public void Unequip()
    {
        if (spawned) Destroy(spawned);
        spawned = null;

        if (skillRunner)
        {
            skillRunner.swordTip = null;
            skillRunner.swordBase = null;
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;

        // 1) 직계 Find로 먼저 시도(경로도 가능)
        var direct = root.Find(name);
        if (direct) return direct;

        // 2) 이름 전체 탐색
        var stack = new System.Collections.Generic.Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
        return null;
    }
    
    private void CacheBasePose()
    {
        if (!spawned) return;
        baseLocalPos = spawned.transform.localPosition;
        baseLocalRot = spawned.transform.localRotation;
        baseLocalScale = spawned.transform.localScale;
        baseCached = true;
    }
    public void ApplyWeaponOffset(Vector3 localPos, Vector3 localEuler)
    {
        if (!spawned) return;
        if (!baseCached) CacheBasePose();

        spawned.transform.localPosition = baseLocalPos + localPos;
        spawned.transform.localRotation = baseLocalRot * Quaternion.Euler(localEuler);
    }
    public void ResetWeaponOffset()
    {
        if (!spawned || !baseCached) return;
        spawned.transform.localPosition = baseLocalPos;
        spawned.transform.localRotation = baseLocalRot;
        spawned.transform.localScale = baseLocalScale;
    }
}