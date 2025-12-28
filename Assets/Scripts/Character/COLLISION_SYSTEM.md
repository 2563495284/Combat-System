# 碰撞系统说明文档

## 概述

本系统实现了玩家、敌人和地面之间的混合碰撞方案，满足以下需求：
- 玩家撞到敌人时：玩家停下，敌人不被强制位移
- 玩家/敌人掉到地面：自动停下，不会穿透
- 玩家技能可以造成强制位移（击退效果）

## 技术方案

### 1. 地面碰撞（真实物理碰撞）

**实现方式**：
- 使用 Unity 物理引擎的真实碰撞（非Trigger）
- 碰撞体设置为 `isTrigger = false`
- 地面需要添加 `Collider` 组件（BoxCollider、MeshCollider 等）
- 地面需要设置为 `Ground` Layer（Layer 8）

**优点**：
- Unity 自动处理碰撞响应，防止穿透
- 性能好，无需手动检测和修正
- 稳定可靠

### 2. 角色间碰撞（自定义处理）

**实现方式**：
- 玩家和敌人使用非Trigger碰撞体
- 通过 `OnCollisionEnter/Stay/Exit` 回调处理碰撞
- 玩家碰撞到敌人时：
  - 玩家水平速度被清零（停止移动）
  - 玩家被轻微推离敌人（防止重叠）
  - 敌人不受影响（继续移动）
- 敌人碰撞到玩家时：
  - 敌人不受影响（继续移动）
  - 玩家会自己停下（由玩家的碰撞处理逻辑控制）

**关键代码**：
```csharp
// 玩家控制器中
private void HandleCollision(Collider other, bool isEnter)
{
    if (other.CompareTag("Enemy"))
    {
        // 停止玩家移动
        Vector3 currentVel = rb.linearVelocity;
        currentVel.x = 0;
        currentVel.z = 0;
        rb.linearVelocity = currentVel;
        
        // 推离敌人
        Vector3 pushDirection = (transform.position - other.transform.position);
        pushDirection.y = 0;
        transform.position += pushDirection.normalized * 0.1f;
    }
}
```

### 3. 技能强制位移（击退系统）

**实现方式**：
- 调用 `ApplyKnockback(direction, force)` 方法
- 击退时临时设置 `ignoreCharacterCollisions = true`
- 允许角色穿透其他角色（但不会穿透地面）
- 击退结束后自动恢复碰撞检测

**使用示例**：
```csharp
// 在技能系统中
var playerController = target.GetComponent<Character25DController>();
if (playerController != null)
{
    Vector3 knockbackDir = (target.position - attacker.position).normalized;
    playerController.ApplyKnockback(knockbackDir, 5f);
}
```

## 设置步骤

### 1. 设置地面

1. 选择地面 GameObject
2. 添加 `Collider` 组件（BoxCollider 或 MeshCollider）
3. 设置 Layer 为 `Ground`（Layer 8）
4. 确保 Collider 的 `Is Trigger` 为 `false`

### 2. 设置玩家

1. 确保玩家 GameObject 有 `Character25DController` 组件
2. 确保有 `CapsuleCollider` 组件
3. 确保 `CapsuleCollider.isTrigger = false`（代码会自动设置）
4. 设置 Tag 为 `Player`
5. 设置 Layer 为 `Player`（Layer 6）

### 3. 设置敌人

1. 确保敌人 GameObject 有 `Enemy25DController` 组件
2. 确保有 `CapsuleCollider` 组件
3. 确保 `CapsuleCollider.isTrigger = false`（代码会自动设置）
4. 设置 Tag 为 `Enemy`
5. 设置 Layer 为 `Enemy`（Layer 7）

### 4. 配置物理层碰撞矩阵

在 Unity Editor 中：
1. 打开 `Edit > Project Settings > Physics`
2. 确保以下层之间可以碰撞：
   - `Player` 与 `Ground` ✓
   - `Player` 与 `Enemy` ✓
   - `Enemy` 与 `Ground` ✓
   - `Enemy` 与 `Enemy` ✓（可选，用于敌人之间的碰撞）

## 注意事项

1. **碰撞体大小**：确保碰撞体大小合适，避免角色重叠或穿透
2. **物理材质**：当前使用默认物理材质（无摩擦力），如需调整可在 Inspector 中设置
3. **击退效果**：击退时角色可以穿透其他角色，但不会穿透地面
4. **性能**：使用真实物理碰撞比 Trigger 检测性能更好，但需要合理设置碰撞层

## 常见问题

### Q: 角色穿透地面怎么办？
A: 检查地面是否有 Collider 组件，Layer 是否正确设置为 `Ground`，以及 `isTrigger` 是否为 `false`。

### Q: 玩家撞到敌人后卡住了？
A: 检查碰撞体大小是否合适，以及推离逻辑是否正常工作。可以调整 `pushDistance` 参数。

### Q: 击退效果不生效？
A: 确保调用的是 `ApplyKnockback` 方法，而不是直接修改 Rigidbody 速度。击退时会临时忽略角色间碰撞。

### Q: 敌人被玩家推开了？
A: 检查敌人控制器的碰撞处理逻辑，确保敌人不受玩家碰撞影响。当前实现中敌人应该不受影响。

## 代码结构

- `Character25DController.cs`：玩家控制器，处理玩家碰撞
- `Enemy25DController.cs`：敌人控制器，处理敌人碰撞
- 两个控制器都使用相同的碰撞处理模式，但行为略有不同

## 未来扩展

可以考虑添加：
1. 物理材质系统（摩擦力、弹跳等）
2. 碰撞音效和特效
3. 更精细的碰撞响应（如根据碰撞角度调整推离方向）
4. 碰撞伤害系统（可选）

