using UnityEngine;

public enum AttackType
{
    Melee,
    Bow,
    Staff
}

[CreateAssetMenu(fileName = "NewAttackData", menuName = "Combat/AttackData", order = 0)]
public class AttackData : ScriptableObject
{
    public AttackType attackType = AttackType.Melee;
    public float damage = 10f;
    public float range = 2f;
    public float radius = 1.5f;
    public float attackDelay = 0.2f;
    public float cooldown = 0.5f;
    public LayerMask hitLayers;
}
