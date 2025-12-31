
/// <summary>
/// 伤害事件
/// </summary>
public class DamageEvent
{
    public CombatEntity attacker;   // 攻击者
    public CombatEntity target;     // 目标
    public float damage;            // 伤害值
    public bool isCrit;             // 是否暴击
    public DamageType damageType;   // 伤害类型
}

/// <summary>
/// 治疗事件
/// </summary>
public class HealEvent
{
    public CombatEntity healer;     // 治疗者
    public CombatEntity target;     // 目标
    public float healAmount;        // 治疗量
}

/// <summary>
/// 死亡事件
/// </summary>
public class DeathEvent
{
    public CombatEntity entity;     // 死亡实体
    public CombatEntity killer;     // 击杀者
}

/// <summary>
/// 移动事件
/// </summary>
public class MoveEvent
{
    public CombatEntity entity;     // 移动实体
    public UnityEngine.Vector3 direction; // 移动方向
}

