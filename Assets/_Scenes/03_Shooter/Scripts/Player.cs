using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;
    public GameManager GameManager;

    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount;
        public int CastQCount;
        public int CastWCount;
        public int CastECount;
        public int CastRDownCount;
        public int CastRUpCount;
        public int MoveClickCount;
        public int HasAimPointCount;
        public Vector3 MoveWorldPoint;
        public Vector3 AimWorldPoint;
    }

    [Networked] public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;
    private bool _isInitialized;
    private bool _identitySetupDone;

    private int _lastLMBCount, _lastQCount, _lastWCount, _lastECount, _lastRDownCount, _lastRUpCount, _lastMoveClickCount, _lastHasAimPointCount;

    public override void Spawned()
    {
        if (InputHandler != null) InputHandler.enabled = false;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCountsToCurrent();
        
        GameManager = FindObjectOfType<GameManager>();
        _isInitialized = true;
    }

    // GameManager에서 스폰 직후 호출
    public void Init()
    {
        if (!HasStateAuthority) return;
        SetupIdentityAndUI();
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 1. 권한자는 실제 입력을 읽음
        if (HasStateAuthority)
        {
            if (Mouse.current != null && Keyboard.current != null)
            {
                InputHandler.UpdateAimPoint();
                InputHandler.ReadInputsIntoState();
            }
        }
    }

    public override void Render()
    {
        if (!_isInitialized) return;

        // 2. [Proxy 전용] 내 화면에 보이는 상대방 캐릭터의 팀 설정 (한 번만)
        if (!HasStateAuthority && !_identitySetupDone && Runner.SessionInfo.PlayerCount >= 2)
        {
            SetupIdentityAndUI();
        }

        // 3. [Proxy 전용] 입력 동기화
        if (!HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(NetworkedInput))
                {
                    SyncNetworkInputToProxyHandler();
                    InputHandler.DispatchStateToControllers();
                    InputHandler.ClearOneFrameTriggers();
                }
            }
        }
    }

    private void SetupIdentityAndUI()
    {
        if (GameManager == null || GameManager.BattleUIManager == null) return;
        var identity = InputHandler.GetComponent<CombatIdentity>();
        if (identity == null) return;

        // 내가 조종하는 로컬 캐릭터인 경우
        if (HasStateAuthority)
        {
            if (GameManager.player == 0) // 레드팀
            {
                identity.SetIdentity(0, TeamId.A, 0);
                GameManager.BattleUIManager.RedPlayer = InputHandler.gameObject;
            }
            else // 블루팀
            {
                identity.SetIdentity(1, TeamId.B, 1);
                GameManager.BattleUIManager.BluePlayer = InputHandler.gameObject;
            }

            // 내 UI에 정보 연결
            GameManager.BattleUIManager.playerRunner = InputHandler.skillRunner;
            GameManager.BattleUIManager.spellRunner = InputHandler.summoner;
        }
        // 내 화면에 보이는 상대방(Proxy) 캐릭터인 경우
        else
        {
            // 내가 레드팀(0)이면 상대는 블루팀(1), 내가 블루팀(1)이면 상대는 레드팀(0)
            if (GameManager.player == 0)
            {
                identity.SetIdentity(1, TeamId.B, 1);
                GameManager.BattleUIManager.BluePlayer = InputHandler.gameObject;
            }
            else
            {
                identity.SetIdentity(0, TeamId.A, 0);
                GameManager.BattleUIManager.RedPlayer = InputHandler.gameObject;
            }
        }

        _identitySetupDone = true;
        Debug.Log($"[Identity Setup] {gameObject.name} as {(HasStateAuthority ? "Local" : "Proxy")}");
    }

    // ... (FixedUpdateNetwork, SyncCountsToCurrent, SyncNetworkInputToProxyHandler는 기존과 동일)
    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized || !HasStateAuthority) return;
        var state = NetworkedInput;
        if (InputHandler.inputState.castLMB) state.CastLMBCount++;
        if (InputHandler.inputState.castQ) state.CastQCount++;
        if (InputHandler.inputState.castW) state.CastWCount++;
        if (InputHandler.inputState.castE) state.CastECount++;
        if (InputHandler.inputState.castRDown) state.CastRDownCount++;
        if (InputHandler.inputState.castRUp) state.CastRUpCount++;
        if (InputHandler.inputState.moveClick) state.MoveClickCount++;
        if (InputHandler.inputState.hasAimPoint) state.HasAimPointCount++;
        state.MoveWorldPoint = InputHandler.inputState.moveWorldPoint;
        state.AimWorldPoint = InputHandler.inputState.aimWorldPoint;
        NetworkedInput = state;
        InputHandler.DispatchStateToControllers();
        InputHandler.ClearOneFrameTriggers();
    }

    private void SyncCountsToCurrent()
    {
        var current = NetworkedInput;
        _lastLMBCount = current.CastLMBCount;
        _lastQCount = current.CastQCount;
        _lastWCount = current.CastWCount;
        _lastECount = current.CastECount;
        _lastRDownCount = current.CastRDownCount;
        _lastRUpCount = current.CastRUpCount;
        _lastMoveClickCount = current.MoveClickCount;
        _lastHasAimPointCount = current.HasAimPointCount;
    }

    private void SyncNetworkInputToProxyHandler()
    {
        var current = NetworkedInput;
        InputHandler.inputState.castLMB = (current.CastLMBCount != _lastLMBCount);
        InputHandler.inputState.castQ = (current.CastQCount != _lastQCount);
        InputHandler.inputState.castW = (current.CastWCount != _lastWCount);
        InputHandler.inputState.castE = (current.CastECount != _lastECount);
        InputHandler.inputState.castRDown = (current.CastRDownCount != _lastRDownCount);
        InputHandler.inputState.castRUp = (current.CastRUpCount != _lastRUpCount);
        InputHandler.inputState.moveClick = (current.MoveClickCount != _lastMoveClickCount);
        InputHandler.inputState.hasAimPoint = (current.HasAimPointCount != _lastHasAimPointCount);
        InputHandler.inputState.moveWorldPoint = current.MoveWorldPoint;
        InputHandler.inputState.aimWorldPoint = current.AimWorldPoint;
        _lastLMBCount = current.CastLMBCount;
        _lastQCount = current.CastQCount;
        _lastWCount = current.CastWCount;
        _lastECount = current.CastECount;
        _lastRDownCount = current.CastRDownCount;
        _lastRUpCount = current.CastRUpCount;
        _lastMoveClickCount = current.MoveClickCount;
        _lastHasAimPointCount = current.HasAimPointCount;
    }
}