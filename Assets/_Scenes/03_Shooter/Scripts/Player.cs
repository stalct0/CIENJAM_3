using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;
    public GameManager GameManager;

    // 1. 네트워크 동기화 변수들 (HP, 게이지 추가)
    [Networked] public float NetworkedHP { get; set; }
    [Networked] public float NetworkedMaxHP { get; set; } = 100f;
    [Networked] public float NetworkedUltGauge { get; set; } // 궁극기 게이지 추가

    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount, CastQCount, CastWCount, CastECount;
        public int CastRDownCount, CastRUpCount;
        public int CastDCount, CastFCount;
        public int MoveClickCount, HasAimPointCount;
        public Vector3 MoveWorldPoint, AimWorldPoint;
    }

    [Networked] public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;
    private bool _isInitialized;
    private bool _identitySetupDone;
    
    // 하위 컴포넌트 참조
    private HealthEX _healthComponent;
    private UltGauge _ultGaugeComponent; 

    private int _lCount, _qCount, _wCount, _eCount, _rdCount, _ruCount, _dCount, _fCount, _mCount, _hCount;

    public override void Spawned()
    {
        if (InputHandler == null) InputHandler = GetComponentInChildren<PlayerInputHandler>();
        if (InputHandler != null) InputHandler.enabled = false;
        
        // 컴포넌트 탐색
        _healthComponent = GetComponentInChildren<HealthEX>();
        _ultGaugeComponent = GetComponentInChildren<UltGauge>();
        
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCountsToCurrent();
        
        GameManager = FindObjectOfType<GameManager>();
        if (HasStateAuthority) NetworkedHP = NetworkedMaxHP;

        _isInitialized = true;
    }

    public void Init() { if (HasStateAuthority) SetupIdentityAndUI(); }

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

            // 2. [권한자] 로컬 데이터 -> 네트워크 변수로 복사
            if (_healthComponent != null) NetworkedHP = _healthComponent.hp;
            
            // 궁극기 게이지 실시간 동기화
            if (_ultGaugeComponent != null) NetworkedUltGauge = _ultGaugeComponent.GaugePercent;
        }
    }

    public override void Render()
    {
        if (!_isInitialized) return;

        if (!HasStateAuthority && !_identitySetupDone && Runner.SessionInfo.PlayerCount >= 2)
            SetupIdentityAndUI();

        // 3. 네트워크 변화 감지 및 프록시 적용
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
            
            if (change == nameof(NetworkedHP))
            {
                if (!HasStateAuthority && _healthComponent != null)
                    _healthComponent.hp = (int)NetworkedHP;
            }

            // [프록시] 궁극기 게이지 동기화
            if (change == nameof(NetworkedUltGauge))
            {
                if (!HasStateAuthority && _ultGaugeComponent != null)
                {
                    // 프록시의 UltGauge 값을 네트워크 값으로 강제 동기화
                    // (UltGauge 스크립트 수정 없이 값만 덮어씌움)
                    var field = typeof(UltGauge).GetField("gauge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(_ultGaugeComponent, NetworkedUltGauge);
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized || !HasStateAuthority) return;

        var state = NetworkedInput;
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
        s.castLMB = (c.CastLMBCount != _lCount); s.castQ = (c.CastQCount != _qCount);
        s.castW = (c.CastWCount != _wCount); s.castE = (c.CastECount != _eCount);
        s.castRDown = (c.CastRDownCount != _rdCount); s.castRUp = (c.CastRUpCount != _ruCount);
        s.castD = (c.CastDCount != _dCount); s.castF = (c.CastFCount != _fCount);
        s.moveClick = (c.MoveClickCount != _mCount); s.hasAimPoint = (c.HasAimPointCount != _hCount);
        s.moveWorldPoint = c.MoveWorldPoint; s.aimWorldPoint = c.AimWorldPoint;
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