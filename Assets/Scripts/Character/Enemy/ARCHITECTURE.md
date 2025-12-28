# 敌人系统架构文档

## 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    Enemy25DController                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Rigidbody   │  │ CombatEntity │  │   Collider   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │           EnemyBlackboard (数据层)                │      │
│  │  • AI State                                       │      │
│  │  • Target Info                                    │      │
│  │  • Movement Data                                  │      │
│  │  • Attack Data                                    │      │
│  └──────────────────────────────────────────────────┘      │
│                          ↓                                   │
│  ┌──────────────────────────────────────────────────┐      │
│  │           EnemyAITask (AI状态机)                  │      │
│  │                                                    │      │
│  │  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐        │      │
│  │  │ Idle │→│Chase │→│Attack│→│ Dead │        │      │
│  │  └──────┘  └──────┘  └──────┘  └──────┘        │      │
│  │      ↑         ↓                                  │      │
│  │      └─────────┘                                  │      │
│  └──────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## 类关系图

```
┌──────────────────┐
│  MonoBehaviour   │
└────────┬─────────┘
         │
         ↓
┌──────────────────────┐      ┌──────────────────┐
│ Enemy25DController   │◄─────│ EnemyBlackboard  │
└──────────┬───────────┘      └──────────────────┘
           │
           │ has
           ↓
┌──────────────────────┐
│    EnemyAITask       │
│  (TaskEntry<BB>)     │
└──────────┬───────────┘
           │
           │ manages
           ↓
    ┌──────┴──────┐
    │             │
    ↓             ↓
┌────────┐   ┌────────┐   ┌────────────┐
│IdleTask│   │ChaseTask│   │AttackTask  │
└────────┘   └────────┘   └────────────┘
```

## 数据流图

```
Input (玩家位置)
    ↓
┌──────────────────────────┐
│  Target Detection        │  检测玩家
└────────┬─────────────────┘
         ↓
┌──────────────────────────┐
│  Update Blackboard       │  更新黑板数据
└────────┬─────────────────┘
         ↓
┌──────────────────────────┐
│  State Evaluation        │  评估状态转换
└────────┬─────────────────┘
         ↓
┌──────────────────────────┐
│  Execute Current Task    │  执行当前任务
└────────┬─────────────────┘
         ↓
┌──────────────────────────┐
│  Apply Movement          │  应用移动
└────────┬─────────────────┘
         ↓
┌──────────────────────────┐
│  Update Physics          │  更新物理
└──────────────────────────┘
```

## 状态机转换图

```
                    ┌──────────┐
                    │  Start   │
                    └────┬─────┘
                         ↓
                    ┌────────┐
              ┌────►│  Idle  │◄────┐
              │     └────┬───┘     │
              │          │          │
              │  Detect  │          │ Lost
              │  Player  │          │ Target
              │          ↓          │
              │     ┌────────┐     │
              │     │ Chase  │─────┘
              │     └────┬───┘
              │          │
              │   Enter  │
              │   Range  │
              │          ↓
              │     ┌────────┐
              │     │ Attack │
              │     └────┬───┘
              │          │
              │  Attack  │
              │  Complete│
              │          │
              └──────────┘
                         
                    HP = 0
                         ↓
                    ┌────────┐
                    │  Dead  │
                    └────────┘
```

## 任务执行时序图

```
Frame N:
  Enemy25DController.Update()
    │
    ├─→ UpdateGroundedState()
    ├─→ UpdateTargetDetection()
    ├─→ UpdateBlackboard()
    │
    └─→ EnemyAITask.Update()
          │
          ├─→ EvaluateStateTransition()
          │     │
          │     ├─→ Check Current State
          │     ├─→ Check Conditions
          │     └─→ TransitionToState() [if needed]
          │
          └─→ CurrentSubTask.Update()
                │
                ├─→ IdleTask.OnUpdate()
                │     ├─→ Wait or Patrol
                │     └─→ Update MoveDirection
                │
                ├─→ ChaseTask.OnUpdate()
                │     ├─→ Calculate Direction
                │     └─→ Update MoveDirection
                │
                └─→ AttackTask.OnUpdate()
                      ├─→ Update Timer
                      ├─→ ExecuteAttackHit()
                      └─→ Check Complete

FixedUpdate:
  Enemy25DController.FixedUpdate()
    │
    ├─→ ApplyMovement()
    │     ├─→ Calculate Velocity
    │     └─→ Apply to Rigidbody
    │
    └─→ ApplyGravity()
          └─→ Apply Gravity Force
```

## 组件交互图

