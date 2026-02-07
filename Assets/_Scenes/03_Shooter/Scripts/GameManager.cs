using UnityEngine;
using Fusion;

/// <summary>
/// Handles player connections (spawning of Player instances).
/// </summary>
public sealed class GameManager : NetworkBehaviour
{
    public Player PlayerPrefab;
    public Player LocalPlayer { get; set; }

    private SpawnPoint[] _spawnPoints;

    public Vector3 GetSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0) return Vector3.zero;

        var spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius;
        return spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
    }

    public override void Spawned()
    {
        _spawnPoints = FindObjectsOfType<SpawnPoint>();

        // Shared Mode에서는 각 클라이언트가 자신의 Player를 직접 Spawn합니다.
        // Runner.LocalPlayer를 소유권(Input Authority)으로 넘겨줍니다.
        var myPlayer = Runner.Spawn(PlayerPrefab, GetSpawnPosition(), Quaternion.identity, Runner.LocalPlayer);
        
        // 생성된 객체가 내 것이라면 LocalPlayer로 등록
        LocalPlayer = myPlayer;

        // Fusion 내부 시스템에 내 플레이어 객체 등록
        Runner.SetPlayerObject(Runner.LocalPlayer, myPlayer.Object);
    }

    public override void FixedUpdateNetwork()
    {
        // 필요한 경우 여기서 전체 플레이어 상태를 체크하는 로직을 넣습니다.
        // 현재는 루프만 돌고 있으므로 비워두어도 무방합니다.
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        LocalPlayer = null;
    }
}