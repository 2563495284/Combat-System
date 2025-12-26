using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Tasks
{
    /// <summary>
    /// 相机跟随任务
    /// 基于 TaskEntry 的相机控制逻辑
    /// </summary>
    public class CameraFollowTask : TaskEntry
    {
        private CameraController cameraController;
        private Transform target;

        public CameraFollowTask(CameraController camera, Transform target)
        {
            this.cameraController = camera;
            this.target = target;
        }

        protected override void OnStart()
        {
            if (cameraController != null && target != null)
            {
                cameraController.SetTarget(target);
                Debug.Log($"相机跟随任务启动 - 目标: {target.name}");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            // CameraController 会自动处理跟随逻辑
            // 这里可以添加特殊的相机行为

            // 示例：根据黑板数据触发相机效果
            if (Blackboard != null)
            {
                // 检查是否需要触发震动
                if (Blackboard.TryGet<bool>("TriggerCameraShake", out bool shouldShake) && shouldShake)
                {
                    float intensity = Blackboard.Get<float>("ShakeIntensity", 0.2f);
                    float duration = Blackboard.Get<float>("ShakeDuration", 0.3f);

                    cameraController?.Shake(intensity, duration);

                    // 清除标记
                    Blackboard.Set("TriggerCameraShake", false);
                }
            }
        }

        /// <summary>
        /// 切换跟随目标
        /// </summary>
        public void ChangeTarget(Transform newTarget)
        {
            target = newTarget;
            cameraController?.SetTarget(newTarget);
        }

        /// <summary>
        /// 触发相机震动
        /// </summary>
        public void TriggerShake(float intensity = 0.2f, float duration = 0.3f)
        {
            cameraController?.Shake(intensity, duration);
        }

        /// <summary>
        /// 设置相机边界
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            cameraController?.SetBounds(min, max);
        }

        protected override void OnStop()
        {
            Debug.Log("相机跟随任务停止");
        }
    }
}

