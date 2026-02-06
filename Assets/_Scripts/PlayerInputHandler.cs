using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerInputHandler : MonoBehaviour
{
    public struct _inputState
    {
        public bool Q;
        public bool W;
        public bool E;
        public bool R;
        public bool LMB;
        public bool MovePosition;
    }
    
    public _inputState inputState;
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
        if (Mouse.current == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            inputState.LMB = true;
            skillRunner.TryCast(SkillSlot.LMB);
            inputState.LMB = false;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            inputState.Q = true;
            skillRunner.TryCast(SkillSlot.Q);
            inputState.Q = false;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            inputState.W = true;
            skillRunner.TryCast(SkillSlot.W);
            inputState.W = false;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            inputState.E = true;
            skillRunner.TryCast(SkillSlot.E);
            inputState.E = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            inputState.R = true;
            skillRunner.TryCast(SkillSlot.R);
            inputState.R = false;
        }
        

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
        {
            mover.MoveTo(hit.point);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            skillRunner.TryCast(SkillSlot.Q);
        }
    }
}
