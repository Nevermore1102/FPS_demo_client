using UnityEngine;

namespace Unity.FPS.zzy.player
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    public class CameraController : MonoBehaviour
    {
        [Header("基础设置")]
        public float mouseSensitivity = 2f;    // 鼠标灵敏度
        public float maxLookAngle = 80f;       // 最大仰角
        public Transform playerBody;            // 玩家身体（用于水平旋转）
        public bool invertY = true;             // 是否反转Y轴（上下视角）
        
        [Header("第三人称设置")]
        public CameraMode cameraMode = CameraMode.FirstPerson; // 相机模式
        public float thirdPersonDistance = 5f;  // 第三人称相机距离
        public float thirdPersonHeight = 2f;    // 第三人称相机高度
        public float smoothSpeed = 10f;         // 相机跟随平滑度
        public Vector3 thirdPersonOffset = new Vector3(0f, 1f, 0f); // 第三人称视角偏移
        public LayerMask collisionLayers;       // 相机碰撞检测层

        private float xRotation = 0f;           // 垂直旋转角度
        private float currentDistance;          // 当前相机距离
        private bool isCursorLocked = true;     // 鼠标锁定状态
        private Vector3 targetPosition;         // 目标位置
        private Vector3 smoothVelocity;         // 平滑移动速度

        void Start()
        {
            // 锁定并隐藏鼠标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentDistance = thirdPersonDistance;
        }

        void Update()
        {
            // 按ESC键切换鼠标锁定状态
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorLock();
            }

            // 按V键切换视角模式
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleCameraMode();
            }

            if (isCursorLocked)
            {
                // 获取鼠标输入
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                // 计算垂直旋转（上下看）
                xRotation -= mouseY * (invertY ? 1 : -1);
                xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

                // 根据相机模式更新位置和旋转
                if (cameraMode == CameraMode.FirstPerson)
                {
                    UpdateFirstPersonCamera(mouseX);
                }
                else
                {
                    UpdateThirdPersonCamera(mouseX);
                }
            }
        }

        private void UpdateFirstPersonCamera(float mouseX)
        {
            // 应用旋转
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            
            // 水平旋转玩家身体
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }

        private void UpdateThirdPersonCamera(float mouseX)
        {
            if (playerBody == null) return;

            // 水平旋转玩家身体
            playerBody.Rotate(Vector3.up * mouseX);

            // 计算目标位置
            Vector3 targetRotation = Quaternion.Euler(xRotation, playerBody.eulerAngles.y, 0f).eulerAngles;
            Vector3 direction = Quaternion.Euler(targetRotation) * Vector3.back;
            
            // 计算理想位置
            Vector3 idealPosition = playerBody.position + thirdPersonOffset + direction * thirdPersonDistance;
            
            // 进行碰撞检测
            RaycastHit hit;
            if (Physics.Linecast(playerBody.position + thirdPersonOffset, idealPosition, out hit, collisionLayers))
            {
                currentDistance = Mathf.Min((hit.point - (playerBody.position + thirdPersonOffset)).magnitude, thirdPersonDistance);
            }
            else
            {
                currentDistance = thirdPersonDistance;
            }

            // 计算实际目标位置
            targetPosition = playerBody.position + thirdPersonOffset + direction * currentDistance;

            // 平滑移动相机
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref smoothVelocity, 1f / smoothSpeed);
            transform.rotation = Quaternion.Euler(targetRotation);
        }

        // 切换相机模式
        private void ToggleCameraMode()
        {
            cameraMode = (cameraMode == CameraMode.FirstPerson) ? CameraMode.ThirdPerson : CameraMode.FirstPerson;
            
            if (cameraMode == CameraMode.FirstPerson)
            {
                // 切换到第一人称时，重置相机位置
                transform.localPosition = Vector3.zero;
            }
        }

        // 切换鼠标锁定状态
        private void ToggleCursorLock()
        {
            isCursorLocked = !isCursorLocked;
            Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCursorLocked;
        }
    }
}