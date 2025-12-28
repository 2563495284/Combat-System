using UnityEngine;
using CombatSystem.Core;
using CombatSystem.Attributes;

namespace Character3C.Enemy
{
    /// <summary>
    /// 敌人测试辅助工具
    /// 提供快速测试和调试敌人系统的功能
    /// </summary>
    public class EnemyTestHelper : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private KeyCode spawnKey = KeyCode.E;
        [SerializeField] private KeyCode killAllKey = KeyCode.K;
        [SerializeField] private float spawnDistance = 5f;
        
        [Header("敌人模板")]
        [SerializeField] private EnemyTemplate[] templates;
        [SerializeField] private int currentTemplateIndex = 0;
        
        private Transform playerTransform;
        
        private void Start()
        {
            // 查找玩家
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        private void Update()
        {
            // 生成敌人
            if (Input.GetKeyDown(spawnKey))
            {
                SpawnTestEnemy();
            }
            
            // 杀死所有敌人
            if (Input.GetKeyDown(killAllKey))
            {
                KillAllEnemies();
            }
            
            // 切换模板
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentTemplateIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentTemplateIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentTemplateIndex = 2;
        }
        
        /// <summary>
        /// 生成测试敌人
        /// </summary>
        private void SpawnTestEnemy()
        {
            Vector3 spawnPos;
            
            if (playerTransform != null)
            {
                // 在玩家前方生成
                spawnPos = playerTransform.position + playerTransform.forward * spawnDistance;
            }
            else
            {
                // 在当前位置生成
                spawnPos = transform.position;
            }
            
            // 使用当前模板
            if (templates != null && templates.Length > 0)
            {
                int index = Mathf.Clamp(currentTemplateIndex, 0, templates.Length - 1);
                EnemyTemplate template = templates[index];
                
                GameObject enemy = CreateEnemyFromTemplate(template, spawnPos);
                Debug.Log($"生成测试敌人: {enemy.name} (模板: {template.name})");
            }
            else
            {
                Debug.LogWarning("没有配置敌人模板");
            }
        }
        
        /// <summary>
        /// 从模板创建敌人
        /// </summary>
        private GameObject CreateEnemyFromTemplate(EnemyTemplate template, Vector3 position)
        {
            GameObject enemyObj = new GameObject(template.name);
            enemyObj.transform.position = position;
            
            // 添加基础组件
            var rb = enemyObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            var collider = enemyObj.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1f, 0);
            
            // 添加战斗实体
            var combatEntity = enemyObj.AddComponent<CombatEntity>();
            combatEntity.EntityName = template.name;
            combatEntity.EntityType = CombatSystem.EntityType.Monster;
            combatEntity.Camp = (int)CombatSystem.CampType.Enemy;
            
            // 配置属性
            combatEntity.AttrComp.SetBaseAttr(AttrType.Hp, template.maxHealth);
            combatEntity.AttrComp.SetBaseAttr(AttrType.Attack, template.attack);
            combatEntity.AttrComp.SetBaseAttr(AttrType.Defense, template.defense);
            combatEntity.AttrComp.SetBaseAttr(AttrType.MoveSpeed, template.moveSpeed);
            combatEntity.AttrComp.ModifyCurrentValue(AttrType.Hp, template.maxHealth);
            
            // 添加控制器（会自动配置）
            var controller = enemyObj.AddComponent<Enemy25DController>();
            
            // 创建简单视觉
            CreateSimpleVisual(enemyObj, template.color);
            
            // 设置层级
            enemyObj.layer = LayerMask.NameToLayer("Enemy");
            
            return enemyObj;
        }
        
        /// <summary>
        /// 创建简单视觉
        /// </summary>
        private void CreateSimpleVisual(GameObject parent, Color color)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            Destroy(body.GetComponent<Collider>());
            
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            
            // 创建眼睛
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(body.transform);
            eye.transform.localPosition = new Vector3(0, 0.3f, 0.4f);
            eye.transform.localScale = Vector3.one * 0.2f;
            Destroy(eye.GetComponent<Collider>());
            
            var eyeRenderer = eye.GetComponent<Renderer>();
            if (eyeRenderer != null)
            {
                eyeRenderer.material.color = Color.yellow;
            }
        }
        
        /// <summary>
        /// 杀死所有敌人
        /// </summary>
        private void KillAllEnemies()
        {
            var enemies = FindObjectsByType<Enemy25DController>(FindObjectsSortMode.None);
            int count = enemies.Length;
            
            foreach (var enemy in enemies)
            {
                var combatEntity = enemy.GetComponent<CombatEntity>();
                if (combatEntity != null)
                {
                    // 造成致命伤害
                    combatEntity.DealDamage(combatEntity, 999999f);
                }
            }
            
            Debug.Log($"杀死了 {count} 个敌人");
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== 敌人测试工具 ===");
            GUILayout.Label($"按 {spawnKey} 生成敌人");
            GUILayout.Label($"按 {killAllKey} 杀死所有敌人");
            GUILayout.Label("按 1/2/3 切换敌人模板");
            
            if (templates != null && templates.Length > 0)
            {
                int index = Mathf.Clamp(currentTemplateIndex, 0, templates.Length - 1);
                GUILayout.Label($"当前模板: {templates[index].name}");
            }
            
            var enemies = FindObjectsByType<Enemy25DController>(FindObjectsSortMode.None);
            GUILayout.Label($"当前敌人数量: {enemies.Length}");
            
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// 敌人模板
    /// </summary>
    [System.Serializable]
    public class EnemyTemplate
    {
        public string name = "Slime";
        public Color color = Color.red;
        
        [Header("属性")]
        public float maxHealth = 100f;
        public float attack = 20f;
        public float defense = 5f;
        public float moveSpeed = 3f;
        
        [Header("AI")]
        public float detectionRadius = 10f;
        public float attackRadius = 2f;
        public float attackDamage = 15f;
    }
}

