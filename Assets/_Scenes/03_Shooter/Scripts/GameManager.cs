using System.Linq;
using Fusion;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public Player PlayerPrefab;
    public SpawnPoint spA, spB;
    public int player; // 로컬 플레이어 인덱스 (0 or 1)

    [Header("UI & Camera")]
    public BattleUIManager BattleUIManager;
    public GameObject sceneCamera;

    [Networked] public NetworkBool IsBattleStarted { get; set; }

    public Player LocalPlayer { get; set; }
    private bool _localStartDone;

    private void Awake() { if (sceneCamera != null) sceneCamera.SetActive(false); }

    public override void Spawned() {
        player = Runner.IsSharedModeMasterClient ? 0 : 1;
        SpawnPoint target = (player == 0) ? spA : spB;
        var spawnPos = target.transform.position + (Vector3)(Random.insideUnitCircle * target.Radius);
        
        var myPlayer = Runner.Spawn(PlayerPrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer);
        LocalPlayer = myPlayer;
        Runner.SetPlayerObject(Runner.LocalPlayer, myPlayer.Object);
        myPlayer.Init();
    }

    public override void FixedUpdateNetwork() {
        if (Runner.IsSharedModeMasterClient && !IsBattleStarted && Runner.SessionInfo.PlayerCount >= 2) 
            IsBattleStarted = true;
    }

    public override void Render() {
        if (IsBattleStarted && !_localStartDone && LocalPlayer != null) {
            _localStartDone = true;
            if (sceneCamera != null) sceneCamera.SetActive(true);
            if (BattleUIManager != null) BattleUIManager.gameObject.SetActive(true);
        }
    }
}