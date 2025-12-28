using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Enemy.Tasks
{
    /// <summary>
    /// 敌人追击任务
    /// 追逐目标直到进入攻击范围或丢失目标
    /// </summary>
    public class ChaseTask : TaskEntry<EnemyBlackboard>
    {
        private Enemy25DController controller;
        
        public ChaseTask(Enemy25DController controller)
        {
            this.controller = controller;
        }
        
        protected override void OnStart()
        {
            Debug.Log($"{controller.name} - 进入追击状态");
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (Blackboard.Target == null)
            {
                Blackboard.MoveDirection = Vector3.zero;
                return;
            }
            
            // 计算朝向目标的方向
            Vector3 currentPos = Blackboard.Position;
            Vector3 targetPos = Blackboard.TargetPosition;
            
            Vector3 direction = targetPos - currentPos;
            direction.y = 0; // 只在水平面移动
            
            float distance = direction.magnitude;
            
            // 如果已经很接近目标，减速
            if (distance < Blackboard.AttackRadius * 1.2f)
            {
                // 在攻击范围边缘减速
                Blackboard.MoveDirection = direction.normalized * 0.5f;
            }
            else
            {
                // 全速追击
                Blackboard.MoveDirection = direction.normalized;
            }
        }
        
        protected override void OnStop()
        {
            Blackboard.MoveDirection = Vector3.zero;
            Debug.Log($"{controller.name} - 退出追击状态");
        }
    }
}

