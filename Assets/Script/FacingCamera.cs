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
    
    void Start()
    {
        childs = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            childs[i] = transform.GetChild(i);
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
                childs[i].rotation = Camera.main.transform.rotation;
            }
        }
    }
}
