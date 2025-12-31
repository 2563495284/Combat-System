using System.Collections.Generic;
using System.Linq;
using CombatSystem.Configuration;
using CombatSystem.Core;
using UnityEngine;

namespace CombatSystem.Components
{
    /// <summary>
    /// 状态组件
    /// 管理实体身上的所有状态和状态槽
    /// </summary>
    public class StateComponent
    {
        /// <summary>
        /// 所有状态槽，按槽ID排序
        /// </summary>
        public readonly List<StateSlot> slots = new List<StateSlot>();

        /// <summary>
        /// 按状态激活顺序排序的状态槽
        /// </summary>
        public readonly List<StateSlot> activeSlots = new List<StateSlot>();

        /// <summary>
        /// 状态字典，按状态ID分类
        /// </summary>
        public readonly Dictionary<int, List<State>> stateDic = new Dictionary<int, List<State>>();

        /// <summary>
        /// 动态槽ID分配器
        /// </summary>
        private int _nextDynamicSlotId = StateSlot.MAX_STATIC_SLOT_ID + 1;

        /// <summary>
        /// 组件拥有者
        /// </summary>
        public CombatEntity Owner { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public StateComponent(CombatEntity owner)
        {
            Owner = owner;
            InitializeStaticSlots();
        }

        /// <summary>
        /// 初始化静态槽
        /// </summary>
        private void InitializeStaticSlots()
        {
            for (int i = 1; i <= StateSlot.MAX_STATIC_SLOT_ID; i++)
            {
                var slot = new StateSlot(i, true);
                slots.Add(slot);
            }
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        public State AddState(StateCfg cfg, CombatEntity caster = null, int level = 1)
        {
            if (cfg == null)
            {
                Debug.LogError("状态配置为空");
                return null;
            }

            // 检查是否已存在相同状态
            if (TryGetState(cfg.cid, out var existingState))
            {
                // 尝试叠加
                if (existingState.AddStack())
                {
                    existingState.RefreshDuration();
                    return existingState;
                }
                // 无法叠加，刷新持续时间
                existingState.RefreshDuration();
                return existingState;
            }

            // 创建新状态
            var state = new State(cfg)
            {
                Owner = Owner,
                Caster = caster ?? Owner,
                Level = level
            };

            // 分配状态槽
            if (!AllocateSlot(state))
            {
                Debug.LogWarning($"无法为状态分配槽: {cfg.name}");
                return null;
            }

            // 添加到字典
            if (!stateDic.ContainsKey(cfg.cid))
            {
                stateDic[cfg.cid] = new List<State>();
            }
            stateDic[cfg.cid].Add(state);

            // 启动状态
            state.Start();

            // 添加到活动列表
            if (!activeSlots.Contains(state.Slot))
            {
                activeSlots.Add(state.Slot);
            }

            Debug.Log($"添加状态: {state}");
            return state;
        }

        /// <summary>
        /// 分配状态槽
        /// </summary>
        private bool AllocateSlot(State state)
        {
            int slotId = state.Cfg.slot;

            if (slotId > 0)
            {
                // 指定槽
                var slot = GetSlot(slotId);
                if (slot == null)
                {
                    // 静态槽不存在，创建动态槽
                    slot = CreateDynamicSlot(slotId);
                }

                if (slot.CanBind(state))
                {
                    var oldState = slot.BindState(state);
                    if (oldState != null)
                    {
                        RemoveState(oldState);
                    }
                    return true;
                }
                return false;
            }
            else
            {
                // 自动分配槽
                var slot = CreateDynamicSlot();
                slot.BindState(state);
                return true;
            }
        }

        /// <summary>
        /// 获取状态槽
        /// </summary>
        public StateSlot GetSlot(int slotId)
        {
            return slots.Find(s => s.Id == slotId);
        }

        /// <summary>
        /// 创建动态槽
        /// </summary>
        private StateSlot CreateDynamicSlot(int slotId = -1)
        {
            if (slotId < 0)
            {
                slotId = _nextDynamicSlotId++;
            }

            var slot = new StateSlot(slotId, false);
            slots.Add(slot);
            slots.Sort((a, b) => a.Id.CompareTo(b.Id));
            return slot;
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        public bool RemoveState(State state)
        {
            if (state == null)
                return false;

            // 停止状态
            state.Stop();

            // 从字典移除
            if (stateDic.TryGetValue(state.Cfg.cid, out var list))
            {
                list.Remove(state);
                if (list.Count == 0)
                {
                    stateDic.Remove(state.Cfg.cid);
                }
            }

            // 从槽解绑
            if (state.Slot != null)
            {
                var slot = state.Slot; // 保存槽引用，因为 UnbindState 会将 state.Slot 设置为 null

                if (slot.State == state)
                {
                    slot.UnbindState();
                }

                // 如果槽为空，从活动列表移除
                if (slot.IsEmpty())
                {
                    activeSlots.Remove(slot);

                    // 移除动态空槽
                    if (!slot.IsStatic)
                    {
                        slots.Remove(slot);
                    }
                }
            }

            Debug.Log($"移除状态: {state}");
            return true;
        }

        /// <summary>
        /// 通过ID移除状态
        /// </summary>
        public bool RemoveStateById(int stateId)
        {
            if (TryGetState(stateId, out var state))
            {
                return RemoveState(state);
            }
            return false;
        }

        /// <summary>
        /// 尝试获取状态
        /// </summary>
        public bool TryGetState(int stateId, out State state)
        {
            if (stateDic.TryGetValue(stateId, out var list) && list.Count > 0)
            {
                state = list[0];
                return true;
            }
            state = null;
            return false;
        }

        /// <summary>
        /// 获取某ID的所有状态
        /// </summary>
        public List<State> GetStates(int stateId)
        {
            return stateDic.TryGetValue(stateId, out var list) ? list : new List<State>();
        }

        /// <summary>
        /// 获取所有状态
        /// </summary>
        public List<State> GetAllStates()
        {
            var result = new List<State>();
            foreach (var list in stateDic.Values)
            {
                result.AddRange(list);
            }
            return result;
        }

        /// <summary>
        /// 获取主状态（1号槽的状态）
        /// </summary>
        public State GetMainState()
        {
            var mainSlot = GetSlot(StaticSlotIds.MAIN_STATE);
            return mainSlot?.State;
        }

        /// <summary>
        /// 更新所有状态
        /// </summary>
        public void Update(int curFrame)
        {
            // 复制列表防止迭代中修改
            var slotsToUpdate = new List<StateSlot>(activeSlots);

            foreach (var slot in slotsToUpdate)
            {
                if (slot.State != null && slot.State.Active)
                {
                    slot.State.Update(curFrame);

                    // 检查是否过期
                    if (slot.State.IsExpired())
                    {
                        RemoveState(slot.State);
                    }
                }
            }
        }

        /// <summary>
        /// 广播事件到所有活动状态
        /// </summary>
        public void BroadcastEvent(object evt)
        {
            foreach (var slot in activeSlots)
            {
                slot.State?.OnEvent(evt);
            }
        }

        /// <summary>
        /// 清除所有状态
        /// </summary>
        public void Clear()
        {
            var allStates = GetAllStates();
            foreach (var state in allStates)
            {
                RemoveState(state);
            }

            stateDic.Clear();
            activeSlots.Clear();

            // 保留静态槽
            slots.RemoveAll(s => !s.IsStatic);
            _nextDynamicSlotId = StateSlot.MAX_STATIC_SLOT_ID + 1;
        }
    }
}

