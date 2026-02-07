using UnityEngine;

public class AnimEventsForwarder : MonoBehaviour
{
    [SerializeField] private SkillRunner runner;

    private void Awake()
    {
        if (!runner) runner = GetComponentInParent<SkillRunner>();
    }

    // Animation Event 함수명으로 이걸 지정
    public void Hit()
    {
        runner?.AnimEvent_Hit();
    }

    public void End()
    {
        runner?.AnimEvent_End();
    }
    
    public void StartCast()
    {
        runner?.AnimEvent_Start();
    }

    public void Move()
    {
        runner?.AnimEvent_Move();
    }
    
    public void GuardStart()
    {
        runner?.AnimEvent_GuardStart();
    }
    
    public void GuardEnd()
    {
        runner?.AnimEvent_GuardEnd();
    }
    
}