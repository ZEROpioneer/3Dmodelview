using UnityEngine;

/// <summary>
/// 控制漫游相机的移动（WASD）和旋转（鼠标）功能
/// 挂载对象：漫游相机的父对象（如RoamCameraRig）
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("基础移动速度（单位：米/秒）")]
    public float moveSpeed = 5f;
    
    [Tooltip("加速系数（控制从静止到最大速度的快慢）")]
    public float acceleration = 15f;
    
    [Tooltip("减速系数（控制从移动到静止的快慢）")]
    public float deceleration = 20f;
    
    [Tooltip("最大移动速度限制（防止速度无限增加）")]
    public float maxVelocity = 8f;

    [Header("旋转设置")]
    [Tooltip("鼠标旋转灵敏度（值越大旋转越灵敏）")]
    public float rotateSensitivity = 1.5f;
    
    [Tooltip("垂直旋转的最低角度（向下看的最大角度）")]
    public float minVerticalAngle = -60f;
    
    [Tooltip("垂直旋转的最高角度（向上看的最大角度）")]
    public float maxVerticalAngle = 60f;
    
    [Tooltip("是否反转Y轴旋转（上下拖动鼠标时方向反转）")]
    public bool invertY = false;
    
    [Tooltip("控制旋转的鼠标按键（0=左键，1=右键，2=中键）")]
    public int mouseButton = 0;

    [Tooltip("需要控制旋转的主相机（拖入场景中的漫游相机）")]
    public Camera mainCamera;

    // 当前移动速度向量（存储X、Y、Z三个方向的速度）
    private Vector3 currentVelocity;
    
    // 角色控制器组件（用于处理碰撞和移动）
    private CharacterController characterController;
    
    // 水平旋转角度（绕Y轴旋转，控制左右方向）
    private float xRotation;
    
    // 垂直旋转角度（绕X轴旋转，控制上下方向）
    private float yRotation;
    
    // 记录鼠标按键是否被按下（用于判断是否需要旋转）
    private bool isMouseButtonPressed;

    /// <summary>
    /// 初始化组件和参数
    /// </summary>
    void Start()
    {
        // 获取或添加角色控制器组件（处理碰撞检测）
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            // 如果没有角色控制器，自动添加并设置默认参数
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.3f;   // 碰撞体半径（避免穿墙）
            characterController.height = 1.6f;   // 碰撞体高度（模拟人高）
        }
        
        // 如果未指定主相机，自动获取场景中Tag为MainCamera的相机
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 初始化旋转角度（基于相机当前的朝向）
        if (mainCamera != null)
        {
            Vector3 angles = mainCamera.transform.eulerAngles;
            xRotation = angles.y;  // 水平旋转初始值（Y轴角度）
            
            // 处理垂直角度的范围（将0-360度转换为-180-180度，方便后续限制角度）
            yRotation = angles.x > 180 ? angles.x - 360 : angles.x;
        }
    }

    /// <summary>
    /// 每帧更新移动和旋转逻辑
    /// </summary>
    void Update()
    {
        // 如果主相机未设置，直接退出（避免空引用错误）
        if (mainCamera == null) return;

        // 处理鼠标旋转逻辑
        HandleMouseRotation();

        // 处理键盘移动逻辑
        HandleMovement();
    }

    /// <summary>
    /// 处理鼠标旋转控制
    /// </summary>
    private void HandleMouseRotation()
    {
        // 检测指定的鼠标按键是否被按下（默认左键）
        isMouseButtonPressed = Input.GetMouseButton(mouseButton);
        if (isMouseButtonPressed)
        {
            // 获取鼠标X轴和Y轴的移动量（Raw表示未平滑处理的原始输入）
            float mouseX = Input.GetAxisRaw("Mouse X") * rotateSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * rotateSensitivity;

            // 如果启用Y轴反转，将Y轴移动量取反
            if (invertY)
                mouseY = -mouseY;

            // 更新水平旋转角度（左右拖动鼠标改变）
            xRotation += mouseX;
            
            // 更新垂直旋转角度（上下拖动鼠标改变）
            yRotation -= mouseY;
            
            // 限制垂直旋转角度在指定范围内（防止过度仰头或低头）
            yRotation = Mathf.Clamp(yRotation, minVerticalAngle, maxVerticalAngle);
            
            // 应用旋转到相机（欧拉角转四元数，避免万向锁问题）
            mainCamera.transform.rotation = Quaternion.Euler(yRotation, xRotation, 0);
        }
    }

    /// <summary>
    /// 处理键盘WASD移动控制
    /// </summary>
    private void HandleMovement()
    {
        // 获取水平和垂直输入（A/D对应Horizontal，W/S对应Vertical）
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // 将输入转换为二维向量并归一化（避免斜向移动速度过快）
        Vector2 input = new Vector2(horizontal, vertical).normalized;

        // 目标移动方向（初始化为零向量）
        Vector3 targetDir = Vector3.zero;
        
        // 如果有有效输入（输入向量长度大于0.01，避免微小输入触发移动）
        if (input.sqrMagnitude > 0.01f)
        {
            // 获取相机的前向和右向向量（用于计算移动方向）
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            
            // 忽略Y轴方向（确保移动始终在水平面上，不会上下飘）
            camForward.y = 0;
            camRight.y = 0;
            
            // 归一化向量（确保前向和右向的移动速度一致）
            camForward.Normalize();
            camRight.Normalize();

            // 计算最终移动方向（结合相机朝向：前向*前后输入 + 右向*左右输入）
            targetDir = (camForward * input.y + camRight * input.x).normalized;
        }

        // 根据目标方向计算目标速度
        Vector3 targetVelocity = targetDir * moveSpeed;

        // 处理加速和减速
        if (input.sqrMagnitude > 0.01f)
        {
            // 有输入时：向目标速度加速（使用MoveTowards实现平滑过渡）
            currentVelocity = Vector3.MoveTowards(
                currentVelocity, 
                targetVelocity, 
                acceleration * Time.deltaTime  // 加速步长（与时间相关，确保帧率稳定）
            );
            
            // 限制速度不超过最大值
            if (currentVelocity.magnitude > maxVelocity)
                currentVelocity = currentVelocity.normalized * maxVelocity;
        }
        else
        {
            // 无输入时：向零速度减速（逐渐停止）
            currentVelocity = Vector3.MoveTowards(
                currentVelocity, 
                Vector3.zero, 
                deceleration * Time.deltaTime  // 减速步长
            );
        }

        // 通过角色控制器移动（自动处理碰撞，不会穿墙）
        characterController.Move(currentVelocity * Time.deltaTime);
    }
}
