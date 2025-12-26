using UnityEngine;

namespace Character3C
{
    /// <summary>
    /// 2.5D 相机控制器
    /// 支持等角视角和自由视角
    /// </summary>
    public class Camera25DController : MonoBehaviour
    {
        public enum CameraMode
        {
            Isometric,      // 等角视角（固定45度）
            TopDown,        // 俯视视角
            ThirdPerson,    // 第三人称视角
            Fixed,          // 固定位置
        }

        [Header("相机模式")]
        [SerializeField] private CameraMode mode = CameraMode.Isometric;

        [Header("跟随设置")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool followZ = true;

        [Header("等角视角设置")]
        [SerializeField] private float isometricAngle = 30f;
        [SerializeField] private float isometricHeight = 10f;
        [SerializeField] private float isometricDistance = 10f;

        [Header("边界限制")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector3 minBounds = new Vector3(-50, 0, -50);
        [SerializeField] private Vector3 maxBounds = new Vector3(50, 20, 50);

        [Header("旋转控制")]
        [SerializeField] private bool allowRotation = false;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;

        private Vector3 targetPosition;
        private Vector3 velocity;
        private float currentRotationAngle = 0f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();

            // 根据模式设置初始相机参数
            SetupCameraForMode();
        }

        private void Start()
        {
            if (target != null)
            {
                // 立即移动到目标位置
                UpdateTargetPosition();
                transform.position = targetPosition;

                // 立即朝向目标
                UpdateCameraRotation();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 处理相机旋转
            if (allowRotation)
            {
                HandleRotation();
            }
            else
            {
                // 非旋转模式下，确保相机始终朝向目标
                UpdateCameraRotation();
            }

            // 更新目标位置
            UpdateTargetPosition();

            // 平滑移动到目标位置
            MoveToTarget();
        }

        /// <summary>
        /// 更新相机朝向目标
        /// </summary>
        private void UpdateCameraRotation()
        {
            if (target == null) return;

            // 根据不同模式使用不同的朝向策略
            switch (mode)
            {
                case CameraMode.Isometric:
                case CameraMode.TopDown:
                case CameraMode.ThirdPerson:
                    // 让相机朝向目标
                    Vector3 direction = target.position - transform.position;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = targetRotation;
                    }
                    break;

                case CameraMode.Fixed:
                    // 固定模式不改变旋转
                    break;
            }
        }

        /// <summary>
        /// 设置相机模式
        /// </summary>
        private void SetupCameraForMode()
        {
            switch (mode)
            {
                case CameraMode.Isometric:
                    // 等角视角：从斜上方俯视
                    // 计算相机的旋转和位置
                    float angleRad = isometricAngle * Mathf.Deg2Rad;
                    float horizontalDistance = isometricDistance * Mathf.Cos(angleRad);

                    // 设置偏移：相机在目标的右后上方
                    offset = new Vector3(
                        horizontalDistance * Mathf.Sin(45 * Mathf.Deg2Rad),  // X偏移
                        isometricHeight,                                      // Y偏移（高度）
                        -horizontalDistance * Mathf.Cos(45 * Mathf.Deg2Rad)  // Z偏移
                    );

                    // 设置旋转：俯视角度 + 45度水平旋转
                    transform.rotation = Quaternion.Euler(isometricAngle, 45, 0);

                    // 设置正交投影
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;

                    Debug.Log($"等角视角已设置 - Rotation: {transform.rotation.eulerAngles}, Offset: {offset}", this);
                    break;

                case CameraMode.TopDown:
                    // 俯视视角：正上方看下来
                    transform.rotation = Quaternion.Euler(90, 0, 0);
                    offset = new Vector3(0, 15, 0);
                    cam.orthographic = true;
                    cam.orthographicSize = 8f;

                    Debug.Log($"俯视视角已设置 - Rotation: {transform.rotation.eulerAngles}, Offset: {offset}", this);
                    break;

                case CameraMode.ThirdPerson:
                    // 第三人称视角：角色后方
                    transform.rotation = Quaternion.Euler(20, 0, 0);
                    offset = new Vector3(0, 5, -8);
                    cam.orthographic = false;
                    cam.fieldOfView = 60f;

                    Debug.Log($"第三人称视角已设置 - Rotation: {transform.rotation.eulerAngles}, Offset: {offset}", this);
                    break;

                case CameraMode.Fixed:
                    // 固定相机：不跟随
                    followX = false;
                    followY = false;
                    followZ = false;

                    Debug.Log("固定视角已设置", this);
                    break;
            }
        }

        /// <summary>
        /// 处理相机旋转
        /// </summary>
        private void HandleRotation()
        {
            if (Input.GetKey(rotateLeftKey))
            {
                currentRotationAngle += rotationSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(rotateRightKey))
            {
                currentRotationAngle -= rotationSpeed * Time.deltaTime;
            }

            // 应用旋转到偏移
            Quaternion rotation = Quaternion.Euler(0, currentRotationAngle, 0);
            Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, offset.z);
            offset = rotatedOffset;

            // 旋转相机朝向
            Vector3 lookDir = target.position - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
            }
        }

