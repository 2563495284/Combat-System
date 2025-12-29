using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Tasks
{
    /// <summary>
    /// 玩家控制任务 (2.5D)
    /// 基于 TaskEntry 框架的玩家控制逻辑
    /// 负责管理移动、攻击、冲刺等状态任务
    /// </summary>
    public class PlayerControlTask : TaskEntry<CharacterBlackboard25D>
    {
        private Character25DController character;
        private TaskEntry<CharacterBlackboard25D> currentStateTask;

        public PlayerControlTask(Character25DController character)
        {
            this.character = character;
        }

        protected override void OnStart()
        {
            Debug.Log("玩家控制任务启动");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 更新当前状态任务
            if (currentStateTask != null)
            {
                currentStateTask.Update(deltaTime);
                
                // 如果状态任务完成，清除它
                if (currentStateTask.IsCompleted)
                {
                    currentStateTask = null;
                }
            }

            // 检查是否可以切换状态
            if (currentStateTask == null || currentStateTask.IsCompleted)
            {
                // 优先级：攻击 > 冲刺 > 移动
                
                // 检查攻击输入
                if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
                {
                    StartAttackTask();
                    return;
                }

                // 检查冲刺输入
                if (Blackboard.InputDash && Blackboard.CanDash && !Blackboard.IsDashing)
                {
                    StartDashTask();
                    return;
                }

                // 检查移动输入
                if (Blackboard.CanMove && Blackboard.InputMove.sqrMagnitude > 0.01f)
                {
                    StartMoveTask();
                    return;
                }
            }
            else
            {
                // 如果当前有状态任务，检查是否可以中断
                // 攻击和冲刺不能被移动中断，但可以被更高优先级的状态中断
                if (currentStateTask is MoveStateTask)
                {
                    // 移动可以被攻击或冲刺中断
                    if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
                    {
                        StartAttackTask();
                        return;
                    }
                    
                    if (Blackboard.InputDash && Blackboard.CanDash && !Blackboard.IsDashing)
                    {
                        StartDashTask();
                        return;
                    }
                }
            }

            // 更新攻击连击
            UpdateAttackCombo(deltaTime);
        }

        /// <summary>
        /// 开始移动任务
        /// </summary>
        private void StartMoveTask()
        {
            if (currentStateTask is MoveStateTask)
                return; // 已经在移动状态

            // 停止当前任务
            currentStateTask?.Stop();
            
            // 创建并设置移动任务
            currentStateTask = new MoveStateTask();
            currentStateTask.Blackboard = Blackboard;
            currentStateTask.Start();
        }

        /// <summary>
        /// 开始攻击任务
        /// </summary>
        private void StartAttackTask()
        {
            // 停止当前任务
            currentStateTask?.Stop();
            
            // 创建并设置攻击任务
            currentStateTask = new AttackStateTask(character.GetComponent<CombatEntity>());
            currentStateTask.Blackboard = Blackboard;
            currentStateTask.Start();
        }

        /// <summary>
        /// 开始冲刺任务
        /// </summary>
        private void StartDashTask()
        {
            // 停止当前任务
            currentStateTask?.Stop();
            
            // 创建并设置冲刺任务
            currentStateTask = new DashStateTask();
            currentStateTask.Blackboard = Blackboard;
            currentStateTask.Start();
        }

        /// <summary>
        /// 更新攻击连击
        /// </summary>
        private void UpdateAttackCombo(float deltaTime)
        {
            if (!Blackboard.IsAttacking)
                return;

            // 检查攻击动画是否完成
            var animator = character.GetComponent<CharacterAnimator>();
            if (animator != null)
            {
                var stateInfo = animator.GetCurrentStateInfo(0);

                // 假设攻击动画在 "Attack" 层
                if (stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 0.95f)
                {
                    EndAttack();
                }
            }
            else
            {
                // 如果没有动画器，使用简单的计时器
                if (Time.time - Blackboard.LastAttackTime > 0.5f)
                {
                    EndAttack();
                }
            }
        }

        /// <summary>
        /// 结束攻击
        /// </summary>
        private void EndAttack()
        {
            Blackboard.IsAttacking = false;
            Blackboard.CanMove = true;

            // 重置连击索引
            if (Time.time - Blackboard.LastAttackTime > 1f)
            {
                Blackboard.ComboIndex = 0;
            }
            else
            {
                // 增加连击索引（最多3连击）
                Blackboard.ComboIndex = (Blackboard.ComboIndex + 1) % 3;
            }

            Debug.Log($"攻击结束 - 下一连击索引: {Blackboard.ComboIndex}");
        }

        protected override void HandleEvent(object evt)
        {
            // 处理游戏事件
            // 例如：角色受伤、拾取物品等
        }

        protected override void OnStop()
        {
            // 停止当前状态任务
            currentStateTask?.Stop();
            currentStateTask = null;
            
            Debug.Log("玩家控制任务停止");
        }
    }
}

