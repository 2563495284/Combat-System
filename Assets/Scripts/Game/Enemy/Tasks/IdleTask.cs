// using UnityEngine;
// using BTree;
// /// <summary>
// /// 敌人空闲/巡逻任务
// /// 在出生点附近随机巡逻或等待
// /// </summary>
// public class IdleTask : TaskEntry<EnemyBlackboard>
// {
//     private Enemy25DController controller;

//     // private bool isWaiting = true;

//     public IdleTask(Enemy25DController controller)
//     {
//         this.controller = controller;
//     }

//     protected override int Enter()
//     {
//         Debug.Log($"{controller.name} - 进入空闲状态");

//         // 清除目标
//         Blackboard.Target = null;

//         // 停止移动
//         Blackboard.MoveDirection = Vector3.zero;

//         // 开始等待
//         // isWaiting = true;

//         return TaskStatus.RUNNING;
//     }

//     protected override int Execute()
//     {
//         // 空闲状态持续运行，除非外部停止
//         return TaskStatus.RUNNING;
//     }

//     protected override void Exit()
//     {
//         Blackboard.MoveDirection = Vector3.zero;
//         Debug.Log($"{controller.name} - 退出空闲状态");
//     }
// }


