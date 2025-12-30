# BTree 行为树框架学习文档

## 📋 目录

1. [框架概述](#框架概述)
2. [核心概念](#核心概念)
3. [类层次结构](#类层次结构)
4. [任务状态](#任务状态)
5. [节点类型](#节点类型)
6. [执行流程](#执行流程)
7. [上下文管理](#上下文管理)
8. [取消机制](#取消机制)
9. [高级特性](#高级特性)
10. [使用示例](#使用示例)
11. [最佳实践](#最佳实践)

---

## 框架概述

这是一个**专业级的行为树框架**，采用**心跳驱动 + 事件驱动混合模式**，具有以下核心特点：

### 🎯 设计理念
- **心跳为主，事件为辅**：心跳自顶向下驱动，事件无规律响应
- **泛型化设计**：支持自定义黑板类型 `Task<T>`
- **协作式取消**：任务取消依赖于任务自身检查取消信号
- **内联优化**：支持任务内联执行，优化性能
- **可复用**：任务树可重置和重复执行

### 🔧 技术特点
- 支持序列化/反序列化
- 完善的生命周期管理
- 灵活的上下文继承
- 前置条件（Guard）机制
- 丰富的控制流选项

---

## 核心概念

### 1. **Task (任务)**
行为树中的基本执行单元，所有节点都继承自 `Task<T>`

```csharp
public abstract class Task<T> : ICancelTokenListener where T : class
```

### 2. **TaskEntry (任务入口)**
行为树的根节点和入口点，负责整棵树的驱动

```csharp
public class TaskEntry<T> : Task<T>
{
    public void Update(int curFrame);  // 驱动行为树更新
    public bool Test();                // 作为条件树测试
}
```

### 3. **Blackboard (黑板)**
任务的运行上下文，存储任务执行所需的数据
- 泛型参数 `T` 即为黑板类型
- 可以在父子节点间自动继承
- 也可以为每个子节点分配独立黑板

### 4. **CancelToken (取消令牌)**
用于协作式取消任务的信号机制
- 支持取消原因和紧急程度
- 可以层级传播
- 支持监听器模式

---

## 类层次结构

```
Task<T> (抽象基类)
├── TaskEntry<T> (任务入口)
├── BranchTask<T> (分支节点基类)
│   ├── Selector<T> (选择器)
│   ├── Sequence<T> (序列)
│   ├── SimpleParallel<T> (简单并行)
│   ├── Switch<T> (开关)
│   └── ...
├── Decorator<T> (装饰器基类)
│   ├── Repeat<T> (重复)
│   ├── Inverter<T> (反转)
│   ├── AlwaysSuccess<T> (总是成功)
│   ├── OnlyOnce<T> (仅执行一次)
│   └── ...
└── LeafTask<T> (叶子节点基类)
    ├── Success<T> (成功)
    ├── Failure<T> (失败)
    ├── Running<T> (运行中)
    └── WaitFrame<T> (等待帧)
```

---

## 任务状态

### 状态码定义

```csharp
public class TaskStatus
{
    public const int NEW = 0;           // 初始状态
    public const int RUNNING = 1;       // 执行中
    public const int SUCCESS = 2;       // 执行成功
    public const int CANCELLED = 3;     // 被取消
    public const int ERROR = 4;         // 默认失败码
    public const int GUARD_FAILED = 5;  // 前置条件失败
    public const int CHILDLESS = 6;     // 没有子节点
    public const int TIMEOUT = 8;       // 执行超时
    // ... 更多状态码
}
```

### 状态判断

```csharp
task.IsRunning      // 是否正在运行
task.IsCompleted    // 是否已完成（成功、失败或取消）
task.IsSucceeded    // 是否成功
task.IsFailed       // 是否失败
task.IsCancelled    // 是否被取消
```

---

## 节点类型

### 1. 分支节点 (BranchTask)

#### **Selector (选择器)**
- **语义**：顺序执行子节点，直到某个子节点**成功**
- **返回**：
  - 子节点成功 → SUCCESS
  - 所有子节点失败 → ERROR
  - 子节点取消 → CANCELLED

```csharp
var selector = new Selector<MyBlackboard>();
selector.AddChild(new TaskA());
selector.AddChild(new TaskB());
selector.AddChild(new TaskC());
// 执行顺序：A失败→B失败→C成功，返回SUCCESS
```

#### **Sequence (序列)**
- **语义**：顺序执行子节点，直到某个子节点**失败**
- **返回**：
  - 所有子节点成功 → SUCCESS
  - 子节点失败 → 子节点的失败码
  - 子节点取消 → CANCELLED

```csharp
var sequence = new Sequence<MyBlackboard>();
sequence.AddChild(new CheckHealth());
sequence.AddChild(new FindEnemy());
sequence.AddChild(new Attack());
// 必须所有子节点都成功才返回SUCCESS
```

### 2. 装饰器节点 (Decorator)

装饰器包装单个子节点，修改其行为

#### **Repeat (重复)**
- 重复执行子节点指定次数
- 支持多种计数模式

```csharp
var repeat = new Repeat<MyBlackboard>
{
    Required = 3,  // 重复3次
    CountMode = RepeatMode.MODE_ALWAYS  // 总是计数
};
repeat.Child = new DoSomething();
```

#### **Inverter (反转)**
- 反转子节点的成功/失败结果

```csharp
var inverter = new Inverter<MyBlackboard>();
inverter.Child = new IsEnemyNear();  // 敌人不在附近时返回成功
```

#### **AlwaysSuccess / AlwaysFail**
- 强制返回特定结果

#### **OnlyOnce (仅执行一次)**
- 子节点只执行一次，后续返回缓存结果

### 3. 叶子节点 (LeafTask)

最底层的执行节点，不包含子节点

#### **内置叶子节点**
```csharp
Success<T>      // 立即返回成功
Failure<T>      // 立即返回失败
Running<T>      // 立即返回运行中
WaitFrame<T>    // 等待指定帧数
```

#### **自定义叶子节点**
```csharp
public class AttackEnemy : LeafTask<CombatBlackboard>
{
    protected override int Execute()
    {
        var enemy = blackboard.CurrentEnemy;
        if (enemy == null)
            return TaskStatus.ERROR;
        
        // 执行攻击逻辑
        enemy.TakeDamage(10);
        return TaskStatus.SUCCESS;
    }
    
    protected override void OnEventImpl(object eventObj)
    {
        // 处理外部事件
    }
}
```

---

## 执行流程

### 任务生命周期

```
┌──────────────────────────────────────────────┐
│ 1. NEW (初始状态)                             │
└──────────────────────────────────────────────┘
                    ↓ Template_Start
┌──────────────────────────────────────────────┐
│ 2. BeforeEnter() - 初始化                     │
│    - 设置控制流选项                            │
│    - 准备运行上下文                            │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 3. Enter() - 进入运行状态                      │
│    - 可以直接返回完成状态                      │
│    - 或返回RUNNING进入执行阶段                 │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 4. Execute() - 心跳执行 (循环调用)            │
│    - 每帧调用一次                              │
│    - 返回RUNNING继续执行                       │
│    - 返回SUCCESS/ERROR完成                     │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 5. Exit() - 退出清理                          │
│    - 停止子节点                                │
│    - 取消注册的监听器                          │
│    - 释放自动继承的上下文                      │
└──────────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────────┐
│ 6. COMPLETED (SUCCESS/ERROR/CANCELLED)       │
└──────────────────────────────────────────────┘
```

### 核心方法详解

#### **BeforeEnter()**
```csharp
protected virtual void BeforeEnter()
{
    // 对象初始化
    // 设置控制流选项
    // 不能使自己进入完成状态
}
```

#### **Enter()**
```csharp
protected virtual int Enter()
{
    // 任务启动逻辑
    // 可以初始化子节点
    // 允许直接返回完成状态
    return TaskStatus.RUNNING;
}
```

#### **Execute()**
```csharp
protected abstract int Execute()
{
    // 心跳逻辑，每帧调用
    // 返回RUNNING继续执行
    // 返回SUCCESS/ERROR完成任务
}
```

#### **Exit()**
```csharp
protected virtual void Exit()
{
    // 清理运行时产生的临时数据
    // 取消注册的监听器
    // 对应Enter()方法的清理
}
```

---

## 上下文管理

### 运行上下文三要素

#### 1. **Blackboard (黑板)**
```csharp
public T Blackboard { get; set; }
```
主要数据上下文，存储任务执行需要的数据

#### 2. **SharedProps (共享属性)**
```csharp
public object SharedProps { get; set; }
```
配置上下文，用于策划配置，应该是只读的

#### 3. **CancelToken (取消令牌)**
```csharp
public CancelToken CancelToken { get; set; }
```
取消上下文，用于协作式取消

### 上下文继承

子节点在启动时会自动从父节点继承上下文：

```csharp
// 自动继承父节点的黑板
if (child.Blackboard == null) {
    child.Blackboard = parent.Blackboard;
}

// 自动继承父节点的共享属性
if (child.SharedProps == null) {
    child.SharedProps = parent.SharedProps;
}

// 自动继承父节点的取消令牌
if (child.CancelToken == null) {
    child.CancelToken = parent.CancelToken;
}
```

### 上下文控制选项

```csharp
// 每个子节点使用独立黑板
task.IsBlackboardPerChild = true;

// 每个子节点使用独立取消令牌
task.IsCancelTokenPerChild = true;
```

---

## 取消机制

### CancelToken 基本用法

```csharp
// 创建取消令牌
var cancelToken = new CancelToken();

// 发起取消请求
cancelToken.Cancel(CancelCodes.REASON_SHUTDOWN);

// 检查是否收到取消信号
if (cancelToken.IsCancelRequested)
{
    return TaskStatus.CANCELLED;
}
```

### 取消监听

```csharp
public class MyTask : LeafTask<MyBlackboard>
{
    protected override void BeforeEnter()
    {
        // 自动注册取消监听
        IsAutoListenCancel = true;
    }
    
    public override void OnCancelRequested(CancelToken cancelToken)
    {
        // 收到取消信号的回调
        // 可以在这里进行及时响应
    }
}
```

### 取消码结构

```csharp
// 取消码 = 原因 (低20位) + 特殊信息 (高12位)
int cancelCode = CancelCodes.REASON_DEFAULT;      // 默认原因
int reason = CancelCodes.GetReason(cancelCode);   // 提取原因
int degree = CancelCodes.GetDegree(cancelCode);   // 提取紧急程度
```

---

## 高级特性

### 1. 前置条件 (Guard)

Guard 是任务的前置条件，在任务启动前检查

```csharp
// 设置前置条件
var attackTask = new Attack();
attackTask.Guard = new CheckEnemyInRange();

// 前置条件失败时，任务不会启动
// 返回 TaskStatus.GUARD_FAILED
```

**Guard 特点**：
- 必须在一步内完成（不能返回RUNNING）
- 只依赖共享上下文（黑板和props）
- 可以内联反转（InvertedGuard）
- 支持嵌套（Guard的Guard）

### 2. 任务内联 (Inlining)

内联优化可以减少调用栈深度，提高性能

```csharp
[TaskInlinable]  // 标记任务可内联
public class Selector<T> : SingleRunningChildBranch<T>
{
    protected override int Execute()
    {
        // 内联执行子节点
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null)
        {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, this);
        }
        // ...
    }
}
```

### 3. Active 状态管理

控制任务是否执行心跳逻辑

```csharp
// 暂停任务的心跳执行（但不停止任务）
task.SetActive(false);

// 恢复任务的心跳执行
task.SetActive(true);

// 查询状态
bool isActive = task.IsActiveSelf;
bool isActiveInHierarchy = task.IsActiveInHierarchy;
```

**应用场景**：
- 等待外部事件时暂停心跳
- 需要配合定时器唤醒
- 不影响事件处理

### 4. 控制流选项

```csharp
// 延迟启动（Enter和Execute分开执行）
task.IsSlowStart = true;

// 自动重置子节点
task.IsAutoResetChildren = true;

// 手动检测取消（禁用自动检测）
task.IsManualCheckCancel = true;

// 打破内联
task.IsBreakInline = true;
```

### 5. 重入检测

用于处理事件驱动下的任务状态变化

```csharp
protected override int Execute()
{
    // 捕获重入ID
    int reentryId = ReentryId;
    
    // 执行可能触发事件的外部逻辑
    DoSomethingThatMayTriggerEvent();
    
    // 检查任务是否已退出
    if (IsExited(reentryId))
    {
        return status;  // 任务已结束，立即返回
    }
    
    // 继续执行
    return TaskStatus.RUNNING;
}
```

### 6. ControlData

父节点可以在子节点上存储数据

```csharp
// 父节点存储数据到子节点
child.ControlData = new MyData { Index = 0 };

// 子节点获取数据
var data = (MyData)this.ControlData;
```

---

## 使用示例

### 示例1：简单的AI行为树

```csharp
// 1. 定义黑板
public class AIBlackboard
{
    public GameObject Enemy { get; set; }
    public float Health { get; set; }
    public Vector3 PatrolTarget { get; set; }
}

// 2. 创建叶子节点
public class FindEnemy : LeafTask<AIBlackboard>
{
    protected override int Execute()
    {
        var enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (enemy != null)
        {
            blackboard.Enemy = enemy;
            return TaskStatus.SUCCESS;
        }
        return TaskStatus.ERROR;
    }
    
    protected override void OnEventImpl(object eventObj) { }
}

public class AttackEnemy : LeafTask<AIBlackboard>
{
    protected override int Execute()
    {
        var enemy = blackboard.Enemy;
        if (enemy == null)
            return TaskStatus.ERROR;
        
        // 攻击逻辑
        Debug.Log($"Attacking {enemy.name}");
        return TaskStatus.SUCCESS;
    }
    
    protected override void OnEventImpl(object eventObj) { }
}

public class Patrol : LeafTask<AIBlackboard>
{
    protected override int Execute()
    {
        // 巡逻逻辑
        Debug.Log("Patrolling...");
        return TaskStatus.SUCCESS;
    }
    
    protected override void OnEventImpl(object eventObj) { }
}

// 3. 构建行为树
var blackboard = new AIBlackboard { Health = 100 };

var root = new Selector<AIBlackboard>();

// 分支1：发现敌人就攻击
var attackSequence = new Sequence<AIBlackboard>();
attackSequence.AddChild(new FindEnemy());
attackSequence.AddChild(new AttackEnemy());

// 分支2：否则巡逻
var patrol = new Patrol();

root.AddChild(attackSequence);
root.AddChild(patrol);

// 4. 创建并运行任务入口
var taskEntry = new TaskEntry<AIBlackboard>(
    name: "AI_Behavior",
    rootTask: root,
    blackboard: blackboard
);

// 每帧更新
int currentFrame = 0;
void Update()
{
    taskEntry.Update(currentFrame++);
}
```

### 示例2：带重复和条件的行为树

```csharp
// 巡逻3次，然后休息
var root = new Sequence<AIBlackboard>();

// 重复巡逻3次
var repeatPatrol = new Repeat<AIBlackboard>
{
    Required = 3,
    CountMode = RepeatMode.MODE_ALWAYS
};
repeatPatrol.Child = new Patrol();

root.AddChild(repeatPatrol);
root.AddChild(new Rest());

// 添加前置条件：只有生命值>30%才执行
root.Guard = new CheckHealthAbove30Percent();
```

### 示例3：处理外部事件

```csharp
public class WaitForSignal : LeafTask<MyBlackboard>
{
    private bool signalReceived = false;
    
    protected override void BeforeEnter()
    {
        signalReceived = false;
        // 暂停心跳，等待事件
        SetActive(false);
    }
    
    protected override int Execute()
    {
        if (signalReceived)
        {
            return TaskStatus.SUCCESS;
        }
        return TaskStatus.RUNNING;
    }
    
    protected override void OnEventImpl(object eventObj)
    {
        if (eventObj is SignalEvent signal)
        {
            signalReceived = true;
            // 恢复心跳
            SetActive(true);
        }
    }
}

// 发送事件
taskEntry.OnEvent(new SignalEvent());
```

### 示例4：使用TreeLoader加载行为树

```csharp
// 实现加载器
public class MyTreeLoader : ITreeLoader
{
    public object? TryLoadObject(string nameOrGuid)
    {
        // 从资源文件加载行为树
        return Resources.Load<TextAsset>($"BehaviorTrees/{nameOrGuid}");
    }
    
    public List<object> LoadManyFromFile(string fileName, 
        Predicate<IEntry>? filter, bool sharable = false)
    {
        // 批量加载实现
        return new List<object>();
    }
}

// 使用加载器
var loader = new MyTreeLoader();
var taskEntry = loader.LoadTree<AIBlackboard>("EnemyAI");
taskEntry.Entity = enemyGameObject;
taskEntry.Update(0);
```

---

## 最佳实践

### ✅ 推荐做法

1. **数据与行为分离**
   ```csharp
   // 好：数据存储在黑板中
   public class AttackTask : LeafTask<CombatBlackboard>
   {
       protected override int Execute()
       {
           var enemy = blackboard.CurrentEnemy;  // 从黑板获取
           // ...
       }
   }
   ```

2. **Guard 用于前置条件检查**
   ```csharp
   // 设置前置条件而不是在Execute中检查
   attackTask.Guard = new IsEnemyInRange();
   ```

3. **正确处理重入**
   ```csharp
   protected override int Execute()
   {
       int rid = ReentryId;
       
       // 执行外部逻辑
       child.Template_Execute(true);
       
       // 检查是否已退出
       if (IsExited(rid))
           return status;
       
       // 继续处理
   }
   ```

4. **Exit 中清理资源**
   ```csharp
   protected override void Exit()
   {
       // 取消注册的监听器
       // 清理临时数据
       // 对应 Enter() 的清理操作
   }
   ```

5. **ResetForRestart 处理复用**
   ```csharp
   public override void ResetForRestart()
   {
       base.ResetForRestart();
       // 重置所有运行时产生的数据
       count = 0;
       tempData = null;
   }
   ```

### ❌ 避免的陷阱

1. **不要在运行时增删子节点**
   ```csharp
   // 危险操作
   if (IsRunning)
   {
       AddChild(newChild);  // ❌ 可能导致问题
   }
   ```

2. **不要在BeforeEnter中使自己进入完成状态**
   ```csharp
   protected override void BeforeEnter()
   {
       // ❌ 不允许
       return TaskStatus.SUCCESS;
   }
   ```

3. **Guard 不能返回 RUNNING**
   ```csharp
   public class MyGuard : LeafTask<MyBlackboard>
   {
       protected override int Execute()
       {
           // ❌ Guard必须一步完成
           return TaskStatus.RUNNING;
       }
   }
   ```

4. **不要忘记处理取消信号**
   ```csharp
   protected override int Execute()
   {
       // ✅ 长时间运行的任务应该检查取消
       if (cancelToken.IsCancelRequested)
           return TaskStatus.CANCELLED;
       
       // 执行逻辑
   }
   ```

5. **SharedProps 应该是只读的**
   ```csharp
   // ❌ 不要修改共享属性
   sharedProps.Value = newValue;
   
   // ✅ SharedProps 只用于读取配置
   var config = sharedProps.AttackRange;
   ```

---

## 性能优化建议

### 1. 使用内联优化
```csharp
[TaskInlinable]  // 标记可内联的节点
public class MySelector : Selector<T> { }
```

### 2. 条件检测优化
```csharp
// Selector和Sequence在条件检测模式下有特殊优化
if (IsCheckingGuard())
{
    // 快速路径，不启动子节点
}
```

### 3. 合理使用 Active 状态
```csharp
// 等待事件时暂停心跳
protected override void BeforeEnter()
{
    SetActive(false);  // 减少不必要的Execute调用
}
```

### 4. 避免频繁的内存分配
```csharp
// 复用对象，避免每次都创建新实例
public override void ResetForRestart()
{
    base.ResetForRestart();
    // 重置而不是重新创建
    tempList.Clear();
}
```

---

## 调试技巧

### 1. 查看任务状态
```csharp
Debug.Log($"Task: {task.GetType().Name}");
Debug.Log($"Status: {task.Status}");
Debug.Log($"IsRunning: {task.IsRunning}");
Debug.Log($"RunFrames: {task.RunFrames}");
Debug.Log($"PrevStatus: {task.PrevStatus}");
```

### 2. 使用 TaskVisitor 遍历树
```csharp
// 访问所有子节点
task.VisitChildren(new MyTaskVisitor<T>(), null);

public class MyTaskVisitor<T> : TaskVisitor<T> where T : class
{
    public void VisitChild(Task<T> child, int index, object param)
    {
        Debug.Log($"Child[{index}]: {child.GetType().Name}");
    }
}
```

### 3. 重入检测
```csharp
// 检测任务是否被意外重入
int rid = ReentryId;
// ... 执行一些操作
Debug.Assert(!IsExited(rid), "Task unexpectedly exited!");
```

---

## 总结

这个行为树框架提供了：

✨ **完整的任务生命周期管理**  
✨ **灵活的上下文继承机制**  
✨ **协作式取消机制**  
✨ **前置条件（Guard）支持**  
✨ **内联优化性能**  
✨ **丰富的控制流选项**  
✨ **事件驱动能力**  
✨ **可序列化和复用**

适用于：
- 游戏AI行为逻辑
- 复杂的状态机
- 技能系统
- 任务系统
- 工作流引擎

---

## 快速参考

### 常用节点类型
| 类型 | 说明 | 使用场景 |
|------|------|----------|
| Selector | 选择器，找第一个成功的子节点 | 优先级决策 |
| Sequence | 序列，所有子节点必须成功 | 步骤流程 |
| Repeat | 重复执行 | 循环行为 |
| Inverter | 反转成功/失败 | 条件取反 |
| OnlyOnce | 只执行一次 | 初始化逻辑 |

### 关键方法
| 方法 | 时机 | 用途 |
|------|------|------|
| BeforeEnter() | 启动前 | 初始化 |
| Enter() | 进入运行 | 启动逻辑 |
| Execute() | 每帧 | 心跳逻辑 |
| Exit() | 结束时 | 清理资源 |
| OnEventImpl() | 收到事件 | 事件处理 |
| ResetForRestart() | 重置时 | 复用准备 |

### 重要属性
| 属性 | 说明 |
|------|------|
| Blackboard | 主数据上下文 |
| SharedProps | 配置上下文 |
| CancelToken | 取消令牌 |
| Guard | 前置条件 |
| Status | 当前状态 |
| Control | 控制节点（父节点） |

---

**文档版本**: 1.0  
**最后更新**: 2025-12-30

如有疑问，请参考源码注释或联系框架维护者。

