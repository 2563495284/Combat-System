using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 旋转摄像机组件（XY平面系统）
/// 在XY平面系统中，摄像机绕Z轴旋转（深度轴）
/// </summary>
public class RotatingCamera : MonoBehaviour
{
    public float rotateTime = 0.2f;
    private Transform player;
    private bool isRotating = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning("RotatingCamera: 未找到Player标签的对象");
        }
    }

    void Update()
    {
        if (player == null) return;
        
        // 在XY平面系统中，摄像机跟随玩家位置
        transform.position = player.position;

        Rotate();
    }

    void Rotate()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isRotating)
        {
            StartCoroutine(RotateAround(-45, rotateTime));
        }
        if (Input.GetKeyDown(KeyCode.E) && !isRotating)
        {
            StartCoroutine(RotateAround(45, rotateTime));
        }
    }

    IEnumerator RotateAround(float angle, float time)
    {
        float number = 60 * time;
        float nextAngle = angle / number;
        isRotating = true;

        // 在XY平面系统中，绕Z轴旋转（深度轴）
        for (int i = 0; i < number; i++)
        {
            transform.Rotate(new Vector3(0, 0, nextAngle));
            yield return new WaitForFixedUpdate();
        }

        isRotating = false;
    }
}
