
/// <summary>
/// 状态添加事件
/// </summary>
public class StateAddedEvent
{
    public State state;        // 添加的状态
}

/// <summary>
/// 状态移除事件
/// </summary>
public class StateRemovedEvent
{
    public State state;        // 移除的状态
}

/// <summary>
/// 状态更新事件
/// </summary>
public class StateUpdatedEvent
{
    public State state;        // 更新的状态
    public float deltaTime;    // 时间增量
}

/// <summary>
/// 状态叠层改变事件
/// </summary>
public class StateStackChangedEvent
{
    public State state;        // 状态
    public int oldStack;       // 旧叠层
    public int newStack;       // 新叠层
}


