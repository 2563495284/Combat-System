using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面向摄像机组件（XY平面系统）
/// 使子对象始终面向摄像机，用于2.5D Sprite渲染
/// </summary>
public class FacingCamera : MonoBehaviour
{
    Transform[] childs;
    SpriteRenderer[] spriteRenderers;
    BoxCollider2D[] boxColliders; // BoxCollider2D 组件数组
    bool[] isDynamic; // 标记每个子对象是否为动态物体
    
    [Header("Order in Layer 设置")]
    [Tooltip("基础 Order in Layer 值")]
    public int baseOrderInLayer = 0;
    
    [Tooltip("Y 坐标每单位对应的 Order in Layer 增量（通常为负值，Y越大order越大）")]
    public float yToOrderMultiplier = -100f;
    
    void Start()
    {
        int childCount = transform.childCount;
        childs = new Transform[childCount];
        spriteRenderers = new SpriteRenderer[childCount];
        boxColliders = new BoxCollider2D[childCount];
        isDynamic = new bool[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            childs[i] = transform.GetChild(i);
            spriteRenderers[i] = childs[i].GetComponent<SpriteRenderer>();
            boxColliders[i] = childs[i].GetComponent<BoxCollider2D>();
            
            // 通过检查 Rigidbody2D 或 Rigidbody 组件来判断是否为动态物体
            isDynamic[i] = childs[i].GetComponent<Rigidbody2D>() != null;
            
            // 静态物体：初始化 order in layer
            if (!isDynamic[i] && spriteRenderers[i] != null)
            {
                int orderInLayer = CalculateOrderInLayer(i);
                spriteRenderers[i].sortingOrder = orderInLayer;
            }
        }
    }

    void Update()
    {
        if (Camera.main == null) return;
        
        // 在XY平面系统中，Sprite应该面向摄像机
        // 摄像机从Z轴方向45度俯视，所以Sprite需要面向摄像机
        for (int i = 0; i < childs.Length; i++)
        {
            if (childs[i] != null)
            {
                // 更新旋转
                childs[i].rotation = Camera.main.transform.rotation;
                
                // 动态物体：动态更新 order in layer
                if (isDynamic[i] && spriteRenderers[i] != null)
                {
                    int orderInLayer = CalculateOrderInLayer(i);
                    spriteRenderers[i].sortingOrder = orderInLayer;
                }
            }
        }
    }
    
    /// <summary>
    /// 根据 BoxCollider2D 的底部 Y 坐标计算 Order in Layer
    /// </summary>
    /// <param name="index">子对象索引</param>
    /// <returns>Order in Layer 值</returns>
    private int CalculateOrderInLayer(int index)
    {
        float bottomY;
        
        // 优先使用 BoxCollider2D 的底部
        if (boxColliders[index] != null)
        {
            bottomY = boxColliders[index].bounds.min.y;
        }
        else
        {
            // 如果没有 BoxCollider2D，回退到使用 transform.position.y
            bottomY = childs[index].position.y;
        }
        
        return baseOrderInLayer + Mathf.RoundToInt(bottomY * yToOrderMultiplier);
    }
}
