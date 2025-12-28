using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Enemy.Tasks
{
    /// <summary>
    /// 敌人AI主任务
    /// 管理敌人的AI状态机和行为切换
    /// </summary>
    public class EnemyAITask : TaskEntry<EnemyBlackboard>
    {
        private Enemy25DController controller;
        
        // 子任务
        private IdleTask idleTask;
        private ChaseTask chaseTask;
        private EnemyAttackTask attackTask;
        
        private TaskEntry<EnemyBlackboard> currentSubTask;
        
        public EnemyAITask(Enemy25DController controller)
        {
            this.controller = controller;
            
            // 初始化子任务
            idleTask = new IdleTask(controller);
            chaseTask = new ChaseTask(controller);
            attackTask = new EnemyAttackTask(controller);
        }
        
        protected override void OnStart()
        {
            Debug.Log($"{controller.name} - AI任务启动");
            
            // 初始状态为空闲
            Blackboard.ChangeState(EnemyAIState.Idle);
            SwitchSubTask(idleTask);
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            // 死亡状态不处理
            if (Blackboard.IsDead)
                return;
            
            // 状态切换逻辑
            EvaluateStateTransition();
            
            // 更新当前子任务
            currentSubTask?.Update(deltaTime);
        }
        
        /// <summary>
        /// 评估状态转换
        /// </summary>
        private void EvaluateStateTransition()
        {
            EnemyAIState newState = Blackboard.CurrentState;
            
            switch (Blackboard.CurrentState)
            {
                case EnemyAIState.Idle:
                case EnemyAIState.Patrol:
                    // 发现目标 -> 追击
                    if (Blackboard.IsTargetInDetectionRange())
                    {
                        newState = EnemyAIState.Chase;
                    }
                    break;
                    
                case EnemyAIState.Chase:
                    // 进入攻击范围 -> 攻击
                    if (Blackboard.CanAttackTarget())
                    {
                        newState = EnemyAIState.Attack;
                    }
                    // 目标丢失 -> 返回空闲
                    else if (Blackboard.IsTargetLost())
                    {
                        newState = EnemyAIState.Idle;
                    }
                    break;
                    
                case EnemyAIState.Attack:
                    // 攻击完成且不在攻击范围 -> 追击
                    if (!Blackboard.IsAttacking)
                    {
                        if (Blackboard.DistanceToTarget > Blackboard.AttackRadius)
                        {
                            newState = EnemyAIState.Chase;
                        }
                        // 目标丢失 -> 返回空闲
                        else if (Blackboard.IsTargetLost())
                        {
                            newState = EnemyAIState.Idle;
                        }
                    }
                    break;
                    
                case EnemyAIState.Dead:
                    // 死亡状态不转换
                    break;
            }
            
            // 执行状态切换
            if (newState != Blackboard.CurrentState)
            {
                TransitionToState(newState);
            }
        }
        
        /// <summary>
        /// 转换到新状态
        /// </summary>
        private void TransitionToState(EnemyAIState newState)
        {
            Debug.Log($"{controller.name} - 状态切换: {Blackboard.CurrentState} -> {newState}");
            
            Blackboard.ChangeState(newState);
            
            // 切换对应的子任务
            switch (newState)
            {
                case EnemyAIState.Idle:
                case EnemyAIState.Patrol:
                    SwitchSubTask(idleTask);
                    break;
                    
                case EnemyAIState.Chase:
                    SwitchSubTask(chaseTask);
                    break;
                    
                case EnemyAIState.Attack:
                    SwitchSubTask(attackTask);
                    break;
                    
                case EnemyAIState.Dead:
                    currentSubTask?.Stop();
                    currentSubTask = null;
                    break;
            }
        }
        
        /// <summary>
        /// 切换子任务
        /// </summary>
        private void SwitchSubTask(TaskEntry<EnemyBlackboard> newTask)
        {
            if (currentSubTask == newTask)
                return;
            
            currentSubTask?.Stop();
            currentSubTask = newTask;
            
            if (currentSubTask != null)
            {
                currentSubTask.Blackboard = Blackboard;
                currentSubTask.Start();
            }
        }
        
        protected override void OnStop()
        {
            currentSubTask?.Stop();
            Debug.Log($"{controller.name} - AI任务停止");
        }
    }
}

