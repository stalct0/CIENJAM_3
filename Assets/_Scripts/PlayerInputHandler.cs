using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [System.Serializable]
    public struct PlayerInputState
    {
        public bool castLMB;
        public bool castQ;
        public bool castW;
        public bool castE;
        public bool castR;

        public bool moveClick;          
        public Vector3 moveWorldPoint;  

        public bool hasAimPoint;
        public Vector3 aimWorldPoint;
    }

    [Header("State (read-only at runtime)")]
    public PlayerInputState inputState;

    [Header("Refs")]
    public Camera cam;
    public LayerMask groundMask;

    public PlayerMovement mover;
    public SkillRunner skillRunner;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (mover == null) mover = GetComponent<PlayerMovement>();
        if (skillRunner == null) skillRunner = GetComponent<SkillRunner>();
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // 1) aimPoint는 "매 프레임" 갱신 (스킬이 필요로 함)
        UpdateAimPoint();

        // 2) 입력을 state에만 기록
        ReadInputsIntoState();

        // 3) state를 보고 다른 스크립트들을 구동
        DispatchStateToControllers();

        // 4) 1프레임 트리거들은 리셋 (aimPoint는 유지)
        ClearOneFrameTriggers();
    }

    void UpdateAimPoint()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            inputState.hasAimPoint = true;
            inputState.aimWorldPoint = hit.point;
        }
        else
        {
            inputState.hasAimPoint = false;
        }
    }

    void ReadInputsIntoState()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            inputState.castLMB = true;

        if (Keyboard.current.qKey.wasPressedThisFrame) inputState.castQ = true;
        if (Keyboard.current.wKey.wasPressedThisFrame) inputState.castW = true;
        if (Keyboard.current.eKey.wasPressedThisFrame) inputState.castE = true;
        if (Keyboard.current.rKey.wasPressedThisFrame) inputState.castR = true;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            inputState.moveClick = true;

            if (inputState.hasAimPoint)
                inputState.moveWorldPoint = inputState.aimWorldPoint;
        }
    }

    void DispatchStateToControllers()
    {
        if (skillRunner != null)
            skillRunner.SetAimPoint(inputState.hasAimPoint, inputState.aimWorldPoint);

        if (mover != null && inputState.moveClick && inputState.hasAimPoint)
        {
            if (skillRunner == null || !skillRunner.IsCasting)
                mover.MoveTo(inputState.moveWorldPoint);
        }

        // 스킬 우선순위
        if (skillRunner != null)
        {
            if (inputState.castLMB) skillRunner.TryCast(SkillSlot.LMB);
            else if (inputState.castQ) skillRunner.TryCast(SkillSlot.Q);
            else if (inputState.castW) skillRunner.TryCast(SkillSlot.W);
            else if (inputState.castE) skillRunner.TryCast(SkillSlot.E);
            else if (inputState.castR) skillRunner.TryCast(SkillSlot.R);
        }
    }

    void ClearOneFrameTriggers()
    {
        inputState.castLMB = false;
        inputState.castQ = false;
        inputState.castW = false;
        inputState.castE = false;
        inputState.castR = false;
        inputState.moveClick = false;
    }
}