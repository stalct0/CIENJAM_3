using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;
    public GameManager GameManager;

    // 1. 체력 및 상태 네트워크 변수
    [Networked] public float NetworkedHP { get; set; }
    [Networked] public float NetworkedMaxHP { get; set; } = 100f;

    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount, CastQCount, CastWCount, CastECount;
        public int CastRDownCount, CastRUpCount;
        public int CastDCount, CastFCount; // D, F 스킬 추가
        public int MoveClickCount, HasAimPointCount;
        public Vector3 MoveWorldPoint, AimWorldPoint;
    }

    [Networked] public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;
    private bool _isInitialized;
    private bool _identitySetupDone;
    private HealthEX _healthComponent; // 로컬 체력 컴포넌트

    // 프록시 카운트 비교용 로컬 변수
    private int _lCount, _qCount, _wCount, _eCount, _rdCount, _ruCount, _dCount, _fCount, _mCount, _hCount;

    public override void Spawned()
    {
        if (InputHandler != null) InputHandler.enabled = false;
        _healthComponent = GetComponentInChildren<HealthEX>();
        
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCountsToCurrent();
        
        GameManager = FindObjectOfType<GameManager>();
        
        // 초기 체력 설정 (권한자)
        if (HasStateAuthority) NetworkedHP = NetworkedMaxHP;

        _isInitialized = true;
    }

    public void Init()
    {
        if (!HasStateAuthority) return;
        SetupIdentityAndUI();
    }

    private void Update()
    {
        if (!_isInitialized) return;

        if (HasStateAuthority)
        {
            if (Mouse.current != null && Keyboard.current != null)
            {
                InputHandler.UpdateAimPoint();
                InputHandler.ReadInputsIntoState();
            }

            // 2. [권한자] 로컬 체력 값을 네트워크 변수로 브릿지
            if (_healthComponent != null)
            {
                NetworkedHP = _healthComponent.hp;
            }
        }
    }

    public override void Render()
    {
        if (!_isInitialized) return;

        // 3. 팀 및 UI 설정 (모든 클라이언트)
        if (!HasStateAuthority && !_identitySetupDone && Runner.SessionInfo.PlayerCount >= 2)
        {
            SetupIdentityAndUI();
        }

        // 4. 네트워크 변화 감지
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedInput))
            {
                if (!HasStateAuthority)
                {
                    SyncNetworkInputToProxyHandler();
                    InputHandler.DispatchStateToControllers();
                    InputHandler.ClearOneFrameTriggers();
                }
            }
            
            // 5. [프록시] 체력 변화 동기화
            if (change == nameof(NetworkedHP))
            {
                if (!HasStateAuthority && _healthComponent != null)
                {
                    _healthComponent.hp = (int)NetworkedHP;
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized || !HasStateAuthority) return;

        var state = NetworkedInput;
        // 입력 수집 (D, F 포함)
        var s = InputHandler.inputState;
        if (s.castLMB) state.CastLMBCount++;
        if (s.castQ) state.CastQCount++;
        if (s.castW) state.CastWCount++;
        if (s.castE) state.CastECount++;
        if (s.castRDown) state.CastRDownCount++;
        if (s.castRUp) state.CastRUpCount++;
        if (s.castD) state.CastDCount++;
        if (s.castF) state.CastFCount++;
        if (s.moveClick) state.MoveClickCount++;
        if (s.hasAimPoint) state.HasAimPointCount++;
        
        state.MoveWorldPoint = s.moveWorldPoint;
        state.AimWorldPoint = s.aimWorldPoint;
        
        NetworkedInput = state;

        // 권한자 로직 실행
        InputHandler.DispatchStateToControllers();
        InputHandler.ClearOneFrameTriggers();
    }

    private void SyncCountsToCurrent()
    {
        var current = NetworkedInput;
        _lCount = current.CastLMBCount; _qCount = current.CastQCount;
        _wCount = current.CastWCount; _eCount = current.CastECount;
        _rdCount = current.CastRDownCount; _ruCount = current.CastRUpCount;
        _dCount = current.CastDCount; _fCount = current.CastFCount;
        _mCount = current.MoveClickCount; _hCount = current.HasAimPointCount;
    }

    private void SyncNetworkInputToProxyHandler()
    {
        var c = NetworkedInput;
        var s = InputHandler.inputState;

        s.castLMB = (c.CastLMBCount != _lCount);
        s.castQ = (c.CastQCount != _qCount);
        s.castW = (c.CastWCount != _wCount);
        s.castE = (c.CastECount != _eCount);
        s.castRDown = (c.CastRDownCount != _rdCount);
        s.castRUp = (c.CastRUpCount != _ruCount);
        s.castD = (c.CastDCount != _dCount);
        s.castF = (c.CastFCount != _fCount);
        s.moveClick = (c.MoveClickCount != _mCount);
        s.hasAimPoint = (c.HasAimPointCount != _hCount);
        s.moveWorldPoint = c.MoveWorldPoint;
        s.aimWorldPoint = c.AimWorldPoint;

        InputHandler.inputState = s;

        _lCount = c.CastLMBCount; _qCount = c.CastQCount; _wCount = c.CastWCount; _eCount = c.CastECount;
        _rdCount = c.CastRDownCount; _ruCount = c.CastRUpCount; _dCount = c.CastDCount; _fCount = c.CastFCount;
        _mCount = c.MoveClickCount; _hCount = c.HasAimPointCount;
    }

    private void SetupIdentityAndUI()
    {
        if (GameManager == null || GameManager.BattleUIManager == null) return;
        var identity = InputHandler.GetComponent<CombatIdentity>();
        if (identity == null) return;

        if (HasStateAuthority)
        {
            if (GameManager.player == 0) {
                identity.SetIdentity(0, TeamId.A, 0);
                GameManager.BattleUIManager.RedPlayer = InputHandler.gameObject;
            } else {
                identity.SetIdentity(1, TeamId.B, 1);
                GameManager.BattleUIManager.BluePlayer = InputHandler.gameObject;
            }
            // 스킬 및 소환사 주문 쿨다운(시간) 동기화를 위해 Runner 연결
            GameManager.BattleUIManager.playerRunner = InputHandler.skillRunner;
            GameManager.BattleUIManager.spellRunner = InputHandler.summoner;
        }
        else
        {
            if (GameManager.player == 0) {
                identity.SetIdentity(1, TeamId.B, 1);
                GameManager.BattleUIManager.BluePlayer = InputHandler.gameObject;
            } else {
                identity.SetIdentity(0, TeamId.A, 0);
                GameManager.BattleUIManager.RedPlayer = InputHandler.gameObject;
            }
        }
        _identitySetupDone = true;
    }
}