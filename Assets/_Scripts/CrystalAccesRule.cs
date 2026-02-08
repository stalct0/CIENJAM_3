using UnityEngine;

public enum CrystalAccessType
{
    OwnerOnly,  // 내 팀만 부술 수 있음
    EnemyOnly,  // 상대 팀만 부술 수 있음
    Anyone
}

public class CrystalAccessRule : MonoBehaviour
{
    public TeamId ownerTeam = TeamId.None;
    public CrystalAccessType access = CrystalAccessType.OwnerOnly;

    public bool CanBeDamagedBy(TeamId attackerTeam)
    {
        if (attackerTeam == TeamId.None || ownerTeam == TeamId.None) return false;

        return access switch
        {
            CrystalAccessType.OwnerOnly => attackerTeam == ownerTeam,
            CrystalAccessType.EnemyOnly => attackerTeam != ownerTeam,
            _ => true,
        };
    }
}