using UnityEngine;

namespace Character3C
{
    /// <summary>
    /// 2D 相机控制器
    /// 提供平滑跟随、震动等效果
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;

        [Header("边界限制")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-10, -10);
        [SerializeField] private Vector2 maxBounds = new Vector2(10, 10);

        [Header("前瞻设置")]
        [SerializeField] private bool useLookAhead = false;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSpeed = 2f;

        [Header("死区设置")]
        [SerializeField] private bool useDeadZone = false;
        [SerializeField] private Vector2 deadZoneSize = new Vector2(1f, 0.5f);

        // 相机状态
        private Vector3 targetPosition;
        private Vector3 velocity;
        private Vector2 lookAheadOffset;

        // 震动效果
        private float shakeIntensity;
        private float shakeDuration;
        private float shakeTimer;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float deltaTime = Time.deltaTime;

            // 更新震动
            UpdateShake(deltaTime);

            // 计算目标位置
            CalculateTargetPosition();

            // 平滑移动到目标位置
            MoveToTarget(deltaTime);
        }

        /// <summary>
        /// 计算目标位置
        /// </summary>
        private void CalculateTargetPosition()
        {
            Vector3 desiredPosition = target.position;

            // 应用偏移
            desiredPosition += new Vector3(offset.x, offset.y, 0);

            // 前瞻
            if (useLookAhead)
            {
                UpdateLookAhead();
                desiredPosition += new Vector3(lookAheadOffset.x, lookAheadOffset.y, 0);
            }

            // 死区检测
            if (useDeadZone)
            {
                Vector3 currentPos = transform.position;
                Vector2 diff = new Vector2(desiredPosition.x - currentPos.x, desiredPosition.y - currentPos.y);

                // X 轴死区
                if (Mathf.Abs(diff.x) < deadZoneSize.x * 0.5f)
                {
                    desiredPosition.x = currentPos.x;
                }

                // Y 轴死区
                if (Mathf.Abs(diff.y) < deadZoneSize.y * 0.5f)
                {
                    desiredPosition.y = currentPos.y;
                }
            }

            // 应用轴向限制
            if (!followX)
            {
                desiredPosition.x = transform.position.x;
            }

            if (!followY)
            {
                desiredPosition.y = transform.position.y;
            }

            // 保持相机 Z 轴
            desiredPosition.z = transform.position.z;

            // 应用边界限制
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            targetPosition = desiredPosition;
        }

        /// <summary>
        /// 更新前瞻偏移
        /// </summary>
        private void UpdateLookAhead()
        {
            // 获取目标的移动方向
            if (target.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Vector2 targetOffset = rb.linearVelocity.normalized * lookAheadDistance;
                lookAheadOffset = Vector2.Lerp(lookAheadOffset, targetOffset, lookAheadSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 移动到目标位置
        /// </summary>
        private void MoveToTarget(float deltaTime)
        {
            if (smoothSpeed > 0)
            {
                // 平滑移动
                Vector3 smoothPosition = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref velocity,
                    1f / smoothSpeed
                );

                transform.position = smoothPosition;
            }
            else
            {
                // 立即移动
                transform.position = targetPosition;
            }

            // 应用震动偏移
            if (shakeTimer > 0)
            {
                Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity;
                transform.position += shakeOffset;
            }
        }

        /// <summary>
        /// 更新震动效果
        /// </summary>
        private void UpdateShake(float deltaTime)
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= deltaTime;
                if (shakeTimer <= 0)
                {
                    shakeTimer = 0;
                    shakeIntensity = 0;
                }
            }
        }

        /// <summary>
        /// 触发相机震动
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }

        /// <summary>
        /// 停止震动
        /// </summary>
        public void StopShake()
        {
            shakeTimer = 0;
            shakeIntensity = 0;
        }

        /// <summary>
        /// 设置跟随目标
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            // 立即移动到目标位置
            if (target != null)
            {
                CalculateTargetPosition();
                transform.position = targetPosition;
            }
        }

        /// <summary>
        /// 设置偏移
        /// </summary>
        public void SetOffset(Vector2 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// 设置边界
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }

        /// <summary>
        /// 禁用边界
        /// </summary>
        public void DisableBounds()
        {
            useBounds = false;
        }

        /// <summary>
        /// 立即移动到目标
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                CalculateTargetPosition();
                transform.position = targetPosition;
                velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 获取相机视野范围
        /// </summary>
        public Bounds GetViewBounds()
        {
            if (cam == null) cam = GetComponent<Camera>();

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            return new Bounds(transform.position, new Vector3(width, height, 0));
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制边界
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
                Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0);
                Gizmos.DrawWireCube(center, size);
            }

            // 绘制死区
            if (useDeadZone && target != null)
            {
                Gizmos.color = Color.green;
                Vector3 pos = target.position + new Vector3(offset.x, offset.y, 0);
                Gizmos.DrawWireCube(pos, new Vector3(deadZoneSize.x, deadZoneSize.y, 0));
            }
        }
    }
}

