using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;

    // 1. 네트워크 동기화 구조체 (RDown, RUp 추가)
    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount;
        public int CastQCount;
        public int CastWCount;
        public int CastECount;
        
        // R 키의 입력을 Down과 Up 각각 카운트로 관리
        public int CastRDownCount;
        public int CastRUpCount;

        public int MoveClickCount;
        public int HasAimPointCount;

        public Vector3 MoveWorldPoint;
        public Vector3 AimWorldPoint;
    }

    [Networked]
    public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;

    // 프록시 카운트 비교용 로컬 변수들
    private int _lastLMBCount;
    private int _lastQCount;
    private int _lastWCount;
    private int _lastECount;
    private int _lastRDownCount;
    private int _lastRUpCount;
    private int _lastMoveClickCount;
    private int _lastHasAimPointCount;

    private bool _isInitialized;

    public GameManager GameManager;

    public override void Spawned()
    {
        // A. 초기화: 핸들러 Update 차단 및 감지기 설정
        if (InputHandler != null) InputHandler.enabled = false;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCountsToCurrent();

        // B. GameManager 등록 (UI 활성화 및 로컬 참조용)
        if (HasStateAuthority)
        {
            GameManager=FindObjectOfType<GameManager>();
            if (GameManager != null) GameManager.LocalPlayer=this;
        }

        _isInitialized = true;
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

    void Update()
    {
        // 권한자는 매 프레임 실제 하드웨어 입력을 읽어 핸들러에 임시 저장
        if (HasStateAuthority && _isInitialized)
        {
            if (Mouse.current == null || Keyboard.current == null) return;
            InputHandler.UpdateAimPoint();
            InputHandler.ReadInputsIntoState();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized) return;

        // [권한자: 입력 전송]
        if (HasStateAuthority)
        {
            var state = NetworkedInput;

            // 핸들러의 bool 트리거를 네트워크 카운트로 변환
            if (InputHandler.inputState.castLMB) state.CastLMBCount++;
            if (InputHandler.inputState.castQ) state.CastQCount++;
            if (InputHandler.inputState.castW) state.CastWCount++;
            if (InputHandler.inputState.castE) state.CastECount++;
            
            // R 키 Down/Up 반영
            if (InputHandler.inputState.castRDown) state.CastRDownCount++;
            if (InputHandler.inputState.castRUp)   state.CastRUpCount++;

            if (InputHandler.inputState.moveClick) state.MoveClickCount++;
            if (InputHandler.inputState.hasAimPoint) state.HasAimPointCount++;

            state.MoveWorldPoint = InputHandler.inputState.moveWorldPoint;
            state.AimWorldPoint = InputHandler.inputState.aimWorldPoint;

            NetworkedInput = state;

            // 로직 실행 및 트리거 리셋
            InputHandler.DispatchStateToControllers();
            InputHandler.ClearOneFrameTriggers();
        }
    }

    public override void Render()
    {
        // [프록시: 입력 수신 및 실행]
        if (!HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(NetworkedInput))
                {
                    SyncNetworkInputToProxyHandler();
                    
                    // 프록시에서도 Dispatch 호출하여 애니메이션/이펙트 재생
                    InputHandler.DispatchStateToControllers();
                    
                    // 프록시 트리거 즉시 리셋 (중요)
                    InputHandler.ClearOneFrameTriggers();
                }
            }
        }
    }

    private void SyncNetworkInputToProxyHandler()
    {
        var current = NetworkedInput;

        // 카운트 차이로 버튼 '눌림/뗌' 상태 복원
        InputHandler.inputState.castLMB = (current.CastLMBCount != _lastLMBCount);
        InputHandler.inputState.castQ = (current.CastQCount != _lastQCount);
        InputHandler.inputState.castW = (current.CastWCount != _lastWCount);
        InputHandler.inputState.castE = (current.CastECount != _lastECount);
        
        // R 키 Down/Up 복원
        InputHandler.inputState.castRDown = (current.CastRDownCount != _lastRDownCount);
        InputHandler.inputState.castRUp = (current.CastRUpCount != _lastRUpCount);

        InputHandler.inputState.moveClick = (current.MoveClickCount != _lastMoveClickCount);
        InputHandler.inputState.hasAimPoint = (current.HasAimPointCount != _lastHasAimPointCount);

        InputHandler.inputState.moveWorldPoint = current.MoveWorldPoint;
        InputHandler.inputState.aimWorldPoint = current.AimWorldPoint;

        // 비교용 카운트 최신화
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