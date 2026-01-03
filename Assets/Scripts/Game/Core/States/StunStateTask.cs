using BTree;

/// <summary>
/// 眩晕主状态：禁止移动、打断主动技能
/// </summary>
public class StunStateTask : StateTask
{
    protected override int OnEnter()
    {
        Owner?.MoveComp?.Stop();
        Owner?.SkillComp?.InterruptActiveSkill();
        return TaskStatus.RUNNING;
    }

    protected override int OnUpdate(float deltaTime)
    {
        // 持续阻止移动（如果输入系统还在推）
        Owner?.MoveComp?.Stop();
        return TaskStatus.RUNNING;
    }
}


