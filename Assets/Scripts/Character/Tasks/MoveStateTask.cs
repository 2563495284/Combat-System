using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Tasks
{
    /// <summary>
    /// 移动状态任务 (2.5D)
    /// 处理角色的移动状态逻辑
    /// </summary>
    public class MoveStateTask : TaskEntry<CharacterBlackboard25D>
    {
        private float minMoveThreshold = 0.1f;

        protected override void OnStart()
        {
            Debug.Log("进入移动状态");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 检查是否有移动输入（2.5D 需要检查 X 和 Y 两个方向）
            bool isMoving = Blackboard.InputMove.sqrMagnitude > minMoveThreshold * minMoveThreshold;

            if (!isMoving)
            {
                // 没有移动输入，切换到待机状态
                Complete();
                return;
            }

            // 移动逻辑由 Character25DController 处理
            // 这里可以添加移动相关的特效、音效等

            // 示例：每隔一段时间播放脚步声
            UpdateFootstepSounds(deltaTime);
        }

        /// <summary>
        /// 更新脚步声
        /// </summary>
        private void UpdateFootstepSounds(float deltaTime)
        {
            if (!Blackboard.IsGrounded)
                return;

            float lastFootstepTime = Blackboard.Get<float>("LastFootstepTime", 0f);
            float footstepInterval = 0.3f; // 每0.3秒一次脚步声

            if (Time.time - lastFootstepTime > footstepInterval)
            {
                Blackboard.Set("LastFootstepTime", Time.time);
                // 这里可以播放脚步声音效
                // AudioManager.Instance?.PlaySound("Footstep");
            }
        }

        protected override void OnComplete()
        {
            Debug.Log("移动状态完成");
        }

        protected override void OnStop()
        {
            Debug.Log("移动状态停止");
        }
    }
}

