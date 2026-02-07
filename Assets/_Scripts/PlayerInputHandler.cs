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

        // R은 차징이라
        public bool castRDown;
        public bool castRUp;

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

        UpdateAimPoint();
        ReadInputsIntoState();
        DispatchStateToControllers();
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

        // ✅ R: 누름/뗌 둘 다 기록
        if (Keyboard.current.rKey.wasPressedThisFrame)  inputState.castRDown = true;
        if (Keyboard.current.rKey.wasReleasedThisFrame) inputState.castRUp = true;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            inputState.moveClick = true;

            if (inputState.hasAimPoint)
                inputState.moveWorldPoint = inputState.aimWorldPoint;
        }
    }

    void DispatchStateToControllers()
    {
        // aimPoint는 매 프레임 SkillRunner에 전달
        if (skillRunner != null)
            skillRunner.SetAimPoint(inputState.hasAimPoint, inputState.aimWorldPoint);

        // 이동 클릭은 캐스팅 중이면 막음(기존 로직 유지)
        if (mover != null && inputState.moveClick && inputState.hasAimPoint)
        {
            if (skillRunner == null || !skillRunner.IsCasting)
                mover.MoveTo(inputState.moveWorldPoint);
        }

        if (skillRunner == null) return;

        // ✅ 차징 R은 우선 처리: Down/Up 각각 호출
        if (inputState.castRDown)
        {
            skillRunner.TryPress(SkillSlot.R);
            return; // 이 프레임엔 다른 스킬 호출 안 함(우선순위 고정)
        }
        if (inputState.castRUp)
        {
            skillRunner.TryRelease(SkillSlot.R);
            return;
        }

        // 나머지 스킬 우선순위(기존 유지)
        if (inputState.castLMB) skillRunner.TryPress(SkillSlot.LMB);
        else if (inputState.castQ) skillRunner.TryPress(SkillSlot.Q);
        else if (inputState.castW) skillRunner.TryPress(SkillSlot.W);
        else if (inputState.castE) skillRunner.TryPress(SkillSlot.E);
    }

    void ClearOneFrameTriggers()
    {
        inputState.castLMB = false;
        inputState.castQ = false;
        inputState.castW = false;
        inputState.castE = false;

        inputState.castRDown = false;
        inputState.castRUp = false;

        inputState.moveClick = false;
    }
}