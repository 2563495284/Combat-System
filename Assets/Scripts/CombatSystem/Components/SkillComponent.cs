using System.Collections.Generic;
using CombatSystem.Core;

namespace CombatSystem.Components
{
    /// <summary>
    /// 技能组件
    /// 管理正在施放的技能状态
    /// </summary>
    public class SkillComponent
    {
        /// <summary>
        /// 当前执行的所有技能（包含被动）
        /// </summary>
        public readonly List<State> castingSkills = new List<State>();

        /// <summary>
        /// 当前执行的主动技能（前台技能）
        /// </summary>
        public State fgCastingSkill;

        /// <summary>
        /// 组件拥有者
        /// </summary>
        public CombatEntity Owner { get; private set; }

        public SkillComponent(CombatEntity owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 发布技能状态
        /// 当技能状态启动时调用，将其添加到技能组件
        /// </summary>
        public void PublishSkill(State state)
        {
            if (state == null || !state.Cfg.isActiveSkill && !state.Cfg.isPassiveSkill)
                return;

            if (!castingSkills.Contains(state))
            {
                castingSkills.Add(state);
            }

            // 如果是主动技能，设置为前台技能
            if (state.Cfg.isActiveSkill)
            {
                fgCastingSkill = state;
            }
        }

        /// <summary>
        /// 取消发布技能状态
        /// 当技能状态结束时调用
        /// </summary>
        public void UnpublishSkill(State state)
        {
            if (state == null)
                return;

            castingSkills.Remove(state);

            if (fgCastingSkill == state)
            {
                fgCastingSkill = null;
            }
        }

        /// <summary>
        /// 是否正在施放主动技能
        /// </summary>
        public bool IsCastingActiveSkill()
        {
            return fgCastingSkill != null && fgCastingSkill.Active;
        }

        /// <summary>
        /// 是否正在施放指定技能
        /// </summary>
        public bool IsCastingSkill(int skillId)
        {
            return castingSkills.Exists(s => s.Cfg.cid == skillId && s.Active);
        }

        /// <summary>
        /// 获取正在施放的技能
        /// </summary>
        public State GetCastingSkill(int skillId)
        {
            return castingSkills.Find(s => s.Cfg.cid == skillId && s.Active);
        }

        /// <summary>
        /// 中断当前主动技能
        /// </summary>
        public void InterruptActiveSkill()
        {
            if (fgCastingSkill != null)
            {
                fgCastingSkill.Stop();
                UnpublishSkill(fgCastingSkill);
            }
        }

        /// <summary>
        /// 清除所有技能
        /// </summary>
        public void Clear()
        {
            var skills = new List<State>(castingSkills);
            foreach (var skill in skills)
            {
                skill.Stop();
            }
            castingSkills.Clear();
            fgCastingSkill = null;
        }
    }
}

