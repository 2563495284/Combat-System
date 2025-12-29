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

        // private bool isWaiting = true;

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
            // isWaiting = true;
        }

        protected override void OnUpdate(float deltaTime)
        {

        }

        protected override void OnStop()
        {
            Blackboard.MoveDirection = Vector3.zero;
            Debug.Log($"{controller.name} - 退出空闲状态");
        }
    }
}

