using CombatSystem.Attributes;
using CombatSystem.Buffs;
using CombatSystem.Configuration;
using CombatSystem.Core;
using CombatSystem.Skills;
using UnityEngine;

namespace CombatSystem.Utilities
{
    /// <summary>
    /// 战斗系统辅助工具类
    /// </summary>
    public static class CombatHelper
    {
        /// <summary>
        /// 创建战斗实体
        /// </summary>
        public static CombatEntity CreateEntity(string name, EntityType entityType, int camp = 0)
        {
            GameObject go = new GameObject(name);
            var entity = go.AddComponent<CombatEntity>();

            entity.EntityName = name;
            entity.EntityType = entityType;
            entity.Camp = camp;

            // 添加碰撞体（用于AOE检测等）
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Debug.Log($"[CombatHelper] 创建实体: {name}");
            return entity;
        }

        /// <summary>
        /// 应用技能到目标
        /// </summary>
        public static State ApplySkill(CombatEntity caster, CombatEntity target, int skillId, float damage = 20f)
        {
            var cfg = StateCfgManager.Instance.GetConfig(skillId);
            if (cfg == null)
            {
                Debug.LogError($"[CombatHelper] 技能配置不存在: {skillId}");
                return null;
            }

            var state = caster.CastSkill(cfg, target);
            if (state != null)
            {
                // 设置黑板参数
                state.Blackboard.Set("Damage", damage);
                state.Blackboard.Set("CastTime", cfg.duration / 1000f);
            }

            return state;
        }

        /// <summary>
        /// 应用Buff到目标
        /// </summary>
        public static State ApplyBuff(CombatEntity caster, CombatEntity target, int buffId, params object[] parameters)
        {
            var cfg = StateCfgManager.Instance.GetConfig(buffId);
            if (cfg == null)
            {
                Debug.LogError($"[CombatHelper] Buff配置不存在: {buffId}");
                return null;
            }

            var state = target.AddBuff(cfg, caster);
            if (state != null)
            {
                // 根据不同Buff类型设置参数
                SetBuffParameters(state, buffId, parameters);
            }

            return state;
        }

        /// <summary>
        /// 设置Buff参数
        /// </summary>
        private static void SetBuffParameters(State state, int buffId, params object[] parameters)
        {
            switch (buffId)
            {
                case 2001: // 攻击力提升
                    state.Blackboard.Set("AttrType", AttrType.Attack);
                    state.Blackboard.Set("Value", parameters.Length > 0 ? (float)parameters[0] : 10f);
                    state.Blackboard.Set("IsPercent", parameters.Length > 1 ? (bool)parameters[1] : false);
                    break;

                case 2002: // 中毒（持续伤害）
                    state.Blackboard.Set("TickInterval", parameters.Length > 0 ? (float)parameters[0] : 1f);
                    state.Blackboard.Set("TickDamage", parameters.Length > 1 ? (float)parameters[1] : 5f);
                    break;

                case 2003: // 回春术（持续治疗）
                    state.Blackboard.Set("TickInterval", parameters.Length > 0 ? (float)parameters[0] : 1f);
                    state.Blackboard.Set("TickHeal", parameters.Length > 1 ? (float)parameters[1] : 10f);
                    break;

                case 2005: // 护盾
                    state.Blackboard.Set("ShieldValue", parameters.Length > 0 ? (float)parameters[0] : 50f);
                    break;
            }

            // 将State设置到任务
            if (state.Task is BuffTask buffTask)
            {
                buffTask.SetState(state);
            }
            else if (state.Task is SkillTask skillTask)
            {
                skillTask.SetState(state);
            }
        }

        /// <summary>
        /// 计算两个实体之间的距离
        /// </summary>
        public static float Distance(CombatEntity a, CombatEntity b)
        {
            if (a == null || b == null)
                return float.MaxValue;

            return Vector3.Distance(a.transform.position, b.transform.position);
        }

        /// <summary>
        /// 检查是否在攻击范围内
        /// </summary>
        public static bool IsInAttackRange(CombatEntity attacker, CombatEntity target, float range = 2f)
        {
            return Distance(attacker, target) <= range;
        }

        /// <summary>
        /// 查找最近的敌人
        /// </summary>
        public static CombatEntity FindNearestEnemy(CombatEntity entity, float maxRange = 10f)
        {
            var colliders = Physics.OverlapSphere(entity.transform.position, maxRange);
            CombatEntity nearest = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponent<CombatEntity>();
                if (target != null && target != entity && target.IsAlive() && target.Camp != entity.Camp)
                {
                    float dist = Distance(entity, target);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = target;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// 查找范围内的所有敌人
        /// </summary>
        public static CombatEntity[] FindEnemiesInRange(CombatEntity entity, float range)
        {
            var colliders = Physics.OverlapSphere(entity.transform.position, range);
            var enemies = new System.Collections.Generic.List<CombatEntity>();

            foreach (var col in colliders)
            {
                var target = col.GetComponent<CombatEntity>();
                if (target != null && target != entity && target.IsAlive() && target.Camp != entity.Camp)
                {
                    enemies.Add(target);
                }
            }

            return enemies.ToArray();
        }

        /// <summary>
        /// 设置实体属性
        /// </summary>
        public static void SetEntityAttrs(CombatEntity entity, float maxHp, float attack, float defense, float moveSpeed = 5f)
        {
            entity.AttrComp.SetBaseAttr(AttrType.MaxHp, maxHp);
            entity.AttrComp.SetBaseAttr(AttrType.Hp, maxHp);
            entity.AttrComp.SetBaseAttr(AttrType.Attack, attack);
            entity.AttrComp.SetBaseAttr(AttrType.Defense, defense);
            entity.AttrComp.SetBaseAttr(AttrType.MoveSpeed, moveSpeed);
        }
    }
}

