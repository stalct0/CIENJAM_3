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

        public bool castRDown;
        public bool castRUp;
        
        public bool castD;
        public bool castF;
        
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
    public KnockbackController knockback;
    public SummonerSpellRunner summoner;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (mover == null) mover = GetComponent<PlayerMovement>();
        if (skillRunner == null) skillRunner = GetComponent<SkillRunner>();
        if (knockback == null) knockback = GetComponent<KnockbackController>();
        if (summoner == null) summoner = GetComponent<SummonerSpellRunner>();
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        UpdateAimPoint();
        ReadInputsIntoState();
        DispatchStateToControllers();
        ClearOneFrameTriggers();
    }

    public void UpdateAimPoint()
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

    public void ReadInputsIntoState()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            inputState.castLMB = true;

        if (Keyboard.current.qKey.wasPressedThisFrame) inputState.castQ = true;
        if (Keyboard.current.wKey.wasPressedThisFrame) inputState.castW = true;
        if (Keyboard.current.eKey.wasPressedThisFrame) inputState.castE = true;

        if (Keyboard.current.rKey.wasPressedThisFrame)  inputState.castRDown = true;
        if (Keyboard.current.rKey.wasReleasedThisFrame) inputState.castRUp = true;
        if (Keyboard.current.dKey.wasPressedThisFrame) inputState.castD = true;
        if (Keyboard.current.fKey.wasPressedThisFrame) inputState.castF = true;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            inputState.moveClick = true;
            if (inputState.hasAimPoint)
                inputState.moveWorldPoint = inputState.aimWorldPoint;
        }
    }

    public void DispatchStateToControllers()
    {
        if (skillRunner != null)
            skillRunner.SetAimPoint(inputState.hasAimPoint, inputState.aimWorldPoint);

        // 이동 클릭은 Busy면 막음
        if (mover != null && inputState.moveClick && inputState.hasAimPoint)
        {
            if (skillRunner == null || !skillRunner.IsBusy)
                mover.MoveTo(inputState.moveWorldPoint);
        }
        
        if (knockback != null && knockback.IsLocked)
        {
            // 이동 + 스킬 입력 전달 차단
            return;
        }
        if (summoner != null)
        {
            if (inputState.castD) summoner.TryCast(SummonerSlot.D);
            if (inputState.castF) summoner.TryCast(SummonerSlot.F);
        }
        
        if (skillRunner == null) return;

        // R은 Down/Up 둘 다 같은 프레임에 올 수 있음 → 둘 다 전달
        if (inputState.castRDown) skillRunner.TryPress(SkillSlot.R);
        if (inputState.castRUp)   skillRunner.TryRelease(SkillSlot.R);

        // 다른 키들은 한 프레임에 하나만
        if (inputState.castLMB) skillRunner.TryPress(SkillSlot.LMB);
        else if (inputState.castQ) skillRunner.TryPress(SkillSlot.Q);
        else if (inputState.castW) skillRunner.TryPress(SkillSlot.W);
        else if (inputState.castE) skillRunner.TryPress(SkillSlot.E);
    }

    public void ClearOneFrameTriggers()
    {
        inputState.castLMB = false;
        inputState.castQ = false;
        inputState.castW = false;
        inputState.castE = false;

        inputState.castRDown = false;
        inputState.castRUp = false;
        inputState.castD = false;
        inputState.castF = false;
        
        inputState.moveClick = false;
    }
}