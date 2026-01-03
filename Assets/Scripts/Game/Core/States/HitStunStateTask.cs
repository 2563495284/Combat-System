using BTree;

/// <summary>
/// 受击硬直主状态：短时间禁止移动，可打断主动技能
/// </summary>
public class HitStunStateTask : StateTask
{
    protected override int OnEnter()
    {
        Owner?.MoveComp?.Stop();
        Owner?.SkillComp?.InterruptActiveSkill();
        return TaskStatus.RUNNING;
    }

    protected override int OnUpdate(float deltaTime)
    {
        Owner?.MoveComp?.Stop();
        return TaskStatus.RUNNING;
    }
}


