using UnityEngine;
using CombatSystem.Core;
using BTree;
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
        private int currentFrame;

        public PlayerControlTask(Character25DController character)
        {
            this.character = character;
        }

        protected override int Enter()
        {
            Debug.Log("玩家控制任务启动");
            currentFrame = 0;
            return TaskStatus.RUNNING;
        }

        protected override int Execute()
        {
            currentFrame++;

            // 更新当前状态任务
            if (currentStateTask != null)
            {
                currentStateTask.Update(currentFrame);

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
                    return TaskStatus.RUNNING;
                }

                // 检查冲刺输入
                if (Blackboard.InputDash && Blackboard.CanDash && !Blackboard.IsDashing)
                {
                    StartDashTask();
                    return TaskStatus.RUNNING;
                }

                // 检查移动输入
                if (Blackboard.CanMove && Blackboard.InputMove.sqrMagnitude > 0.01f)
                {
                    StartMoveTask();
                    return TaskStatus.RUNNING;
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
                        return TaskStatus.RUNNING;
                    }

                    if (Blackboard.InputDash && Blackboard.CanDash && !Blackboard.IsDashing)
                    {
                        StartDashTask();
                        return TaskStatus.RUNNING;
                    }
                }
            }

            // 更新攻击连击
            UpdateAttackCombo();

            return TaskStatus.RUNNING;
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
            // TaskEntry 会在第一次 Update 时自动启动
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
            // TaskEntry 会在第一次 Update 时自动启动
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
            // TaskEntry 会在第一次 Update 时自动启动
        }

        /// <summary>
        /// 更新攻击连击
        /// </summary>
        private void UpdateAttackCombo()
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

        protected override void OnEventImpl(object evt)
        {
            // 处理游戏事件
            // 例如：角色受伤、拾取物品等
            
            // 将事件传递给当前状态任务
            currentStateTask?.OnEvent(evt);
        }

        protected override void Exit()
        {
            // 停止当前状态任务
            currentStateTask?.Stop();
            currentStateTask = null;

            Debug.Log("玩家控制任务结束");
        }
    }
}

