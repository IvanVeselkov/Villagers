using UnityEngine;
using Mirror;
using System.Collections;

public class CombatBase : NetworkBehaviour
{
    [Header("Combat")]
    public AttackData currentAttack;
    public Transform attackOrigin;
    
    [Header("Network")]
    public float serverCooldown = 0f;
    private float lastAttackTime = -999f;
    
    void Awake()
    {
        if (attackOrigin == null)
            attackOrigin = transform;
    }
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }
    
    [Command]
    public void CmdAttack()
    {
        if (!isServer) return;
        if (currentAttack == null) return;
        if (Time.time < lastAttackTime + currentAttack.cooldown) return;
        
        lastAttackTime = Time.time;
        
        switch (currentAttack.attackType)
        {
            case AttackType.Melee:
                DoMeleeAttack();
                break;
            case AttackType.Bow:
                DoRaycastAttack();
                break;
            case AttackType.Staff:
                DoRaycastAttack();
                break;
        }
    }
    
    [Server]
    void DoMeleeAttack()
    {
        if (currentAttack == null || attackOrigin == null) return;
        
        Vector3 origin = attackOrigin.position + attackOrigin.forward * currentAttack.range * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin, currentAttack.radius, currentAttack.hitLayers);
        
        foreach (Collider hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                damageable = hit.GetComponent<IDamageable>();
            
            if (damageable != null && damageable.IsAlive())
            {
                damageable.DealDamage(currentAttack.damage, gameObject);
            }
        }
    }
    
    [Server]
    void DoRaycastAttack()
    {
        if (currentAttack == null || attackOrigin == null) return;
        
        Ray ray = new Ray(attackOrigin.position, attackOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentAttack.range, currentAttack.hitLayers))
        {
            if (hit.transform.root == transform.root) return;
            
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                damageable = hit.collider.GetComponent<IDamageable>();
            
            if (damageable != null && damageable.IsAlive())
            {
                damageable.DealDamage(currentAttack.damage, gameObject);
            }
        }
    }
    
    public void RequestAttack()
    {
        if (!isLocalPlayer) return;
        CmdAttack();
    }
}
