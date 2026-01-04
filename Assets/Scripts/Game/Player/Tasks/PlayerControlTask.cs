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
    private CombatEntity combatEntity;
    private TaskEntry<CharacterBlackboard> currentStateTask;
    private int currentFrame;
    public PlayerControlTask(Character25DController character)
    {
        this.character = character;
        this.combatEntity = character != null ? character.GetComponent<CombatEntity>() : null;
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

        // 统一锁定计算：由行为树负责最终写入黑板控制开关
        // 目的：避免通过外部脚本/事件监听改黑板造成时序与覆盖问题
        ApplyControlLocks();

        // 更新当前状态任务
        if (currentStateTask != null)
        {
            currentStateTask.Update(currentFrame);

            // 如果状态任务完成，清除它
            if (currentStateTask.IsCompleted)
            {
                // 移动任务结束时移除“移动标记状态”
                if (currentStateTask is MoveStateTask)
                {
                    combatEntity?.StateComp?.RemoveStateById(8001);
                }
                currentStateTask = null;
            }
        }

        // 当前任务/状态可能刚发生变化（例如攻击结束），再算一次保证最终结果正确
        ApplyControlLocks();

        // 检查是否可以切换状态
        if (currentStateTask == null || currentStateTask.IsCompleted)
        {
            // 优先级：攻击  > 移动

            // 检查攻击输入
            if (Blackboard.InputAttack && Blackboard.CanAttack && !Blackboard.IsAttacking)
            {
                StartSkillTask();
                ApplyControlLocks();
                return TaskStatus.RUNNING;
            }
            // 检查移动输入
            if (Blackboard.CanMove && Blackboard.InputMove.sqrMagnitude > 0.01f)
            {
                StartMoveTask();
                ApplyControlLocks();
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
                    ApplyControlLocks();
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

        // 挂载“移动主状态”（MAIN_STATE slot），便于互斥与查询
        if (combatEntity != null)
        {
            var moveCfg = StateCfgManager.Instance.GetConfig(8001);
            if (moveCfg != null && (combatEntity.StateComp == null || !combatEntity.StateComp.TryGetState(8001, out _)))
            {
                combatEntity.ApplyState(moveCfg, combatEntity, 1, null, null);
            }
        }

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
        // 攻击开始前，移除“移动标记状态”
        combatEntity?.StateComp?.RemoveStateById(8001);

        // 停止当前任务
        currentStateTask?.Stop();

        // 创建并设置攻击任务
        currentStateTask = new AttackStateTask(character.GetComponent<CombatEntity>());
        currentStateTask.Blackboard = Blackboard;
        // TaskEntry 会在第一次 Update 时自动启动
    }

    /// <summary>
    /// 由行为树统一写入黑板控制权（锁逻辑不依赖外部组件）
    /// </summary>
    private void ApplyControlLocks()
    {
        if (Blackboard == null || combatEntity == null)
            return;

        // 主状态槽：移动也在主槽，所以不能用“主槽非空”作为锁
        // 锁条件：主槽处于控制类状态（死亡/冰冻/眩晕/受击硬直等），移动(8001)不算
        var mainState = combatEntity.StateComp?.GetMainState();
        int mainStateId = mainState?.Cfg != null ? mainState.Cfg.cid : 0;
        bool inLockMainState = mainState != null && mainStateId != StaticSlotIds.MAIN_STATE
                               && (mainStateId == 9001 || mainStateId == 9002 || mainStateId == 9003 || mainStateId == 9004);

        // 技能施放中（主动技能）
        bool castingActiveSkill = combatEntity.SkillComp != null && combatEntity.SkillComp.IsCastingActiveSkill();

        // 行为树当前任务态（攻击任务视为锁移动）
        bool inAttackTask = currentStateTask is AttackStateTask || Blackboard.IsAttacking;

        Blackboard.IsDead = (mainStateId == 9001);

        // 统一策略（你要不同策略可再细分）：
        // - 控制类主状态：禁移动/跳跃/攻击
        // - 主动技能施放中：禁移动/跳跃/攻击
        // - 攻击任务执行中：禁移动（攻击是否禁由 castingActiveSkill 决定）
        Blackboard.CanMove = !(inLockMainState || castingActiveSkill || inAttackTask);
        Blackboard.CanJump = !(inLockMainState || castingActiveSkill);
        Blackboard.CanAttack = !(inLockMainState || castingActiveSkill);
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

