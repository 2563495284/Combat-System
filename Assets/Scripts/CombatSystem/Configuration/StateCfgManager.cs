using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem.Configuration
{
    /// <summary>
    /// 状态配置管理器
    /// 管理所有状态、技能、Buff的配置
    /// </summary>
    public class StateCfgManager : MonoBehaviour
    {
        private static StateCfgManager _instance;
        public static StateCfgManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StateCfgManager");
                    _instance = go.AddComponent<StateCfgManager>();
                    DontDestroyOnLoad(go);
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 配置字典
        /// </summary>
        private readonly Dictionary<int, StateCfg> _configs = new Dictionary<int, StateCfg>();

        private void Initialize()
        {
            // 初始化默认配置
            InitializeDefaultConfigs();
        }

        /// <summary>
        /// 初始化默认配置（示例）
        /// </summary>
        private void InitializeDefaultConfigs()
        {
            // 技能：简单攻击
            AddConfig(new StateCfg
            {
                cid = 1001,
                name = "普通攻击",
                slot = -1,
                duration = 1000,
                isActiveSkill = true,
                taskTypeName = "CombatSystem.Skills.SimpleAttackSkill"
            });

            // 技能：AOE攻击
            AddConfig(new StateCfg
            {
                cid = 1002,
                name = "范围攻击",
                slot = -1,
                duration = 1500,
                isActiveSkill = true,
                taskTypeName = "CombatSystem.Skills.AoeSkill"
            });

            // 技能：治疗
            AddConfig(new StateCfg
            {
                cid = 1003,
                name = "治疗术",
                slot = -1,
                duration = 1000,
                isActiveSkill = true,
                taskTypeName = "CombatSystem.Skills.HealSkill"
            });

            // 技能：冲刺
            AddConfig(new StateCfg
            {
                cid = 1004,
                name = "冲刺",
                slot = -1,
                duration = 500,
                isActiveSkill = true,
                taskTypeName = "CombatSystem.Skills.DashSkill"
            });

            // 技能：击退
            AddConfig(new StateCfg
            {
                cid = 1005,
                name = "击退",
                slot = -1,
                duration = 800,
                isActiveSkill = true,
                taskTypeName = "CombatSystem.Skills.KnockbackSkill"
            });

            // Buff：攻击力加成
            AddConfig(new StateCfg
            {
                cid = 2001,
                name = "攻击力提升",
                slot = -1,
                duration = 5000,
                isBuff = true,
                taskTypeName = "CombatSystem.Buffs.AttrBuffTask"
            });

            // Buff：持续伤害
            AddConfig(new StateCfg
            {
                cid = 2002,
                name = "中毒",
                slot = -1,
                duration = 5000,
                isBuff = true,
                canDispel = true,
                taskTypeName = "CombatSystem.Buffs.DotBuffTask"
            });

            // Buff：持续治疗
            AddConfig(new StateCfg
            {
                cid = 2003,
                name = "回春术",
                slot = -1,
                duration = 5000,
                isBuff = true,
                taskTypeName = "CombatSystem.Buffs.HotBuffTask"
            });

            // Buff：眩晕
            AddConfig(new StateCfg
            {
                cid = 2004,
                name = "眩晕",
                slot = Core.StaticSlotIds.MAIN_STATE,
                duration = 2000,
                isBuff = true,
                priority = 100,
                taskTypeName = "CombatSystem.Buffs.StunBuffTask"
            });

            // Buff：护盾
            AddConfig(new StateCfg
            {
                cid = 2005,
                name = "护盾",
                slot = -1,
                duration = 10000,
                isBuff = true,
                taskTypeName = "CombatSystem.Buffs.ShieldBuffTask"
            });

            Debug.Log($"[StateCfgManager] 加载了 {_configs.Count} 个配置");
        }

        /// <summary>
        /// 添加配置
        /// </summary>
        public void AddConfig(StateCfg cfg)
        {
            if (cfg == null)
            {
                Debug.LogError("[StateCfgManager] 配置为空");
                return;
            }

            _configs[cfg.cid] = cfg;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        public StateCfg GetConfig(int cid)
        {
            return _configs.TryGetValue(cid, out var cfg) ? cfg : null;
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        public bool RemoveConfig(int cid)
        {
            return _configs.Remove(cid);
        }

        /// <summary>
        /// 获取所有配置
        /// </summary>
        public Dictionary<int, StateCfg> GetAllConfigs()
        {
            return new Dictionary<int, StateCfg>(_configs);
        }

        /// <summary>
        /// 从JSON加载配置
        /// </summary>
        public void LoadFromJson(string json)
        {
            try
            {
                var configs = JsonUtility.FromJson<StateCfgList>(json);
                if (configs != null && configs.configs != null)
                {
                    foreach (var cfg in configs.configs)
                    {
                        AddConfig(cfg);
                    }
                    Debug.Log($"[StateCfgManager] 从JSON加载了 {configs.configs.Length} 个配置");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StateCfgManager] 加载JSON失败: {e.Message}");
            }
        }

        /// <summary>
        /// 导出为JSON
        /// </summary>
        public string ExportToJson()
        {
            var list = new List<StateCfg>(_configs.Values);
            var wrapper = new StateCfgList { configs = list.ToArray() };
            return JsonUtility.ToJson(wrapper, true);
        }

        [System.Serializable]
        private class StateCfgList
        {
            public StateCfg[] configs;
        }
    }
}

