using CombatSystem.Attributes;
using CombatSystem.Components;
using CombatSystem.Events;
using UnityEngine;

namespace CombatSystem.Core
{
    /// <summary>
    /// 战斗实体 - 主要实现部分
    /// 对应游戏中的角色、怪物等可战斗对象
    /// </summary>
    public partial class CombatEntity
    {
        /// <summary>
        /// 事件总线
        /// </summary>
        public CombatEventBus EventBus { get; private set; }

        /// <summary>
        /// 状态组件 - 管理技能和Buff
        /// </summary>
        public StateComponent StateComp { get; private set; }

        /// <summary>
        /// 属性组件 - 管理生命值、攻击力等属性
        /// </summary>
        public AttrComponent AttrComp { get; private set; }

        /// <summary>
        /// 技能组件 - 管理当前施放的技能
        /// </summary>
        public SkillComponent SkillComp { get; private set; }

        /// <summary>
        /// 移动组件
        /// </summary>
        public MoveComponent MoveComp { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            EventBus = new CombatEventBus();
            StateComp = new StateComponent(this);
            AttrComp = new AttrComponent(this);
            SkillComp = new SkillComponent(this);
            MoveComp = new MoveComponent(this);

            // 注册事件监听
            RegisterEventListeners();
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEventListeners()
        {
            EventBus.Register<DamageEvent>(OnDamage);
            EventBus.Register<HealEvent>(OnHeal);
            EventBus.Register<DeathEvent>(OnDeath);
        }

        private int _currentFrame = 0;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _currentFrame++;

            // 更新状态组件
            StateComp?.Update(_currentFrame);

            // 更新移动组件
            MoveComp?.Update(deltaTime);
        }

        #region 战斗相关方法

        /// <summary>
        /// 造成伤害
        /// </summary>
        public void DealDamage(CombatEntity target, float damage, DamageType damageType = DamageType.Physical)
        {
            if (target == null || !target.IsAlive())
                return;

            // 计算实际伤害
            float actualDamage = CalculateDamage(damage, damageType, target);

            // 触发伤害事件
            var damageEvent = new DamageEvent
            {
                attacker = this,
                target = target,
                damage = actualDamage,
                damageType = damageType,
                isCrit = Random.value < AttrComp.GetAttr(AttrType.CritRate) / 100f
            };

            if (damageEvent.isCrit)
            {
                actualDamage *= AttrComp.GetAttr(AttrType.CritDamage) / 100f;
            }

            target.EventBus.Fire(damageEvent);
        }

        /// <summary>
        /// 计算伤害
        /// </summary>
        private float CalculateDamage(float baseDamage, DamageType damageType, CombatEntity target)
        {
            float damage = baseDamage;

            switch (damageType)
            {
                case DamageType.Physical:
                    float attack = AttrComp.GetAttr(AttrType.Attack);
                    float defense = target.AttrComp.GetAttr(AttrType.Defense);
                    damage = Mathf.Max(1, attack + baseDamage - defense * 0.5f);
                    break;

                case DamageType.Magic:
                    float magicAttack = AttrComp.GetAttr(AttrType.MagicAttack);
                    float magicDefense = target.AttrComp.GetAttr(AttrType.MagicDefense);
                    damage = Mathf.Max(1, magicAttack + baseDamage - magicDefense * 0.5f);
                    break;

                case DamageType.True:
                    damage = baseDamage;
                    break;
            }

            return damage;
        }

        /// <summary>
        /// 受到伤害处理
        /// </summary>
        private void OnDamage(DamageEvent evt)
        {
            if (evt.target != this)
                return;

            // 扣除生命值
            AttrComp.ModifyCurrentValue(AttrType.Hp, -evt.damage);

            Debug.Log($"{EntityName} 受到 {evt.damage} 点伤害，剩余HP: {AttrComp.GetAttr(AttrType.Hp)}");

            // 检查死亡
            if (!IsAlive())
            {
                Die(evt.attacker);
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(CombatEntity target, float healAmount)
        {
            if (target == null || !target.IsAlive())
                return;

            var healEvent = new HealEvent
            {
                healer = this,
                target = target,
                healAmount = healAmount
            };

            target.EventBus.Fire(healEvent);
        }

        /// <summary>
        /// 受到治疗处理
        /// </summary>
        private void OnHeal(HealEvent evt)
        {
            if (evt.target != this)
                return;

            AttrComp.ModifyCurrentValue(AttrType.Hp, evt.healAmount);
            Debug.Log($"{EntityName} 恢复 {evt.healAmount} 点生命，当前HP: {AttrComp.GetAttr(AttrType.Hp)}");
        }

        /// <summary>
        /// 死亡
        /// </summary>
        private void Die(CombatEntity killer)
        {
            var deathEvent = new DeathEvent
            {
                entity = this,
                killer = killer
            };

            EventBus.Fire(deathEvent);
            Debug.Log($"{EntityName} 已死亡");
        }

        /// <summary>
        /// 死亡处理
        /// </summary>
        private void OnDeath(DeathEvent evt)
        {
            if (evt.entity != this)
                return;

            // 清理所有状态
            StateComp.Clear();
            SkillComp.Clear();

            // TODO: 播放死亡动画、特效等
        }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive()
        {
            return AttrComp.IsAlive();
        }

        #endregion

        #region 技能和状态相关方法

        /// <summary>
        /// 施放技能
        /// </summary>
        public State CastSkill(Configuration.StateCfg skillCfg, CombatEntity target = null)
        {
            if (skillCfg == null || !skillCfg.isActiveSkill)
            {
                Debug.LogWarning("无效的技能配置");
                return null;
            }

            // 添加技能状态
            var skillState = StateComp.AddState(skillCfg, this);
            if (skillState != null)
            {
                // 发布到技能组件
                SkillComp.PublishSkill(skillState);

                // 触发技能施放事件
                EventBus.Fire(new SkillCastEvent
                {
                    caster = this,
                    skillState = skillState
                });

                // 将目标存入黑板
                if (target != null)
                {
                    skillState.Blackboard.Set("Target", target);
                }
            }

            return skillState;
        }

        /// <summary>
        /// 添加Buff
        /// </summary>
        public State AddBuff(Configuration.StateCfg buffCfg, CombatEntity caster = null)
        {
            if (buffCfg == null || !buffCfg.isBuff)
            {
                Debug.LogWarning("无效的Buff配置");
                return null;
            }

            return StateComp.AddState(buffCfg, caster ?? this);
        }

        /// <summary>
        /// 移除Buff
        /// </summary>
        public bool RemoveBuff(int buffId)
        {
            return StateComp.RemoveStateById(buffId);
        }

        #endregion

        private void OnDestroy()
        {
            // 清理资源
            EventBus?.Clear();
            StateComp?.Clear();
            SkillComp?.Clear();
        }
    }
}

