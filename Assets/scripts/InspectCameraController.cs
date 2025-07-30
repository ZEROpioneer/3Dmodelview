using UnityEngine;

public class InspectCameraController : MonoBehaviour
{
    [Header("旋转目标与参数")]
    public Transform targetModel;
    public float rotateSpeed = 2f;
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 60f;

    [Header("缩放参数")]
    public float zoomSpeed = 0.5f;
    public float minZoomDistance = 1f;
    public float maxZoomDistance = 10f;

    // 以下为触摸屏相关参数（目前仅作为备用）
    // [Header("触屏参数")]
    // [Tooltip("触屏旋转灵敏度（比鼠标高一些，建议3-5）")]
    // public float touchRotateSensitivity = 3f;
    // [Tooltip("触屏缩放灵敏度（比鼠标高一些，建议0.8-1.2）")]
    // public float touchZoomSensitivity = 1f;

    private Vector3 offset;
    private float xRot;
    private float yRot;
    private float currentDistance;

    // 以下为触摸屏相关变量（目前仅作为备用）
    // private float previousTouchDistance; // 上一帧双指距离
    // private bool isSingleTouching;       // 是否单指触摸

    void Start()
    {
        if (targetModel == null)
        {
            Debug.LogError("请设置targetModel（要围绕旋转的模型）");
            enabled = false;
            return;
        }

        offset = transform.position - targetModel.position;
        currentDistance = offset.magnitude;
        currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

        Vector3 angles = transform.eulerAngles;
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
            yRot = Mathf.Clamp(yRot, minVerticalAngle, maxVerticalAngle);
        }

        // 鼠标滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
        }
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
        Quaternion rotation = Quaternion.Euler(yRot, xRot, 0);
        Vector3 newOffset = rotation * (offset.normalized * currentDistance);
        transform.position = targetModel.position + newOffset;
        transform.LookAt(targetModel.position);
    }
}
