using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面向摄像机组件（XY平面系统）
/// 使子对象始终面向摄像机，用于2.5D Sprite渲染
/// </summary>
public class FacingCamera : MonoBehaviour
{
    // 所有子对象（用于静态物体初始化）
    Transform[] allChilds;

    // 动态物体信息（需要每帧更新 order in layer）
    struct DynamicChildInfo
    {
        public Transform transform;
        public SpriteRenderer spriteRenderer;
        public BoxCollider2D boxCollider;
    }
    List<DynamicChildInfo> dynamicChilds;

    private Camera mainCamera;

    [Header("Order in Layer 设置")]
    [Tooltip("基础 Order in Layer 值")]
    public int baseOrderInLayer = 0;

    [Tooltip("Y 坐标每单位对应的 Order in Layer 增量（通常为负值，Y越大order越大）")]
    public float yToOrderMultiplier = -100f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("FacingCamera: Camera.main 未找到！");
            return;
        }

        int childCount = transform.childCount;
        allChilds = new Transform[childCount];
        dynamicChilds = new List<DynamicChildInfo>();

        Quaternion cameraRotation = mainCamera.transform.rotation;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            allChilds[i] = child;
            SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
            BoxCollider2D boxCollider = child.GetComponent<BoxCollider2D>();

            // 初始化旋转（只在 Start 时设置一次）
            child.rotation = cameraRotation;

            // 通过检查 Rigidbody2D 组件来判断是否为动态物体
            bool isDynamic = child.GetComponent<Rigidbody2D>() != null;

            // 初始化 order in layer
            if (spriteRenderer != null)
            {
                DynamicChildInfo childInfo = new DynamicChildInfo
                {
                    transform = child,
                    spriteRenderer = spriteRenderer,
                    boxCollider = boxCollider
                };
                int orderInLayer = CalculateOrderInLayer(childInfo);
                spriteRenderer.sortingOrder = orderInLayer;

                // 如果是动态物体，添加到动态列表
                if (isDynamic)
                {
                    dynamicChilds.Add(childInfo);
                }
            }
        }
    }

    void Update()
    {
        // 只更新动态物体的 order in layer
        for (int i = 0; i < dynamicChilds.Count; i++)
        {
            DynamicChildInfo childInfo = dynamicChilds[i];
            if (childInfo.transform != null && childInfo.spriteRenderer != null)
            {
                int orderInLayer = CalculateOrderInLayer(childInfo);
                childInfo.spriteRenderer.sortingOrder = orderInLayer;
            }
        }
    }

    /// <summary>
    /// 根据索引计算 Order in Layer
    /// </summary>
    private int CalculateOrderInLayer(DynamicChildInfo childInfo)
    {
        float bottomY;

        // 优先使用 BoxCollider2D 的底部
        if (childInfo.boxCollider != null)
        {
            bottomY = childInfo.boxCollider.bounds.min.y;
        }
        else
        {
            // 如果没有 BoxCollider2D，回退到使用 transform.position.y
            bottomY = childInfo.transform.position.y;
        }

        return baseOrderInLayer + Mathf.RoundToInt(bottomY * yToOrderMultiplier);
    }
}
