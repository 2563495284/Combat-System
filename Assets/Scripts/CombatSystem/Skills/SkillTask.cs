using CombatSystem.Core;
using CombatSystem.Events;
using UnityEngine;

namespace CombatSystem.Skills
{
    /// <summary>
    /// 技能任务基类
    /// </summary>
    public abstract class SkillTask : TaskEntry
    {
        protected State State { get; private set; }
        protected CombatEntity Caster => State?.Owner;
        protected CombatEntity Target => Blackboard?.Get<CombatEntity>("Target");

        public void SetState(State state)
        {
            State = state;
        }

        protected override void OnStart()
        {
            Debug.Log($"[Skill] {State.Cfg.name} 开始施放");
        }

        protected override void OnComplete()
        {
            Debug.Log($"[Skill] {State.Cfg.name} 施放完成");

            // 技能完成后从技能组件移除
            Caster?.SkillComp.UnpublishSkill(State);
        }
    }

    /// <summary>
    /// 简单攻击技能
    /// 对目标造成伤害
    /// </summary>
    public class SimpleAttackSkill : SkillTask
    {
        private float _castTime;
        private float _currentTime;
        private bool _hasDealtDamage;

        protected override void OnStart()
        {
            base.OnStart();

            _castTime = Blackboard.Get<float>("CastTime", 0.5f);
            _currentTime = 0;
            _hasDealtDamage = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _currentTime += deltaTime;

            // 前摇时间
            if (!_hasDealtDamage && _currentTime >= _castTime * 0.5f)
            {
                DealDamage();
                _hasDealtDamage = true;
            }

            // 技能完成
            if (_currentTime >= _castTime)
            {
                Complete();
            }
        }

        private void DealDamage()
        {
            if (Target == null || !Target.IsAlive())
            {
                Debug.LogWarning("[SimpleAttackSkill] 目标无效");
                return;
            }

            float damage = Blackboard.Get<float>("Damage", 20f);
            Caster.DealDamage(Target, damage, DamageType.Physical);

            Debug.Log($"[SimpleAttackSkill] {Caster.EntityName} 对 {Target.EntityName} 造成 {damage} 点伤害");
        }
    }

    /// <summary>
    /// AOE技能
    /// 对范围内所有敌人造成伤害
    /// </summary>
    public class AoeSkill : SkillTask
    {
        private float _castTime;
        private float _currentTime;
        private bool _hasDealtDamage;

        protected override void OnStart()
        {
            base.OnStart();

            _castTime = Blackboard.Get<float>("CastTime", 1f);
            _currentTime = 0;
            _hasDealtDamage = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _currentTime += deltaTime;

            // 技能生效时间点
            if (!_hasDealtDamage && _currentTime >= _castTime * 0.7f)
            {
                DealAoeDamage();
                _hasDealtDamage = true;
            }

            // 技能完成
            if (_currentTime >= _castTime)
            {
                Complete();
            }
        }

        private void DealAoeDamage()
        {
            float damage = Blackboard.Get<float>("Damage", 30f);
            float radius = Blackboard.Get<float>("Radius", 5f);
            Vector3 center = Blackboard.Get<Vector3>("Center", Caster.transform.position);

            // 查找范围内的所有敌人
            var colliders = Physics.OverlapSphere(center, radius);
            int hitCount = 0;

            foreach (var col in colliders)
            {
                var entity = col.GetComponent<CombatEntity>();
                if (entity != null && entity != Caster && entity.IsAlive())
                {
                    // 检查阵营（简单判断，实际可能需要更复杂的逻辑）
                    if (entity.Camp != Caster.Camp)
                    {
                        Caster.DealDamage(entity, damage, DamageType.Magic);
                        hitCount++;
                    }
                }
            }

            Debug.Log($"[AoeSkill] AOE技能命中 {hitCount} 个目标");
        }
    }

    /// <summary>
    /// 治疗技能
    /// </summary>
    public class HealSkill : SkillTask
    {
        private float _castTime;
        private float _currentTime;
        private bool _hasHealed;

        protected override void OnStart()
        {
            base.OnStart();

            _castTime = Blackboard.Get<float>("CastTime", 0.8f);
            _currentTime = 0;
            _hasHealed = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _currentTime += deltaTime;

            if (!_hasHealed && _currentTime >= _castTime * 0.6f)
            {
                DoHeal();
                _hasHealed = true;
            }

            if (_currentTime >= _castTime)
            {
                Complete();
            }
        }

        private void DoHeal()
        {
            var healTarget = Target ?? Caster;
            if (healTarget == null || !healTarget.IsAlive())
                return;

            float healAmount = Blackboard.Get<float>("HealAmount", 50f);
            Caster.Heal(healTarget, healAmount);

            Debug.Log($"[HealSkill] {Caster.EntityName} 治疗 {healTarget.EntityName} {healAmount} 点生命");
        }
    }

    /// <summary>
    /// 冲刺技能
    /// 快速移动到目标位置
    /// </summary>
    public class DashSkill : SkillTask
    {
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _duration;
        private float _currentTime;

        protected override void OnStart()
        {
            base.OnStart();

            _startPos = Caster.transform.position;
            _targetPos = Blackboard.Get<Vector3>("TargetPosition", _startPos + Caster.transform.forward * 5f);
            _duration = Blackboard.Get<float>("Duration", 0.3f);
            _currentTime = 0;

            // 停止当前移动
            Caster.MoveComp.Stop();
        }

        protected override void OnUpdate(float deltaTime)
        {
            _currentTime += deltaTime;
            float progress = Mathf.Clamp01(_currentTime / _duration);

            // 插值移动
            Caster.transform.position = Vector3.Lerp(_startPos, _targetPos, progress);

            if (progress >= 1f)
            {
                Complete();
            }
        }

        protected override void OnComplete()
        {
            base.OnComplete();
            Debug.Log($"[DashSkill] 冲刺完成，到达位置: {Caster.transform.position}");
        }
    }

    /// <summary>
    /// 引导技能
    /// 需要持续施法的技能
    /// </summary>
    public class ChannelSkill : SkillTask
    {
        private float _channelTime;
        private float _tickInterval;
        private float _currentTime;
        private float _tickTimer;

        protected override void OnStart()
        {
            base.OnStart();

            _channelTime = Blackboard.Get<float>("ChannelTime", 3f);
            _tickInterval = Blackboard.Get<float>("TickInterval", 0.5f);
            _currentTime = 0;
            _tickTimer = _tickInterval;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _currentTime += deltaTime;
            _tickTimer -= deltaTime;

            // 每个tick造成伤害
            if (_tickTimer <= 0)
            {
                _tickTimer = _tickInterval;
                OnTick();
            }

            // 引导结束
            if (_currentTime >= _channelTime)
            {
                Complete();
            }
        }

        private void OnTick()
        {
            if (Target == null || !Target.IsAlive())
                return;

            float tickDamage = Blackboard.Get<float>("TickDamage", 10f);
            Caster.DealDamage(Target, tickDamage, DamageType.Magic);

            Debug.Log($"[ChannelSkill] 引导技能Tick，造成 {tickDamage} 点伤害");
        }

        protected override void HandleEvent(object evt)
        {
            // 移动会打断引导
            if (evt is MoveEvent)
            {
                Debug.Log("[ChannelSkill] 引导被打断");
                Stop();
            }
        }
    }
}

