using CombatSystem.Core;
using CombatSystem.Events;
using UnityEngine;
using BTree;
namespace CombatSystem.Skills
{
    /// <summary>
    /// 技能任务基类
    /// </summary>
    public abstract class SkillTask<T> : LeafTask<T> where T : Blackboard
    {
        protected State State { get; private set; }
        protected CombatEntity Caster => State?.Owner;
        protected CombatEntity Target => Blackboard?.Get<CombatEntity>("Target");

        private float _lastUpdateTime;

        public void SetState(State state)
        {
            State = state;
        }

        protected float DeltaTime
        {
            get
            {
                float currentTime = Time.time;
                float deltaTime = currentTime - _lastUpdateTime;
                _lastUpdateTime = currentTime;
                return deltaTime;
            }
        }

        protected sealed override int Execute()
        {
            int result = OnExecute(DeltaTime);
            return result;
        }

        protected override int Enter()
        {
            _lastUpdateTime = Time.time;
            Debug.Log($"[Skill] {State.Cfg.name} 开始施放");
            OnSkillStart();
            return TaskStatus.RUNNING;
        }

        protected override void Exit()
        {
            Debug.Log($"[Skill] {State.Cfg.name} 施放完成");
            OnSkillEnd();
            
            // 退出技能状态，返回移动状态机
            Caster?.AnimComp?.ExitSkillState();

            // 技能完成后从技能组件移除
            Caster?.SkillComp.UnpublishSkill(State);
        }

        protected override void OnEventImpl(object eventObj)
        {
            HandleSkillEvent(eventObj);
        }

        /// <summary>
        /// 技能开始时调用
        /// </summary>
        protected virtual void OnSkillStart() { }

        /// <summary>
        /// 技能每帧更新
        /// 返回任务状态：RUNNING, SUCCESS, ERROR等
        /// </summary>
        protected abstract int OnExecute(float deltaTime);

        /// <summary>
        /// 技能结束时调用
        /// </summary>
        protected virtual void OnSkillEnd() { }

        /// <summary>
        /// 处理技能事件
        /// </summary>
        protected virtual void HandleSkillEvent(object evt) { }
    }
}

