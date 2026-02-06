using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
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

        // 우클릭
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