        /// <summary>
        /// 更新目标位置
        /// </summary>
        private void UpdateTargetPosition()
        {
            Vector3 desiredPosition = target.position + offset;

            // 应用轴向限制
            if (!followX) desiredPosition.x = transform.position.x;
            if (!followY) desiredPosition.y = transform.position.y;
            if (!followZ) desiredPosition.z = transform.position.z;

            // 应用边界限制
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.z, maxBounds.z);
            }

            targetPosition = desiredPosition;
        }

        /// <summary>
        /// 移动到目标位置
        /// </summary>
        private void MoveToTarget()
        {
            if (smoothSpeed > 0)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref velocity,
                    1f / smoothSpeed
                );
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        /// <summary>
        /// 设置跟随目标
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                UpdateTargetPosition();
                transform.position = targetPosition;
                UpdateCameraRotation();
            }
        }

        /// <summary>
        /// 设置相机模式
        /// </summary>
        public void SetMode(CameraMode newMode)
        {
            mode = newMode;
            SetupCameraForMode();
        }

        /// <summary>
        /// 立即移动到目标
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                UpdateTargetPosition();
                transform.position = targetPosition;
                velocity = Vector3.zero;
                UpdateCameraRotation();
            }
        }

        /// <summary>
        /// 强制重新应用相机设置（编辑器辅助方法）
        /// </summary>
        [ContextMenu("重新应用相机设置")]
        public void ReapplyCameraSettings()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("未找到Camera组件！", this);
                return;
            }

            SetupCameraForMode();

            if (target != null)
            {
                SnapToTarget();
            }

            Debug.Log($"✓ 相机设置已重新应用\n" +
                     $"- 模式: {mode}\n" +
                     $"- 投影: {(cam.orthographic ? "正交" : "透视")}\n" +
                     $"- 旋转: {transform.rotation.eulerAngles}\n" +
                     $"- 偏移: {offset}", this);
        }

        /// <summary>
        /// 验证相机设置（编辑器辅助方法）
        /// </summary>
        [ContextMenu("验证相机设置")]
        public void ValidateCameraSettings()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("❌ 未找到Camera组件！", this);
                return;
            }

            bool isValid = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 相机设置检查 ===");

            // 检查投影模式
            if (mode != CameraMode.ThirdPerson && !cam.orthographic)
            {
                sb.AppendLine($"❌ 投影模式错误：当前为透视投影，应该是正交投影");
                isValid = false;
            }
            else if (mode == CameraMode.ThirdPerson && cam.orthographic)
            {
                sb.AppendLine($"❌ 投影模式错误：当前为正交投影，应该是透视投影");
                isValid = false;
            }
            else
            {
                sb.AppendLine($"✓ 投影模式正确：{(cam.orthographic ? "正交" : "透视")}");
            }

            // 检查目标
            if (target == null)
            {
                sb.AppendLine("⚠️ 未设置跟随目标（Target）");
                isValid = false;
            }
            else
            {
                sb.AppendLine($"✓ 跟随目标：{target.name}");
            }

            // 显示当前设置
            sb.AppendLine($"\n当前设置：");
            sb.AppendLine($"- 模式: {mode}");
            sb.AppendLine($"- 旋转: {transform.rotation.eulerAngles}");
            sb.AppendLine($"- 偏移: {offset}");
            if (cam.orthographic)
            {
                sb.AppendLine($"- 正交尺寸: {cam.orthographicSize}");
            }
            else
            {
                sb.AppendLine($"- 视野角: {cam.fieldOfView}");
            }

            if (isValid)
            {
                Debug.Log(sb.ToString(), this);
            }
            else
            {
                Debug.LogWarning(sb.ToString(), this);
            }
        }

        private void OnValidate()
        {
            // 在编辑器中修改参数时更新相机设置
            if (Application.isPlaying) return;

            cam = GetComponent<Camera>();
            if (cam != null)
            {
                SetupCameraForMode();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制边界
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 size = maxBounds - minBounds;
                Vector3 center = (minBounds + maxBounds) * 0.5f;
                Gizmos.DrawWireCube(center, size);
            }

            // 绘制到目标的连线
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);

                // 绘制相机视锥体方向
                Gizmos.color = Color.green;
                Vector3 forward = transform.forward * 5f;
                Gizmos.DrawRay(transform.position, forward);

                // 绘制目标位置的标记
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position, 0.5f);
            }
        }
    }
}

