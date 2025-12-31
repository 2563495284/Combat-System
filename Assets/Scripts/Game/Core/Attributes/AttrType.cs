
/// <summary>
/// 属性类型
/// </summary>
public enum AttrType
{
    // 基础属性
    MaxHp = 1,          // 最大生命值
    Hp = 2,             // 当前生命值
    MaxMp = 3,          // 最大魔法值
    Mp = 4,             // 当前魔法值

    // 攻击属性
    Attack = 10,        // 攻击力
    Defense = 11,       // 防御力
    MagicAttack = 12,   // 魔法攻击
    MagicDefense = 13,  // 魔法防御

    // 战斗属性
    CritRate = 20,      // 暴击率（百分比）
    CritDamage = 21,    // 暴击伤害（百分比）
    DodgeRate = 22,     // 闪避率（百分比）
    HitRate = 23,       // 命中率（百分比）

    // 移动属性
    MoveSpeed = 30,     // 移动速度
    AttackSpeed = 31,   // 攻击速度

    // 抗性
    PhysicalRes = 40,   // 物理抗性（百分比）
    MagicRes = 41,      // 魔法抗性（百分比）
}


