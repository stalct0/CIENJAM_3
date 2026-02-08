using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;
    public GameManager GameManager;

    // 1. 체력 네트워크 변수 (UI 동기화용)
    [Networked] public float NetworkedHP { get; set; }
    [Networked] public float NetworkedMaxHP { get; set; } = 100f;

    // 2. 네트워크 입력 구조체 (D, F 키 포함 - 순서 엄수)
    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount;
        public int CastQCount;
        public int CastWCount;
        public int CastECount;
        public int CastRDownCount;
        public int CastRUpCount;
        public int CastDCount; // 추가된 소환사 주문 D
        public int CastFCount; // 추가된 소환사 주문 F
        public int MoveClickCount;
        public int HasAimPointCount;
        public Vector3 MoveWorldPoint;
        public Vector3 AimWorldPoint;
    }

    [Networked] public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;
    private bool _isInitialized;
    private bool _identitySetupDone;
    private HealthEX _healthComponent;

    // 비교용 로컬 카운트 변수들
    private int _lCount, _qCount, _wCount, _eCount, _rdCount, _ruCount, _dCount, _fCount, _mCount, _hCount;

    public override void Spawned()
    {
        if (InputHandler == null) InputHandler = GetComponentInChildren<PlayerInputHandler>();
        if (InputHandler != null) InputHandler.enabled = false;
        
        _healthComponent = GetComponentInChildren<HealthEX>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        // 스폰 시점의 네트워크 상태를 로컬 카운트에 즉시 반영
        SyncCountsToCurrent();
        
        GameManager = FindObjectOfType<GameManager>();
        
        // 권한자가 초기 체력 설정
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
            // 1. 하드웨어 입력 읽기
            if (Mouse.current != null && Keyboard.current != null)
            {
                InputHandler.UpdateAimPoint();
                InputHandler.ReadInputsIntoState();
            }

            // 2. 체력 실시간 브릿지 (Local -> Networked)
            if (_healthComponent != null) NetworkedHP = _healthComponent.hp;
        }
    }

    public override void Render()
    {
        if (!_isInitialized) return;

        // 3. 팀 설정 및 UI 연결 (모든 클라이언트)
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
                    // [Proxy] 입력 복원 및 실행
                    SyncNetworkInputToProxyHandler();
                    InputHandler.DispatchStateToControllers();
                    InputHandler.ClearOneFrameTriggers();
                }
            }
            
            if (change == nameof(NetworkedHP))
            {
                // [Proxy] 체력 동기화
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
        var s = InputHandler.inputState;

        // 5. 입력을 카운트로 변환하여 네트워크 변수에 기록 (R, D, F 포함)
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

        // [Owner] 로컬 컨트롤러 실행
        InputHandler.DispatchStateToControllers();
        InputHandler.ClearOneFrameTriggers();
    }

    private void SyncCountsToCurrent()
    {
        var c = NetworkedInput;
        _lCount = c.CastLMBCount; _qCount = c.CastQCount;
        _wCount = c.CastWCount; _eCount = c.CastECount;
        _rdCount = c.CastRDownCount; _ruCount = c.CastRUpCount;
        _dCount = c.CastDCount; _fCount = c.CastFCount;
        _mCount = c.MoveClickCount; _hCount = c.HasAimPointCount;
    }

    private void SyncNetworkInputToProxyHandler()
    {
        var c = NetworkedInput;
        var s = InputHandler.inputState;

        // 카운트 차이 비교를 통해 bool 상태 복원
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

        // 로컬 카운트 최신화 (다음 프레임 비교용)
        _lCount = c.CastLMBCount; _qCount = c.CastQCount;
        _wCount = c.CastWCount; _eCount = c.CastECount;
        _rdCount = c.CastRDownCount; _ruCount = c.CastRUpCount;
        _dCount = c.CastDCount; _fCount = c.CastFCount;
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
            // 6. UI 매니저에 Runner 연결 (실시간 쿨다운/시간 동기화)
            GameManager.BattleUIManager.playerRunner = InputHandler.skillRunner;
            GameManager.BattleUIManager.spellRunner = InputHandler.summoner;
        }
        else
        {
            // 프록시 팀 설정
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