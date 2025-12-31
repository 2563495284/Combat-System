// using UnityEngine;
// using CombatSystem.Core;
// using CombatSystem.Attributes;

//     /// <summary>
//     /// 敌人生成器
//     /// 用于在场景中生成和配置敌人
//     /// </summary>
//     public class EnemySpawner : MonoBehaviour
//     {
//         [Header("敌人配置")]
//         [SerializeField] private string enemyName = "Slime";
//         [SerializeField] private GameObject enemyVisualPrefab; // 敌人的视觉模型（可选）
        
//         [Header("属性配置")]
//         [SerializeField] private float maxHealth = 100f;
//         [SerializeField] private float attack = 20f;
//         [SerializeField] private float defense = 5f;
//         [SerializeField] private float moveSpeed = 3f;
        
//         [Header("AI配置")]
//         [SerializeField] private float detectionRadius = 10f;
//         [SerializeField] private float attackRadius = 2f;
//         [SerializeField] private float attackDamage = 15f;
//         [SerializeField] private float attackCooldown = 2f;
        
//         [Header("巡逻配置")]
//         [SerializeField] private bool enablePatrol = true;
//         [SerializeField] private float patrolRadius = 5f;
        
//         [Header("生成设置")]
//         [SerializeField] private bool spawnOnStart = true;
//         [SerializeField] private int spawnCount = 1;
//         [SerializeField] private float spawnInterval = 0.5f;
        
//         private void Start()
//         {
//             if (spawnOnStart)
//             {
//                 StartCoroutine(SpawnEnemiesCoroutine());
//             }
//         }
        
//         /// <summary>
//         /// 生成敌人协程
//         /// </summary>
//         private System.Collections.IEnumerator SpawnEnemiesCoroutine()
//         {
//             for (int i = 0; i < spawnCount; i++)
//             {
//                 Vector3 spawnPos = transform.position + new Vector3(
//                     Random.Range(-2f, 2f), 
//                     0, 
//                     Random.Range(-2f, 2f)
//                 );
                
//                 SpawnEnemy(spawnPos);
                
//                 if (i < spawnCount - 1)
//                 {
//                     yield return new WaitForSeconds(spawnInterval);
//                 }
//             }
//         }
        
//         /// <summary>
//         /// 生成单个敌人
//         /// </summary>
//         public GameObject SpawnEnemy(Vector3 position)
//         {
//             // 创建敌人GameObject
//             GameObject enemyObj = new GameObject($"{enemyName}_{Random.Range(1000, 9999)}");
//             enemyObj.transform.position = position;
            
//             // 添加必要的组件
//             SetupComponents(enemyObj);
            
//             // 配置属性
//             ConfigureAttributes(enemyObj);
            
//             // 添加视觉模型
//             if (enemyVisualPrefab != null)
//             {
//                 GameObject visual = Instantiate(enemyVisualPrefab, enemyObj.transform);
//                 visual.transform.localPosition = Vector3.zero;
//             }
//             else
//             {
//                 // 如果没有预制体，创建简单的立方体表示
//                 CreateDefaultVisual(enemyObj);
//             }
            
//             Debug.Log($"生成敌人: {enemyObj.name} 在位置 {position}");
            
//             return enemyObj;
//         }
        
//         /// <summary>
//         /// 设置组件
//         /// </summary>
//         private void SetupComponents(GameObject enemyObj)
//         {
//             // 添加Rigidbody
//             var rb = enemyObj.AddComponent<Rigidbody>();
//             rb.mass = 1f;
//             rb.linearDamping = 0.5f;
//             rb.angularDamping = 0.5f;
//             rb.constraints = RigidbodyConstraints.FreezeRotation;
            
//             // 添加碰撞体
//             var collider = enemyObj.AddComponent<CapsuleCollider>();
//             collider.height = 2f;
//             collider.radius = 0.5f;
//             collider.center = new Vector3(0, 1f, 0);
            
//             // 添加战斗实体
//             var combatEntity = enemyObj.AddComponent<CombatEntity>();
//             combatEntity.EntityName = enemyObj.name;
//             combatEntity.EntityType = CombatSystem.EntityType.Monster;
//             combatEntity.Camp = (int)CombatSystem.CampType.Enemy;
            
//             // 添加敌人控制器
//             var controller = enemyObj.AddComponent<Enemy25DController>();
            
//             // 设置层级
//             enemyObj.layer = LayerMask.NameToLayer("Enemy");
//             if (enemyObj.layer == -1)
//             {
//                 Debug.LogWarning("未找到 'Enemy' 层，使用默认层");
//             }
//         }
        
//         /// <summary>
//         /// 配置属性
//         /// </summary>
//         private void ConfigureAttributes(GameObject enemyObj)
//         {
//             var combatEntity = enemyObj.GetComponent<CombatEntity>();
//             if (combatEntity != null)
//             {
//                 // 设置基础属性
//                 combatEntity.AttrComp.SetBaseAttr(AttrType.Hp, maxHealth);
//                 combatEntity.AttrComp.SetBaseAttr(AttrType.Attack, attack);
//                 combatEntity.AttrComp.SetBaseAttr(AttrType.Defense, defense);
//                 combatEntity.AttrComp.SetBaseAttr(AttrType.MoveSpeed, moveSpeed);
                
//                 // 初始化当前生命值
//                 combatEntity.AttrComp.ModifyCurrentValue(AttrType.Hp, maxHealth);
//             }
//         }
        
//         /// <summary>
//         /// 创建默认视觉表现
//         /// </summary>
//         private void CreateDefaultVisual(GameObject enemyObj)
//         {
//             // 创建身体
//             GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
//             body.name = "Body";
//             body.transform.SetParent(enemyObj.transform);
//             body.transform.localPosition = new Vector3(0, 1f, 0);
//             body.transform.localScale = Vector3.one;
            
//             // 移除碰撞体（主碰撞体在父对象上）
//             Destroy(body.GetComponent<Collider>());
            
//             // 设置颜色
//             var renderer = body.GetComponent<Renderer>();
//             if (renderer != null)
//             {
//                 renderer.material.color = new Color(0.8f, 0.2f, 0.2f); // 红色
//             }
            
//             // 创建眼睛标识方向
//             GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//             eye.name = "Eye";
//             eye.transform.SetParent(body.transform);
//             eye.transform.localPosition = new Vector3(0, 0.3f, 0.4f);
//             eye.transform.localScale = Vector3.one * 0.2f;
            
//             // 移除碰撞体
//             Destroy(eye.GetComponent<Collider>());
            
//             // 设置眼睛颜色
//             var eyeRenderer = eye.GetComponent<Renderer>();
//             if (eyeRenderer != null)
//             {
//                 eyeRenderer.material.color = Color.yellow;
//             }
//         }
        
//         /// <summary>
//         /// 绘制 Gizmos
//         /// </summary>
//         private void OnDrawGizmos()
//         {
//             // 绘制生成位置
//             Gizmos.color = Color.red;
//             Gizmos.DrawWireSphere(transform.position, 0.5f);
            
//             // 绘制检测范围
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
//             // 绘制巡逻范围
//             if (enablePatrol)
//             {
//                 Gizmos.color = Color.cyan;
//                 Gizmos.DrawWireSphere(transform.position, patrolRadius);
    // }
// }

