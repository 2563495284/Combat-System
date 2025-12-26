using UnityEngine;

namespace Character3C
{
    /// <summary>
    /// Billboard 组件
    /// 让2D Sprite始终面向摄像头
    /// 支持多种朝向模式
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            FaceCamera,           // 始终面向相机
            FaceCameraY,          // 仅Y轴旋转面向相机（常用于2.5D）
            FaceCameraForward,    // 面向相机的前方向
        }

        [Header("Billboard 设置")]
        [SerializeField] private BillboardMode mode = BillboardMode.FaceCamera;
        [SerializeField] private bool reverseDirection = false;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Header("朝向修正")]
        [Tooltip("如果Sprite背对摄像机，勾选此项")]
        [SerializeField] private bool flipForward = true;
        [Tooltip("添加额外的Y轴旋转（度数）")]
        [SerializeField] private float additionalYRotation = 0f;

        [Header("优化")]
        [SerializeField] private bool updateEveryFrame = true;
        [SerializeField] private float updateInterval = 0.1f;

        private Camera mainCamera;
        private Transform cameraTransform;
        private float nextUpdateTime;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                FindCamera();
                return;
            }

            // 检查是否需要更新
            if (!updateEveryFrame)
            {
                if (Time.time < nextUpdateTime)
                    return;
                nextUpdateTime = Time.time + updateInterval;
            }

            UpdateRotation();
        }

        /// <summary>
        /// 更新旋转
        /// </summary>
        private void UpdateRotation()
        {
            // 计算从Sprite到摄像机的方向
            Vector3 directionToCamera = cameraTransform.position - transform.position;

            // 根据flipForward决定是否翻转方向
            if (flipForward)
            {
                directionToCamera = -directionToCamera;
            }

            if (reverseDirection)
            {
                directionToCamera = -directionToCamera;
            }

            Quaternion targetRotation;

            switch (mode)
            {
                case BillboardMode.FaceCamera:
                    // 完全面向相机（包含上下角度）
                    if (directionToCamera.sqrMagnitude > 0.001f)
                    {
                        targetRotation = Quaternion.LookRotation(directionToCamera);
                    }
                    else
                    {
                        targetRotation = transform.rotation;
                    }
                    break;

                case BillboardMode.FaceCameraY:
                    // 仅Y轴旋转（2.5D常用）- 保持Sprite直立
                    directionToCamera.y = 0;
                    if (directionToCamera.sqrMagnitude > 0.001f)
                    {
                        targetRotation = Quaternion.LookRotation(directionToCamera);
                    }
                    else
                    {
                        targetRotation = transform.rotation;
                    }
                    break;

                case BillboardMode.FaceCameraForward:
                    // 使用相机的前方向（忽略相机位置）
                    Vector3 forward = cameraTransform.forward;
                    forward.y = 0;
                    if (forward.sqrMagnitude > 0.001f)
                    {
                        targetRotation = Quaternion.LookRotation(forward);
                    }
                    else
                    {
                        targetRotation = transform.rotation;
                    }
                    break;

                default:
                    targetRotation = transform.rotation;
                    break;
            }

            // // 应用额外的Y轴旋转
            // if (additionalYRotation != 0)
            // {
            //     targetRotation *= Quaternion.Euler(0, additionalYRotation, 0);
            // }

            // // 应用旋转偏移
            // if (rotationOffset != Vector3.zero)
            // {
            //     targetRotation *= Quaternion.Euler(rotationOffset);
            // }

            transform.rotation = targetRotation;
        }

        /// <summary>
        /// 查找相机
        /// </summary>
        private void FindCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        public void SetCamera(Camera camera)
        {
            mainCamera = camera;
            cameraTransform = camera != null ? camera.transform : null;
        }
    }
}

