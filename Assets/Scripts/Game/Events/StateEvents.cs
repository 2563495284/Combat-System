/// <summary>
/// 通用状态事件（技能/被动/Buff统一）
/// </summary>
public class StateAddedEvent
{
    public CombatEntity owner;
    public State state;
}

public class StateRemovedEvent
{
    public CombatEntity owner;
    public State state;
    public int stopStatus;   // BTree.TaskStatus
    public string reason;    // 可选：Expired/SlotReplaced/Manual/Dispel/...
}
