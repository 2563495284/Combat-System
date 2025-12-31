using UnityEngine;
using CombatSystem.Core;
using BTree;
namespace Character3C.Tasks
{
    /// <summary>
    /// 冲刺状态任务
    /// 处理角色的冲刺状态逻辑
    /// </summary>
    public class DashStateTask : TaskEntry<CharacterBlackboard25D>
    {
        private bool dashStarted = false;
        private float dashTimer = 0f;
        private float dashDuration = 0.2f;
        private float lastUpdateTime;

        protected override void BeforeEnter()
        {
            dashStarted = false;
            dashTimer = 0f;
            lastUpdateTime = Time.time;
        }

        protected override int Enter()
        {
            Debug.Log("进入冲刺状态");
            return TaskStatus.RUNNING;
        }

        protected override int Execute()
        {
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;

            // 开始冲刺
            if (!dashStarted && Blackboard.IsDashing)
            {
                StartDash();
            }

            // 更新冲刺计时
            if (dashStarted)
            {
                dashTimer += deltaTime;

                // 冲刺过程中的特效更新
                UpdateDashEffect(deltaTime);

                // 检查冲刺是否结束
                if (!Blackboard.IsDashing || dashTimer >= dashDuration)
                {
                    EndDash();
                    return TaskStatus.SUCCESS;
                }
            }

            return TaskStatus.RUNNING;
        }

        /// <summary>
        /// 开始冲刺
        /// </summary>
        private void StartDash()
        {
            dashStarted = true;
            dashTimer = 0f;

            // 播放冲刺音效
            // AudioManager.Instance?.PlaySound("Dash");

            // 创建冲刺残影效果
            CreateDashTrail();

            // 设置角色无敌状态（如果需要）
            Blackboard.Set("IsInvincible", true);

            // 触发轻微相机震动
            Blackboard.Set("TriggerCameraShake", true);
            Blackboard.Set("ShakeIntensity", 0.1f);
            Blackboard.Set("ShakeDuration", 0.15f);

            Debug.Log($"开始冲刺 - 方向: {Blackboard.DashDirection}");
        }

        /// <summary>
        /// 更新冲刺特效
        /// </summary>
        private void UpdateDashEffect(float deltaTime)
        {
            // 更新残影效果
            float trailInterval = 0.05f; // 每0.05秒创建一个残影
            float lastTrailTime = Blackboard.Get<float>("LastDashTrailTime", 0f);

            if (Time.time - lastTrailTime > trailInterval)
            {
                CreateDashTrail();
                Blackboard.Set("LastDashTrailTime", Time.time);
            }

            // 更新角色透明度（可选）
            // float alpha = 0.7f; // 冲刺时半透明
            // 这需要 CharacterAnimator 支持设置透明度
        }

        /// <summary>
        /// 创建冲刺残影
        /// </summary>
        private void CreateDashTrail()
        {
            if (Blackboard.Transform != null)
            {
                Vector3 position = Blackboard.Transform.position;
                // 创建残影特效
                // TrailEffectManager.Instance?.CreateTrail(position, Blackboard.Transform.rotation);
            }
        }

        /// <summary>
        /// 结束冲刺
        /// </summary>
        private void EndDash()
        {
            // 取消无敌状态
            Blackboard.Set("IsInvincible", false);

            // 恢复角色透明度
            // characterAnimator?.SetSpriteAlpha(1f);

            Debug.Log("冲刺结束");
        }

        protected override void OnEventImpl(object evt)
        {
            // 处理冲刺过程中的事件
            // 例如：冲刺攻击、冲刺取消等
        }

        protected override void Exit()
        {
            // 确保清理冲刺状态
            Blackboard.Set("IsInvincible", false);
            Debug.Log("冲刺状态结束");
        }
    }
}

