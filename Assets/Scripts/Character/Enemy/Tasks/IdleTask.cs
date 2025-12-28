using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Enemy.Tasks
{
    /// <summary>
    /// 敌人空闲/巡逻任务
    /// 在出生点附近随机巡逻或等待
    /// </summary>
    public class IdleTask : TaskEntry<EnemyBlackboard>
    {
        private Enemy25DController controller;
        
        private bool isWaiting = true;
        private float waitTimer = 0f;
        
        public IdleTask(Enemy25DController controller)
        {
            this.controller = controller;
        }
        
        protected override void OnStart()
        {
            Debug.Log($"{controller.name} - 进入空闲状态");
            
            // 清除目标
            Blackboard.Target = null;
            
            // 停止移动
            Blackboard.MoveDirection = Vector3.zero;
            
            // 开始等待
            isWaiting = true;
            waitTimer = Blackboard.PatrolWaitTime;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (isWaiting)
            {
                // 等待状态
                waitTimer -= deltaTime;
                
                if (waitTimer <= 0)
                {
                    // 等待结束，选择新的巡逻点
                    ChooseNewPatrolPoint();
                    isWaiting = false;
                }
            }
            else
            {
                // 巡逻状态
                MoveToPatrolPoint(deltaTime);
            }
        }
        
        /// <summary>
        /// 选择新的巡逻点
        /// </summary>
        private void ChooseNewPatrolPoint()
        {
            // 在出生点附近随机选择一个点
            Vector2 randomCircle = Random.insideUnitCircle * Blackboard.PatrolRadius;
            Vector3 targetPos = Blackboard.SpawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            Blackboard.PatrolTarget = targetPos;
            
            Debug.Log($"{controller.name} - 选择巡逻点: {targetPos}");
        }
        
        /// <summary>
        /// 移动到巡逻点
        /// </summary>
        private void MoveToPatrolPoint(float deltaTime)
        {
            Vector3 currentPos = Blackboard.Position;
            Vector3 targetPos = Blackboard.PatrolTarget;
            
            // 计算水平方向
            Vector3 direction = targetPos - currentPos;
            direction.y = 0;
            
            float distance = direction.magnitude;
            
            // 到达巡逻点
            if (distance < 0.5f)
            {
                Blackboard.MoveDirection = Vector3.zero;
                isWaiting = true;
                waitTimer = Blackboard.PatrolWaitTime;
                return;
            }
            
            // 移动朝向巡逻点
            Blackboard.MoveDirection = direction.normalized;
        }
        
        protected override void OnStop()
        {
            Blackboard.MoveDirection = Vector3.zero;
            Debug.Log($"{controller.name} - 退出空闲状态");
        }
    }
}