```
┌─────────────────────────────────────────────────────┐
│              Enemy25DController                      │
│                                                      │
│  ┌────────────────┐         ┌──────────────────┐  │
│  │ Physics Layer  │         │   AI Layer       │  │
│  │                │         │                  │  │
│  │ • Rigidbody    │         │ • EnemyAITask    │  │
│  │ • Collider     │         │ • SubTasks       │  │
│  │ • Movement     │         │ • State Machine  │  │
│  └────────┬───────┘         └────────┬─────────┘  │
│           │                          │             │
│           └──────────┬───────────────┘             │
│                      ↓                              │
│           ┌──────────────────┐                     │
│           │ EnemyBlackboard  │                     │
│           │  (Data Bridge)   │                     │
│           └──────────┬───────┘                     │
│                      ↓                              │
│           ┌──────────────────┐                     │
│           │  CombatEntity    │                     │
│           │  (Combat Layer)  │                     │
│           │                  │                     │
│           │ • AttrComp       │                     │
│           │ • StateComp      │                     │
│           │ • SkillComp      │                     │
│           │ • EventBus       │                     │
│           └──────────────────┘                     │
└─────────────────────────────────────────────────────┘
```

## 攻击判定流程

```
AttackTask.OnUpdate()
    │
    ├─→ attackTimer >= hitTiming?
    │       │
    │       └─→ ExecuteAttackHit()
    │             │
    │             ├─→ Calculate Attack Position
    │             │     └─→ Position + Offset (based on facing)
    │             │
    │             ├─→ Physics.OverlapBox()
    │             │     └─→ Detect Targets in Range
    │             │
    │             ├─→ For Each Hit:
    │             │     │
    │             │     ├─→ Get CombatEntity
    │             │     │
    │             │     ├─→ CombatEntity.DealDamage()
    │             │     │     │
    │             │     │     ├─→ Calculate Damage
    │             │     │     │     └─→ Attack - Defense * 0.5
    │             │     │     │
    │             │     │     └─→ Fire DamageEvent
    │             │     │           └─→ Target.EventBus.Fire()
    │             │     │
    │             │     └─→ Apply Knockback
    │             │           └─→ AddForce to Rigidbody
    │             │
    │             └─→ Play Effects
    │
    └─→ attackTimer >= attackDuration?
            │
            └─→ Complete()
```

## 内存布局

```
Enemy GameObject
├── Components (Unity)
│   ├── Transform
│   ├── Rigidbody (12 bytes + physics data)
│   ├── CapsuleCollider (16 bytes + collider data)
│   └── Renderers (variable)
│
├── CombatEntity (CombatSystem)
│   ├── EventBus (~1KB)
│   ├── StateComponent (~2KB)
│   ├── AttrComponent (~500 bytes)
│   ├── SkillComponent (~1KB)
│   └── MoveComponent (~500 bytes)
│
├── Enemy25DController
│   ├── Blackboard (~500 bytes)
│   ├── CurrentTask (~200 bytes)
│   └── SubTasks (3 × ~200 bytes)
│
└── Visual Objects
    ├── Body (Mesh + Material)
    └── Eye (Mesh + Material)

Total per Enemy: ~8-10KB (excluding meshes/textures)
```

## 性能特性

### Update Frequency
```
Update (60 FPS):
  - Target Detection
  - Blackboard Update
  - AI State Evaluation
  - Task Logic

FixedUpdate (50 FPS):
  - Physics Movement
  - Gravity Application
```

### Optimization Points
```
1. Target Detection
   - Cache player reference
   - Update distance calculation per frame
   - Avoid FindGameObjectWithTag in loop

2. State Transition
   - Early exit conditions
   - Minimal state checks
   - No memory allocation

3. Physics
   - Custom gravity (no Unity gravity)
   - Freeze rotation constraints
   - Simple collider shapes
```

## 扩展点

### 1. 添加新状态
```csharp
// 1. 添加枚举
public enum EnemyAIState {
    Idle, Patrol, Chase, Attack, Flee, Dead
}

// 2. 创建任务
public class FleeTask : TaskEntry<EnemyBlackboard> { }

// 3. 在 EnemyAITask 中集成
private FleeTask fleeTask;
```

### 2. 自定义攻击
```csharp
public class RangedAttackTask : EnemyAttackTask {
    protected override void ExecuteAttackHit() {
        // 发射投射物
        SpawnProjectile();
    }
}
```

### 3. 添加感知系统
```csharp
public class PerceptionComponent {
    public List<CombatEntity> VisibleTargets;
    public List<Vector3> HeardSounds;
    
    public void Update() {
        UpdateVision();
        UpdateHearing();
    }
}
```

## 设计原则

1. **单一职责**: 每个类只负责一个功能
2. **开闭原则**: 对扩展开放，对修改封闭
3. **依赖倒置**: 依赖抽象（TaskEntry）而非具体实现
4. **组合优于继承**: 使用任务组合而非继承层次
5. **数据驱动**: 通过黑板共享数据，避免紧耦合

## 总结

这个架构提供了：
- ✅ 清晰的层次结构
- ✅ 良好的扩展性
- ✅ 高效的性能
- ✅ 易于调试
- ✅ 完整的CombatSystem集成

