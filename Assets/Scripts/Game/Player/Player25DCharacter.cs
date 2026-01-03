using UnityEngine;

/// <summary>
/// 2.5D 玩家角色管理器
/// 整合所有2.5D系统组件
/// </summary>
[RequireComponent(typeof(Character25DController))]
[RequireComponent(typeof(InputController))]
[RequireComponent(typeof(CombatEntity))]
[RequireComponent(typeof(AnimationComponent))]
public class Player25DCharacter : MonoBehaviour
{
    private AnimationComponent animationComponent;

    private Character25DController controller;
    private InputController inputController;
    private CombatEntity combatEntity;

    private void Awake()
    {
        controller = GetComponent<Character25DController>();
        inputController = GetComponent<InputController>();
        combatEntity = GetComponent<CombatEntity>();
        animationComponent = GetComponent<AnimationComponent>();
    }

    private void Start()
    {

    }

    private void Update()
    {
        // 更新动画（由 AnimationComponent 统一管理）
        animationComponent?.UpdateAnimations(controller?.Blackboard);
    }

    /// <summary>
    /// 获取角色控制器
    /// </summary>
    public Character25DController GetController() => controller;

    /// <summary>
    /// 获取黑板
    /// </summary>
    public CharacterBlackboard GetBlackboard() => controller?.Blackboard;

    /// <summary>
    /// 获取战斗实体
    /// </summary>
    public CombatEntity GetCombatEntity() => combatEntity;

    /// <summary>
    /// 获取动画组件
    /// </summary>
    public AnimationComponent GetAnimationComponent() => animationComponent;
}

