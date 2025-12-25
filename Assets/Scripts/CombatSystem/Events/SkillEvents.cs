using CombatSystem.Core;

namespace CombatSystem.Events
{
    /// <summary>
    /// 技能施放事件
    /// </summary>
    public class SkillCastEvent
    {
        public CombatEntity caster;     // 施法者
        public State skillState;        // 技能状态
    }

    /// <summary>
    /// 技能完成事件
    /// </summary>
    public class SkillCompleteEvent
    {
        public CombatEntity caster;     // 施法者
        public State skillState;        // 技能状态
    }

    /// <summary>
    /// 技能打断事件
    /// </summary>
    public class SkillInterruptEvent
    {
        public CombatEntity caster;     // 施法者
        public State skillState;        // 技能状态
        public string reason;           // 打断原因
    }

    /// <summary>
    /// 技能冷却完成事件
    /// </summary>
    public class SkillCooldownCompleteEvent
    {
        public CombatEntity entity;     // 实体
        public int skillId;             // 技能ID
    }
}

