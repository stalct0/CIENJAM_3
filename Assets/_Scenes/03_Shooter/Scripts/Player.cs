using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler InputHandler;

    // 1. 네트워크 동기화 데이터 구조체
    public struct PlayerNetworkInputState : INetworkStruct
    {
        public int CastLMBCount;
        public int CastQCount;
        public int CastWCount;
        public int CastECount;
        public int CastRCount;
        public int MoveClickCount;
        public int HasAimPointCount;

        public Vector3 MoveWorldPoint;
        public Vector3 AimWorldPoint;
    }

    [Networked]
    public PlayerNetworkInputState NetworkedInput { get; set; }

    private ChangeDetector _changeDetector;

    public GameManager GameManager;

    // 프록시 카운트 비교용 로컬 변수
    private int _lastLMBCount;
    private int _lastQCount;
    private int _lastWCount;
    private int _lastECount;
    private int _lastRCount;
    private int _lastMoveClickCount;
    private int _lastHasAimPointCount;

    private bool _isInitialized;

    public override void Spawned()
    {
        // A. 기본 초기화
        if (InputHandler != null) InputHandler.enabled = false;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCountsToCurrent();

        // B. GameManager 등록 (내 캐릭터일 때만)
        if (HasStateAuthority)
        {
            if (GameManager != null)
            {
                GameManager.LocalPlayer = this;
            }
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
        _lastRCount = current.CastRCount;
        _lastMoveClickCount = current.MoveClickCount;
        _lastHasAimPointCount = current.HasAimPointCount;
    }

    void Update()
    {
        // 권한자는 매 프레임 실제 마우스/키보드 입력을 읽음
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

        // [권한자 시뮬레이션]
        if (HasStateAuthority)
        {
            var state = NetworkedInput;

            // 로컬 입력을 카운트로 변환
            if (InputHandler.inputState.castLMB) state.CastLMBCount++;
            if (InputHandler.inputState.castQ) state.CastQCount++;
            if (InputHandler.inputState.castW) state.CastWCount++;
            if (InputHandler.inputState.castE) state.CastECount++;
            if (InputHandler.inputState.castR) state.CastRCount++;
            if (InputHandler.inputState.moveClick) state.MoveClickCount++;
            if (InputHandler.inputState.hasAimPoint) state.HasAimPointCount++;

            state.MoveWorldPoint = InputHandler.inputState.moveWorldPoint;
            state.AimWorldPoint = InputHandler.inputState.aimWorldPoint;

            NetworkedInput = state;

            // 권한자 로직 실행 및 트리거 초기화
            InputHandler.DispatchStateToControllers();
            InputHandler.ClearOneFrameTriggers();
        }
    }

    public override void Render()
    {
        // [프록시 시뮬레이션] Shared Mode에서는 Render에서 변화를 감지해야 함
        if (!HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(NetworkedInput))
                {
                    SyncNetworkInputToProxyHandler();
                    
                    // 갱신된 입력을 바탕으로 프록시 캐릭터 구동
                    InputHandler.DispatchStateToControllers();
                    
                    // 실행 후 프록시 트리거도 즉시 초기화 (중요)
                    InputHandler.ClearOneFrameTriggers();
                }
            }
        }
    }

    private void SyncNetworkInputToProxyHandler()
    {
        var current = NetworkedInput;

        InputHandler.inputState.castLMB = (current.CastLMBCount != _lastLMBCount);
        InputHandler.inputState.castQ = (current.CastQCount != _lastQCount);
        InputHandler.inputState.castW = (current.CastWCount != _lastWCount);
        InputHandler.inputState.castE = (current.CastECount != _lastECount);
        InputHandler.inputState.castR = (current.CastRCount != _lastRCount);
        InputHandler.inputState.moveClick = (current.MoveClickCount != _lastMoveClickCount);
        InputHandler.inputState.hasAimPoint = (current.HasAimPointCount != _lastHasAimPointCount);

        InputHandler.inputState.moveWorldPoint = current.MoveWorldPoint;
        InputHandler.inputState.aimWorldPoint = current.AimWorldPoint;

        _lastLMBCount = current.CastLMBCount;
        _lastQCount = current.CastQCount;
        _lastWCount = current.CastWCount;
        _lastECount = current.CastECount;
        _lastRCount = current.CastRCount;
        _lastMoveClickCount = current.MoveClickCount;
        _lastHasAimPointCount = current.HasAimPointCount;
    }
}