using UnityEngine;

public class CombatIdentity : MonoBehaviour
{
    [Header("Assigned by server on spawn")]
    [SerializeField] private ulong ownerId;   // 누가 조작하는지(클라이언트/플레이어)
    [SerializeField] private uint entityId;   // 유닛 고유 ID(디버그/로그/타겟팅)
    [SerializeField] private TeamId team;     // 팀

    public ulong OwnerId => ownerId;
    public uint EntityId => entityId;
    public TeamId Team => team;

    // 서버에서 스폰 시점에 호출해서 값 세팅 + (네트워크 동기화는 스택에 맞게 별도로)
    public void SetIdentity(ulong newOwnerId, TeamId newTeam, uint newEntityId)
    {
        ownerId = newOwnerId;
        team = newTeam;
        entityId = newEntityId;
    }

    public bool IsSameOwner(CombatIdentity other)
        => other != null && other.ownerId == ownerId;

    public bool IsSameTeam(CombatIdentity other)
        => other != null && other.team == team && team != TeamId.None;
}