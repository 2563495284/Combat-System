using UnityEngine;
using BTree;

/// <summary>
/// 死亡状态：主状态槽上的“屏障状态”
/// - 常驻 RUNNING，阻止其它状态（靠 cfg.mutexAll + priority）
/// - 负责在运行期持续禁止移动/中断技能等（防止外部系统反复尝试）
/// </summary>
public class DeathStateTask : StateTask
{
    protected override int OnEnter()
    {
        if (Owner != null)
        {
            Owner.MoveComp?.Stop();
            Owner.SkillComp?.InterruptActiveSkill();
        }
        return TaskStatus.RUNNING;
    }

    protected override int OnUpdate(float deltaTime)
    {
        if (Owner != null)
        {
            Owner.MoveComp?.Stop();
        }
        return TaskStatus.RUNNING;
    }
}


