using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Tasks
{
    /// <summary>
    /// 玩家控制任务
    /// 基于 TaskEntry 框架的玩家控制逻辑
    /// </summary>
    public class PlayerControlTask : TaskEntry<CharacterBlackboard>
    {
        private CharacterController2D character;

        public PlayerControlTask(CharacterController2D character)
        {
            this.character = character;
        }

        protected override void OnStart()
        {
            Debug.Log("玩家控制任务启动");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 任务会通过 InputController 和 CharacterController2D 自动处理输入
            // 这里可以添加额外的玩家控制逻辑

            // 例如：检查攻击输入并触发攻击
            if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
            {
                StartAttack();
            }

            // 更新攻击连击
            UpdateAttackCombo(deltaTime);
        }

        /// <summary>
        /// 开始攻击
        /// </summary>
        private void StartAttack()
        {
            Blackboard.IsAttacking = true;
            Blackboard.LastAttackTime = Time.time;
            Blackboard.CanMove = false;

            // 获取动画器组件并触发攻击动画
            var animator = character.GetComponent<CharacterAnimator>();
            animator?.TriggerAttack(Blackboard.ComboIndex);

            Debug.Log($"开始攻击 - 连击索引: {Blackboard.ComboIndex}");
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
            Debug.Log("玩家控制任务停止");
        }
    }
}

