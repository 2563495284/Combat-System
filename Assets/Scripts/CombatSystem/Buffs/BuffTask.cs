using CombatSystem.Attributes;
using CombatSystem.Core;
using CombatSystem.Events;
using UnityEngine;

namespace CombatSystem.Buffs
{
    /// <summary>
    /// Buff任务基类
    /// 用于实现各种Buff效果
    /// </summary>
    public abstract class BuffTask : TaskEntry
    {
        protected State State { get; private set; }
        protected CombatEntity Owner => State?.Owner;

        public void SetState(State state)
        {
            State = state;
        }

        protected override void OnStart()
        {
            ApplyBuff();
        }

        protected override void OnStop()
        {
            RemoveBuff();
        }

        protected abstract void ApplyBuff();
        protected abstract void RemoveBuff();
    }

    /// <summary>
    /// 属性加成Buff
    /// 增加攻击力、防御力等属性
    /// </summary>
    public class AttrBuffTask : BuffTask
    {
        private AttrModifier _modifier;

        protected override void ApplyBuff()
        {
            // 从黑板读取配置
            var attrType = Blackboard.Get<AttrType>("AttrType", AttrType.Attack);
            var value = Blackboard.Get<float>("Value", 10f);
            var isPercent = Blackboard.Get<bool>("IsPercent", false);

            _modifier = new AttrModifier(value, isPercent, State);
            Owner.AttrComp.AddModifier(attrType, _modifier);

            Debug.Log($"[AttrBuff] 应用属性加成: {attrType} +{value}{(isPercent ? "%" : "")}");
        }

        protected override void RemoveBuff()
        {
            if (_modifier != null)
            {
                var attrType = Blackboard.Get<AttrType>("AttrType", AttrType.Attack);
                Owner.AttrComp.RemoveModifier(attrType, _modifier);
                Debug.Log($"[AttrBuff] 移除属性加成");
            }
        }
    }

    /// <summary>
    /// 持续伤害Buff（DoT）
    /// </summary>
    public class DotBuffTask : BuffTask
    {
        private float _tickInterval;
        private float _tickDamage;
        private float _tickTimer;

        protected override void ApplyBuff()
        {
            _tickInterval = Blackboard.Get<float>("TickInterval", 1f);
            _tickDamage = Blackboard.Get<float>("TickDamage", 5f);
            _tickTimer = _tickInterval;

            Debug.Log($"[DotBuff] 应用持续伤害: 每{_tickInterval}秒造成{_tickDamage}点伤害");
        }

        protected override void OnUpdate(float deltaTime)
        {
            _tickTimer -= deltaTime;
            if (_tickTimer <= 0)
            {
                _tickTimer = _tickInterval;

                // 造成伤害
                var caster = State.Caster;
                if (caster != null)
                {
                    caster.DealDamage(Owner, _tickDamage, DamageType.True);
                }
            }
        }

        protected override void RemoveBuff()
        {
            Debug.Log($"[DotBuff] 移除持续伤害");
        }
    }

    /// <summary>
    /// 持续治疗Buff（HoT）
    /// </summary>
    public class HotBuffTask : BuffTask
    {
        private float _tickInterval;
        private float _tickHeal;
        private float _tickTimer;

        protected override void ApplyBuff()
        {
            _tickInterval = Blackboard.Get<float>("TickInterval", 1f);
            _tickHeal = Blackboard.Get<float>("TickHeal", 10f);
            _tickTimer = _tickInterval;

            Debug.Log($"[HotBuff] 应用持续治疗: 每{_tickInterval}秒恢复{_tickHeal}点生命");
        }

        protected override void OnUpdate(float deltaTime)
        {
            _tickTimer -= deltaTime;
            if (_tickTimer <= 0)
            {
                _tickTimer = _tickInterval;

                // 治疗
                var caster = State.Caster;
                if (caster != null)
                {
                    caster.Heal(Owner, _tickHeal);
                }
            }
        }

        protected override void RemoveBuff()
        {
            Debug.Log($"[HotBuff] 移除持续治疗");
        }
    }

    /// <summary>
    /// 眩晕Buff
    /// 禁止移动和施法
    /// </summary>
    public class StunBuffTask : BuffTask
    {
        protected override void ApplyBuff()
        {
            // 停止移动
            Owner.MoveComp.Stop();

            // 中断当前技能
            Owner.SkillComp.InterruptActiveSkill();

            Debug.Log($"[StunBuff] {Owner.EntityName} 被眩晕");
        }

        protected override void RemoveBuff()
        {
            Debug.Log($"[StunBuff] {Owner.EntityName} 眩晕解除");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 持续阻止移动
            Owner.MoveComp.Stop();
        }
    }

    /// <summary>
    /// 护盾Buff
    /// 吸收一定量的伤害
    /// </summary>
    public class ShieldBuffTask : BuffTask
    {
        private float _shieldValue;

        protected override void ApplyBuff()
        {
            _shieldValue = Blackboard.Get<float>("ShieldValue", 50f);
            Blackboard.Set("CurrentShield", _shieldValue);

            // 注册伤害事件
            Owner.EventBus.Register<DamageEvent>(OnDamage);

            Debug.Log($"[ShieldBuff] 获得 {_shieldValue} 点护盾");
        }

        protected override void RemoveBuff()
        {
            Owner.EventBus.Unregister<DamageEvent>(OnDamage);
            Debug.Log($"[ShieldBuff] 护盾消失");
        }

        private void OnDamage(DamageEvent evt)
        {
            if (evt.target != Owner)
                return;

            float currentShield = Blackboard.Get<float>("CurrentShield", 0f);
            if (currentShield > 0)
            {
                float absorb = Mathf.Min(currentShield, evt.damage);
                currentShield -= absorb;
                Blackboard.Set("CurrentShield", currentShield);

                Debug.Log($"[ShieldBuff] 护盾吸收 {absorb} 点伤害，剩余 {currentShield}");

                // 护盾破碎
                if (currentShield <= 0)
                {
                    State.Stop();
                }
            }
        }
    }
}

