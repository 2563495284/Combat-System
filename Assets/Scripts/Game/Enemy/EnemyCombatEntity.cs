using UnityEngine;

/// <summary>
/// 敌人战斗实体
/// 实现敌人特定的战斗逻辑和行为
/// </summary>
public class EnemyCombatEntity : CombatEntity
{
    #region 敌人特定配置

    [Header("敌人特定配置")]
    [SerializeField] private bool hasAnimation = true;
    [SerializeField] private bool canMove = true;

    [Header("掉落配置")]
    [SerializeField] private int expReward = 10;
    [SerializeField] private GameObject[] dropItems;

    [Header("AI配置")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseRange = 5f;

    #endregion

    #region 生命周期重写

    protected override void InitializeComponents()
    {
        base.InitializeComponents();
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 设置敌人特定属性
        EntityType = EntityType.Monster;
        Camp = (int)CampType.Enemy;

        Debug.Log($"[EnemyCombatEntity] {EntityName} 初始化完成");
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // 敌人AI更新逻辑
        // 可以在这里实现简单的AI，或者交给专门的AI组件处理
    }

    #endregion

    #region 战斗逻辑重写

    protected override void OnDamageReceived(DamageEvent evt)
    {
        base.OnDamageReceived(evt);

        // 敌人受击反应
        AnimComp?.PlayHitAnimation();

        // 可以在这里添加受击AI逻辑
        // 例如：转向攻击者、进入警戒状态等
    }

    protected override void OnEntityDeath(DeathEvent evt)
    {
        base.OnEntityDeath(evt);

        // 敌人死亡特殊处理
        Debug.Log($"[EnemyCombatEntity] {EntityName} 被 {evt.killer?.EntityName} 击败");

        // 给予奖励
        GiveRewards(evt.killer);

        // 延迟销毁
        Destroy(gameObject, 3f);
    }

    #endregion

    #region 技能相关重写

    protected override bool CanCastSkill(StateCfg skillCfg)
    {
        if (!base.CanCastSkill(skillCfg))
            return false;

        // 敌人特定的技能施放条件检查
        // 例如：检查目标距离、技能CD等

        return true;
    }

    protected override void OnSkillCast(State skillState, CombatEntity target)
    {
        base.OnSkillCast(skillState, target);

        Debug.Log($"[EnemyCombatEntity] {EntityName} 施放技能: {skillState.Cfg.name}");
    }

    #endregion

    #region 敌人特定方法

    /// <summary>
    /// 给予奖励
    /// </summary>
    private void GiveRewards(CombatEntity killer)
    {
        if (killer == null) return;

        // 给予经验值
        Debug.Log($"[EnemyCombatEntity] 奖励 {expReward} 经验值");

        // 掉落物品
        DropItems();
    }

    /// <summary>
    /// 掉落物品
    /// </summary>
    private void DropItems()
    {
        if (dropItems == null || dropItems.Length == 0)
            return;

        foreach (var item in dropItems)
        {
            if (item != null && Random.value < 0.5f) // 50%掉落率
            {
                Instantiate(item, transform.position, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// 获取攻击范围
    /// </summary>
    public float GetAttackRange()
    {
        return attackRange;
    }

    /// <summary>
    /// 获取追击范围
    /// </summary>
    public float GetChaseRange()
    {
        return chaseRange;
    }

    /// <summary>
    /// 检查是否在攻击范围内
    /// </summary>
    public bool IsInAttackRange(CombatEntity target)
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        return distance <= attackRange;
    }

    /// <summary>
    /// 检查是否在追击范围内
    /// </summary>
    public bool IsInChaseRange(CombatEntity target)
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        return distance <= chaseRange;
    }

    #endregion
}
