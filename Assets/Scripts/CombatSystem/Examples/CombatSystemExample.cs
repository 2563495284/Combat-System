using CombatSystem.Attributes;
using CombatSystem.Configuration;
using CombatSystem.Core;
using CombatSystem.Utilities;
using UnityEngine;

namespace CombatSystem.Examples
{
    /// <summary>
    /// 战斗系统使用示例
    /// </summary>
    public class CombatSystemExample : MonoBehaviour
    {
        private CombatEntity _player;
        private CombatEntity _enemy;

        private void Start()
        {
            // 确保配置管理器已初始化
            var manager = StateCfgManager.Instance;

            // 创建玩家
            _player = CombatHelper.CreateEntity("Player", EntityType.Player, camp: 1);
            _player.transform.position = new Vector3(0, 0, 0);
            CombatHelper.SetEntityAttrs(_player, maxHp: 200, attack: 30, defense: 10, moveSpeed: 5f);

            // 创建敌人
            _enemy = CombatHelper.CreateEntity("Enemy", EntityType.Monster, camp: 2);
            _enemy.transform.position = new Vector3(5, 0, 0);
            CombatHelper.SetEntityAttrs(_enemy, maxHp: 150, attack: 20, defense: 5, moveSpeed: 3f);

            Debug.Log("=== 战斗系统示例启动 ===");
            Debug.Log("使用 Inspector 中的按钮或右键菜单来测试功能");
        }

        [ContextMenu("1 - 玩家普通攻击敌人")]
        public void Test_PlayerAttack()
        {
            Debug.Log("\n--- 玩家使用普通攻击 ---");
            CombatHelper.ApplySkill(_player, _enemy, 1001, damage: 25f);
        }

        [ContextMenu("2 - 玩家对敌人施加中毒")]
        public void Test_PlayerPoison()
        {
            Debug.Log("\n--- 玩家对敌人施加中毒 ---");
            CombatHelper.ApplyBuff(_player, _enemy, 2002, 1f, 8f);
        }

        [ContextMenu("3 - 玩家获得攻击力提升")]
        public void Test_PlayerAttackBuff()
        {
            Debug.Log("\n--- 玩家获得攻击力提升 ---");
            CombatHelper.ApplyBuff(_player, _player, 2001, 15f, false);
        }

        [ContextMenu("4 - 玩家施放治疗术")]
        public void Test_PlayerHeal()
        {
            Debug.Log("\n--- 玩家使用治疗术 ---");
            var healSkill = CombatHelper.ApplySkill(_player, _player, 1003);
            if (healSkill != null)
            {
                healSkill.Blackboard.Set("HealAmount", 40f);
            }
        }

        [ContextMenu("5 - 敌人眩晕玩家")]
        public void Test_EnemyStun()
        {
            Debug.Log("\n--- 敌人眩晕玩家 ---");
            CombatHelper.ApplyBuff(_enemy, _player, 2004);
        }

        [ContextMenu("6 - 玩家获得护盾")]
        public void Test_PlayerShield()
        {
            Debug.Log("\n--- 玩家获得护盾 ---");
            CombatHelper.ApplyBuff(_player, _player, 2005, 80f);
        }

        [ContextMenu("7 - 查看玩家状态")]
        public void Test_PrintPlayerStatus()
        {
            PrintEntityStatus(_player);
        }

        [ContextMenu("8 - 查看敌人状态")]
        public void Test_PrintEnemyStatus()
        {
            PrintEntityStatus(_enemy);
        }

        private void PrintEntityStatus(CombatEntity entity)
        {
            Debug.Log($"\n=== {entity.EntityName} 状态 ===");
            Debug.Log($"生命值: {entity.AttrComp.GetAttr(AttrType.Hp)}/{entity.AttrComp.GetAttr(AttrType.MaxHp)}");
            Debug.Log($"攻击力: {entity.AttrComp.GetAttr(AttrType.Attack)}");
            Debug.Log($"防御力: {entity.AttrComp.GetAttr(AttrType.Defense)}");

            var states = entity.StateComp.GetAllStates();
            Debug.Log($"当前状态数量: {states.Count}");
            foreach (var state in states)
            {
                Debug.Log($"  - {state.Cfg.name} (ID:{state.Cfg.cid}) 剩余:{state.TimeLeft}ms 激活:{state.Active}");
            }

            var mainState = entity.StateComp.GetMainState();
            Debug.Log($"主状态: {(mainState != null ? mainState.Cfg.name : "无")}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== 战斗系统示例 ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(10);
            GUILayout.Label("右键点击组件使用测试菜单");
            GUILayout.Label("或使用下方按钮：");

            if (GUILayout.Button("1 - 玩家普通攻击")) Test_PlayerAttack();
            if (GUILayout.Button("2 - 施加中毒")) Test_PlayerPoison();
            if (GUILayout.Button("3 - 攻击力提升")) Test_PlayerAttackBuff();
            if (GUILayout.Button("4 - 治疗术")) Test_PlayerHeal();
            if (GUILayout.Button("5 - 眩晕玩家")) Test_EnemyStun();
            if (GUILayout.Button("6 - 护盾")) Test_PlayerShield();
            if (GUILayout.Button("7 - 查看玩家状态")) Test_PrintPlayerStatus();
            if (GUILayout.Button("8 - 查看敌人状态")) Test_PrintEnemyStatus();

            if (_player != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"玩家 HP: {_player.AttrComp.GetAttr(AttrType.Hp):F0}/{_player.AttrComp.GetAttr(AttrType.MaxHp):F0}");
                GUILayout.Label($"玩家状态数: {_player.StateComp.GetAllStates().Count}");
            }

            if (_enemy != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"敌人 HP: {_enemy.AttrComp.GetAttr(AttrType.Hp):F0}/{_enemy.AttrComp.GetAttr(AttrType.MaxHp):F0}");
                GUILayout.Label($"敌人状态数: {_enemy.StateComp.GetAllStates().Count}");
            }

            GUILayout.EndArea();
        }
    }
}

