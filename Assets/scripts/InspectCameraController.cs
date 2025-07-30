using UnityEngine;

// 相机控制类
public class InspectCameraController : MonoBehaviour
{
    [Header("旋转目标与参数")]
    public Transform targetModel;  // 要围绕旋转的目标模型
    public float rotateSpeed = 2f;  // 旋转速度 
    public float minVerticalAngle = -60f;  // 最小垂直旋转角度
    public float maxVerticalAngle = 60f;  // 最大垂直旋转角度

    [Header("缩放参数")]
    public float zoomSpeed = 2f;  // 缩放速度
    public float minZoomDistance = 1f;  // 最小缩放距离
    public float maxZoomDistance = 10f;  // 最大缩放距离

    // 以下为触摸屏相关参数（目前仅作为备用）
    // [Header("触屏参数")]
    // [Tooltip("触屏旋转灵敏度（比鼠标高一些，建议3-5）")]
    // public float touchRotateSensitivity = 3f;
    // [Tooltip("触屏缩放灵敏度（比鼠标高一些，建议0.8-1.2）")]
    // public float touchZoomSensitivity = 1f;

    private Vector3 offset;  // 相机与目标的初始偏移量
    private float xRot;  // X轴旋转角度
    private float yRot;  // Y轴旋转角度
    private float currentDistance;  // 当前相机与目标距离

    // 以下为触摸屏相关变量（目前仅作为备用）
    // private float previousTouchDistance; // 上一帧双指距离
    // private bool isSingleTouching;       // 是否单指触摸

    void Start()
    {
        // 检查是否设置了 目标模型
        if (targetModel == null)
        {
            Debug.LogError("请设置targetModel（要围绕旋转的模型）");
            enabled = false;  // 禁用脚本
            return;
        }

        offset = transform.position - targetModel.position;  // 计算初始偏移量
        currentDistance = offset.magnitude;  // 初始化当前距离
        currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
        /*
         * Mathf.Clamp(输入值，最小值，最大值)
         *如果输入值小于最小值，则返回最小值
         *如果输入值大于最大值，则返回最大值
         *如果输入值在最小值和最大值之间，则返回输入值本身
         */

        Vector3 angles = transform.eulerAngles;
        /*
         * 在游戏开发（尤其是 Unity 引擎）中，Vector3 angles = transform.eulerAngles;
         * 这行代码的作用是获取当前游戏对象（transform 组件所属的对象）的旋转角度，
         * 并用欧拉角（Euler Angles）的形式存储在 Vector3 类型的变量 angles 中。
         */
        xRot = angles.y;
        yRot = angles.x;
        if (yRot > 180) yRot -= 360;
    }

    void Update()
    {
        if (GetComponent<Camera>().depth <= 0) return;

        // 仅保留鼠标输入处理（触屏输入代码已注释备用）
        HandleMouseInput();

        UpdateCameraPosition();
    }

    /// <summary>
    /// 处理鼠标输入（旋转+缩放）
    /// </summary>
    private void HandleMouseInput()
    {
        // 鼠标旋转（左键拖动）
        if (Input.GetMouseButton(0))
        {
            xRot += Input.GetAxis("Mouse X") * rotateSpeed;
            yRot -= Input.GetAxis("Mouse Y") * rotateSpeed;
            yRot = Mathf.Clamp(yRot, minVerticalAngle, maxVerticalAngle);  // 控制y轴旋转角度
        }

        // 鼠标滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
        }
        /*
         * 它是 Unity 内部已经配置好的输入轴标识，对应鼠标滚轮的滚动动作
         * 当你滚动鼠标滚轮时，Input.GetAxis("Mouse ScrollWheel") 会返回一个浮点值：
         * 向前滚动（远离自己）时，返回正值（通常是 0.1 左右，取决于设置）
         * 向后滚动（靠近自己）时，返回负值（通常是 -0.1 左右）
         * 不滚动时，返回 0
         * 这个名称是固定的约定，必须严格按照 "Mouse ScrollWheel" 拼写才能正确获取鼠标滚轮输入。
         * 如果修改这个字符串（比如写成 "mousewheel" 或其他形式），Unity 就无法识别，会返回 0。
         * 类似的预定义输入轴还有：
         * "Horizontal" 和 "Vertical"：对应键盘方向键或 WASD 键
         * "Mouse X" 和 "Mouse Y"：对应鼠标在水平和垂直方向的移动
         */
    }

    /// <summary>
    /// 触屏输入处理代码（目前注释备用，启用时需解除注释）
    /// 功能：单指拖动旋转，双指捏合缩放
    /// </summary>
    /*
    private void HandleTouchInput()
    {
        // 1. 单指触摸 → 旋转视角
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // 触摸开始时标记状态
            if (touch.phase == TouchPhase.Began)
            {
                isSingleTouching = true;
            }
            // 触摸移动时计算旋转
            else if (touch.phase == TouchPhase.Moved && isSingleTouching)
            {
                // 触屏 deltaPosition 是像素变化，需要转换为角度（乘以灵敏度）
                xRot += touch.deltaPosition.x * touchRotateSensitivity * 0.1f; // 0.1f 是缩放系数，避免旋转过快
                yRot -= touch.deltaPosition.y * touchRotateSensitivity * 0.1f;
                yRot = Mathf.Clamp(yRot, minVerticalAngle, maxVerticalAngle);
            }
            // 触摸结束时重置状态
            else if (touch.phase == TouchPhase.Ended)
            {
                isSingleTouching = false;
            }
        }

        // 2. 双指触摸 → 缩放视角（捏合/张开）
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // 获取当前双指位置
            Vector2 touch1Pos = touch1.position;
            Vector2 touch2Pos = touch2.position;

            // 计算当前双指距离
            float currentTouchDistance = Vector2.Distance(touch1Pos, touch2Pos);

            // 第一帧检测到双指时，记录初始距离
            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                previousTouchDistance = currentTouchDistance;
            }
            // 双指移动时计算缩放
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                // 计算距离变化量（当前 - 上一帧）
                float distanceDelta = currentTouchDistance - previousTouchDistance;

                // 根据距离变化调整相机距离（负号表示：距离变大→相机远离→缩小；距离变小→相机靠近→放大）
                currentDistance -= distanceDelta * touchZoomSensitivity * 0.01f; // 0.01f 是缩放系数
                currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

                // 更新上一帧距离
                previousTouchDistance = currentTouchDistance;
            }
        }
    }
    */

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(yRot, xRot, 0);  // 根据yRot和xRot创建旋转四元数
        Vector3 newOffset = rotation * (offset.normalized * currentDistance);  // 计算新的偏移量：将标准化的初始偏移量乘以当前距离，再应用旋转
        transform.position = targetModel.position + newOffset;  // 设置相机位置为目标模型位置加上新的偏移量
        transform.LookAt(targetModel.position);  // 让相机始终看向目标模型的位置
    }
}
