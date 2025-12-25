namespace CombatSystem
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public enum EntityType
    {
        Player,     // 玩家
        Monster,    // 怪物
        NPC,        // NPC
        Boss,       // Boss
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical,   // 物理
        Magic,      // 魔法
        True,       // 真实伤害
    }

    /// <summary>
    /// 阵营类型
    /// </summary>
    public enum CampType
    {
        Friendly = 0,   // 友方
        Enemy = 1,      // 敌方
        Neutral = 2,    // 中立
    }
}

