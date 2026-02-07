using UnityEngine;

public struct DamageInfo
{
    public GameObject attacker;
    public int amount;
    public Vector3 hitPoint;
    public Vector3 hitDir;
    public SkillDefinition skill;
}