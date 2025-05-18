using UnityEngine;

namespace Unity.FPS.zzy.player
{

public class zzyMove : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;        // 行走速度
    public float runSpeed = 10f;        // 奔跑速度
    public float jumpForce = 5f;        // 跳跃力度
    
    private CharacterController m_characterController;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private bool isGrounded;
    private float gravity = -9.81f;
    private bool useCharacterController = false; // 是否使用CharacterController
    private Transform playerRoot; // 玩家根节点（父对象）

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Hello World! 111");
        //打印对象当前坐标
        Debug.Log("当前坐标为：" + transform.position);
        //打印局部坐标        
        Debug.Log("局部坐标为：" + transform.localPosition);
        
                //按下W键，向前移动
        //先获取物体对象，打印当前物体名字。
        GameObject nowObj = gameObject;
        Debug.Log("当前物体名字为：" + nowObj.name);
        
        //位置变换
        transform.position = transform.position + Vector3.forward * Time.deltaTime * 5;
        Debug.Log("当前坐标为：" + transform.position);

        // 获取CharacterController组件
        m_characterController = GetComponent<CharacterController>();
        if (m_characterController != null)
        {
            useCharacterController = true;
            Debug.Log("成功获取CharacterController组件");
        }
        else
        {
            useCharacterController = false;
            Debug.LogWarning("未找到CharacterController组件，将使用Transform移动。请考虑添加CharacterController组件以获得更好的物理效果。");
        }

        // 获取玩家根节点（父对象）
        playerRoot = transform.parent;
        if (playerRoot == null)
        {
            Debug.LogWarning("未找到父对象，将使用当前对象作为根节点");
            playerRoot = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 计算移动方向（使用根节点的朝向）
        Vector3 move = playerRoot.right * horizontal + playerRoot.forward * vertical;
        
        // 判断是否按住Shift键进行奔跑
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        
        if (useCharacterController)
        {
            // 使用CharacterController移动
            UpdateCharacterControllerMovement(move, currentSpeed);
        }
        else
        {
            // 使用Transform移动
            UpdateTransformMovement(move, currentSpeed);
        }
    }

    private void UpdateCharacterControllerMovement(Vector3 move, float currentSpeed)
    {
        // 检查是否在地面上
        isGrounded = m_characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        
        // 应用移动（移动整个玩家层级）
        Vector3 movement = move * currentSpeed * Time.deltaTime;
        m_characterController.Move(movement);
        playerRoot.position = transform.position;

        // 处理跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // 应用重力
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 verticalMovement = Vector3.up * verticalVelocity * Time.deltaTime;
        m_characterController.Move(verticalMovement);
        playerRoot.position = transform.position;
    }

    private void UpdateTransformMovement(Vector3 move, float currentSpeed)
    {
        // 简单的地面检测（射线检测）
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        
        // 应用移动（移动整个玩家层级）
        Vector3 movement = move * currentSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
        playerRoot.position = transform.position;
        
        // 处理跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = jumpForce;
        }
        
        // 应用重力
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 verticalMovement = Vector3.up * verticalVelocity * Time.deltaTime;
            transform.Translate(verticalMovement, Space.World);
            playerRoot.position = transform.position;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = 0;
        }
    }
}

}