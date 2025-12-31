using UnityEngine;
using BTree;
/// <summary>
/// 玩家控制任务 (2.5D)
/// 基于 TaskEntry 框架的玩家控制逻辑
/// 负责管理移动、攻击、冲刺等状态任务
/// </summary>
public class PlayerControlTask : TaskEntry<CharacterBlackboard>
{
    private Character25DController character;
    private TaskEntry<CharacterBlackboard> currentStateTask;
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
            // 优先级：攻击  > 移动

            // 检查攻击输入
            if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
            {
                StartSkillTask();
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
            // 攻击不能被移动中断，但可以被更高优先级的状态中断
            if (currentStateTask is MoveStateTask)
            {
                // 移动可以被攻击中断
                if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
                {
                    StartSkillTask();
                    return TaskStatus.RUNNING;
                }

            }
        }

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
    private void StartSkillTask()
    {
        // 停止当前任务
        currentStateTask?.Stop();

        // 创建并设置攻击任务
        currentStateTask = new AttackStateTask(character.GetComponent<CombatEntity>());
        currentStateTask.Blackboard = Blackboard;
        // TaskEntry 会在第一次 Update 时自动启动
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

