using UnityEngine;

public class WeaponEquipper : MonoBehaviour
{
 [Header("References")]
    [SerializeField] private Animator animator;

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

    private Transform socket;
    private GameObject spawned;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();

        // Animator가 Humanoid여야 GetBoneTransform이 안정적으로 됩니다.
        if (!animator || !animator.isHuman)
        {
            Debug.LogError("[WeaponEquipper] Animator가 없거나 Humanoid가 아닙니다.");
            return;
        }

        Transform handT = animator.GetBoneTransform(hand);
        if (!handT)
        {
            Debug.LogError("[WeaponEquipper] 손 본을 찾지 못했습니다. Rig가 Humanoid인지 확인하세요.");
            return;
        }

        if (createSocketObject)
        {
            // 손 아래에 WeaponSocket 만들거나 기존 것을 찾음
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
    }

    public void Unequip()
    {
        if (spawned) Destroy(spawned);
        spawned = null;
    }
}
