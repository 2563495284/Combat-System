using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Character3C
{
    /// <summary>
    /// 输入控制器 (2.5D)
    /// 处理玩家输入并更新到角色黑板
    /// 支持 Unity 新输入系统
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float inputDeadZone = 0.1f;

        [Header("角色引用")]
        [SerializeField] private Character25DController character;

#if ENABLE_INPUT_SYSTEM
        // 输入动作（使用新输入系统）
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private InputAction attackAction;
        private InputAction interactAction;
#endif

        // 输入缓存
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool dashPressed;
        private bool attackPressed;
        private bool interactPressed;

        private void Awake()
        {
            if (character == null)
            {
                character = GetComponent<Character25DController>();
            }

            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        /// <summary>
        /// 设置输入动作
        /// </summary>
        private void SetupInputActions()
        {
            // 使用传统输入方式作为后备
            // 如果项目使用了新输入系统，可以在这里绑定 InputActionAsset
        }

        /// <summary>
        /// 启用输入动作
        /// </summary>
        private void EnableInputActions()
        {
#if ENABLE_INPUT_SYSTEM
            moveAction?.Enable();
            jumpAction?.Enable();
            dashAction?.Enable();
            attackAction?.Enable();
            interactAction?.Enable();
#endif
        }

        /// <summary>
        /// 禁用输入动作
        /// </summary>
        private void DisableInputActions()
        {
#if ENABLE_INPUT_SYSTEM
            moveAction?.Disable();
            jumpAction?.Disable();
            dashAction?.Disable();
            attackAction?.Disable();
            interactAction?.Disable();
#endif
        }

        private void Update()
        {
            if (!enableInput || character == null)
                return;

            // 读取输入（使用传统输入系统）
            ReadInput();

            // 更新角色黑板
            UpdateCharacterBlackboard();

            // 处理输入指令
            HandleInputCommands();
        }

        /// <summary>
        /// 读取输入
        /// </summary>
        private void ReadInput()
        {
#if ENABLE_INPUT_SYSTEM
            // 移动输入（新输入系统优先）
            if (moveAction != null && moveAction.enabled)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
            else
#endif
            {
                // 传统输入系统
                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");
                moveInput = new Vector2(horizontal, vertical);
            }

            // 应用死区
            if (moveInput.magnitude < inputDeadZone)
            {
                moveInput = Vector2.zero;
            }

#if ENABLE_INPUT_SYSTEM
            // 跳跃输入
            if (jumpAction != null && jumpAction.enabled)
            {
                jumpPressed = jumpAction.WasPressedThisFrame();
            }
            else
#endif
            {
                jumpPressed = Input.GetButtonDown("Jump");
            }

#if ENABLE_INPUT_SYSTEM
            // 冲刺输入
            if (dashAction != null && dashAction.enabled)
            {
                dashPressed = dashAction.WasPressedThisFrame();
            }
            else
#endif
            {
                dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            }

#if ENABLE_INPUT_SYSTEM
            // 攻击输入
            if (attackAction != null && attackAction.enabled)
            {
                attackPressed = attackAction.WasPressedThisFrame();
            }
            else
#endif
            {
                attackPressed = Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.J);
            }

#if ENABLE_INPUT_SYSTEM
            // 交互输入
            if (interactAction != null && interactAction.enabled)
            {
                interactPressed = interactAction.WasPressedThisFrame();
            }
            else
#endif
            {
                interactPressed = Input.GetKeyDown(KeyCode.E);
            }
        }

        /// <summary>
        /// 更新角色黑板
        /// </summary>
        private void UpdateCharacterBlackboard()
        {
            var blackboard = character.Blackboard;

            blackboard.InputMove = moveInput;
            blackboard.InputJump = jumpPressed;
            blackboard.InputDash = dashPressed;
            blackboard.InputAttack = attackPressed;
            blackboard.InputInteract = interactPressed;
        }

        /// <summary>
        /// 处理输入指令
        /// </summary>
        private void HandleInputCommands()
        {
            // 跳跃
            if (jumpPressed)
            {
                character.Jump();
            }
        }

        /// <summary>
        /// 启用/禁用输入
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableInput = enabled;

            if (!enabled)
            {
                // 清空输入缓存
                moveInput = Vector2.zero;
                jumpPressed = false;
                dashPressed = false;
                attackPressed = false;
                interactPressed = false;

                // 同步清空黑板输入
                if (character != null)
                {
                    character.Blackboard.ResetInputFlags();
                    character.Blackboard.InputMove = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// 设置输入动作资源（新输入系统）
        /// 注意：需要安装 Input System 包并在 Project Settings 中启用
        /// </summary>
        public void SetupInputActionAsset(
#if ENABLE_INPUT_SYSTEM
            InputActionAsset inputActions
#else
            object inputActions
#endif
        )
        {
#if ENABLE_INPUT_SYSTEM
            if (inputActions == null) return;

            moveAction = inputActions.FindAction("Move");
            jumpAction = inputActions.FindAction("Jump");
            dashAction = inputActions.FindAction("Dash");
            attackAction = inputActions.FindAction("Attack");
            interactAction = inputActions.FindAction("Interact");

            EnableInputActions();
#else
            Debug.LogWarning("Input System 未启用。请安装 Input System 包并在 Project Settings → Player → Active Input Handling 中选择 'Both' 或 'Input System Package (New)'");
#endif
        }

        /// <summary>
        /// 获取当前移动输入
        /// </summary>
        public Vector2 GetMoveInput() => moveInput;

        /// <summary>
        /// 检查是否有移动输入
        /// </summary>
        public bool HasMoveInput() => moveInput.sqrMagnitude > 0;
    }
}

