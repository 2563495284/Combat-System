namespace CombatSystem.Core
{
    /// <summary>
    /// 状态槽
    /// 用于管理状态的互斥关系和提供查询接口
    /// </summary>
    public class StateSlot
    {
        /// <summary>
        /// 状态槽ID
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 当前槽上的状态
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// 任务缓存（非必须）
        /// </summary>
        public TaskEntry Task => State?.Task;

        /// <summary>
        /// 是否是静态槽
        /// 静态槽在对象创建时预分配
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// 静态槽的最大ID（可配置）
        /// </summary>
        public const int MAX_STATIC_SLOT_ID = 5;

        /// <summary>
        /// 构造函数
        /// </summary>
        public StateSlot(int id, bool isStatic = false)
        {
            Id = id;
            IsStatic = isStatic || (id <= MAX_STATIC_SLOT_ID);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty()
        {
            return State == null;
        }

        /// <summary>
        /// 绑定状态到槽
        /// </summary>
        /// <returns>被挤掉的旧状态（如果有）</returns>
        public State BindState(State newState)
        {
            State oldState = State;

            // 移除旧状态
            if (oldState != null)
            {
                oldState.Slot = null;
                oldState.Stop();
            }

            // 绑定新状态
            State = newState;
            if (newState != null)
            {
                newState.Slot = this;
            }

            return oldState;
        }

        /// <summary>
        /// 解绑状态
        /// </summary>
        public void UnbindState()
        {
            if (State != null)
            {
                State.Slot = null;
                State.Stop();
                State = null;
            }
        }

        /// <summary>
        /// 检查是否可以绑定新状态
        /// </summary>
        public bool CanBind(State newState)
        {
            if (IsEmpty())
                return true;

            // 比较优先级
            return newState.Cfg.priority >= State.Cfg.priority;
        }

        public override string ToString()
        {
            return $"StateSlot[{Id}] Static:{IsStatic} State:{(State != null ? State.Cfg.name : "Empty")}";
        }
    }

    /// <summary>
    /// 预定义的静态槽ID
    /// </summary>
    public static class StaticSlotIds
    {
        /// <summary>
        /// 主状态槽（类似主状态机）
        /// 用于跳跃、死亡、冰冻等互斥状态
        /// </summary>
        public const int MAIN_STATE = 1;

        /// <summary>
        /// 移动状态槽
        /// </summary>
        public const int MOVEMENT = 2;

        /// <summary>
        /// 攻击状态槽
        /// </summary>
        public const int ATTACK = 3;

        /// <summary>
        /// 防御状态槽
        /// </summary>
        public const int DEFENSE = 4;

        /// <summary>
        /// 特殊状态槽
        /// </summary>
        public const int SPECIAL = 5;
    }
}

