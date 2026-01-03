using BTree;

/// <summary>
/// 冰冻主状态：禁止移动、可选禁止施法（这里默认打断主动技能）
/// </summary>
public class FreezeStateTask : StateTask
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


