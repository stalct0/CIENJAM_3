using UnityEngine;

public enum SkillMoveDirection
{
    Forward,        // 캐스터의 forward
    Input,          // 현재 입력 방향
    TowardTarget,   // 타겟 방향(있을 때)
}

[CreateAssetMenu(menuName="Skills/Cast Move Config")]
public class CastMoveConfig : ScriptableObject
{
    [Header("Displacement")]
    public float distance = 2.0f;      // 총 이동 거리
    public float duration = 0.15f;     // 이동 시간(=시전 중 구간)

    [Header("Shape")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    // 0~1 구간에서 “속도 가중치”. 예: 초반 급가속, 후반 감속

    public SkillMoveDirection direction = SkillMoveDirection.Forward;

    [Header("Control")]
    public bool allowSteer = false;    // 이동 중 방향 갱신(입력 반영) 여부
    public bool blockNormalMove = true; // 이 구간 동안 평소 이동 입력을 막을지

    [Header("Collision")]
    public bool stopOnHit = true;      // 벽/장애물에 닿으면 멈춤
}