using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimController : MonoBehaviour
{
    [Header("References (optional)")]
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Tuning")]
    [Tooltip("이 값 이상이면 Running으로 봅니다.")]
    [SerializeField] private float runningThreshold = 0.1f;

    [Tooltip("Speed 파라미터가 부드럽게 따라가게 하는 감쇠 시간(초).")]
    [SerializeField] private float speedDampTime = 0.08f;

    [Tooltip("속도를 0~1 범위로 정규화할지 여부. (나중에 Walk/Run 나눌 때 유용)")]
    [SerializeField] private bool normalizeByAgentSpeed = true;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Reset()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Update()
    {
        float speed = ReadMoveSpeed();

        // 1) threshold 적용 (Idle/Running만 쓰는 동안은 이게 깔끔)
        //    -> 0 아니어도 미세 흔들림 때문에 Running으로 들어가는 걸 막음
        if (speed < runningThreshold) speed = 0f;

        // 2) Animator 파라미터에 반영 (부드럽게)
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
    }

    private float ReadMoveSpeed()
    {
        if (!agent) return 0f;

        float rawSpeed = agent.velocity.magnitude;

        if (!normalizeByAgentSpeed) return rawSpeed;

        float max = Mathf.Max(0.01f, agent.speed);
        return Mathf.Clamp01(rawSpeed / max);
    }

    public void ForceSpeed(float normalizedSpeed01)
    {
        animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed01));
    }
}
