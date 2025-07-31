using UnityEngine;

public class ModelInspectTrigger : MonoBehaviour
{
    [Header("关联相机")]
    public Camera inspectCamera; // 查看相机
    public Camera roamCamera;    // 漫游相机
    public CameraMovement roamController; // 漫游控制器(控制漫游相机的脚本)

    private bool isInspecting = false;  // 是否处于查看模式
    private float doubleClickInterval = 0.3f;  // 双击判定的时间间隔（秒）
    private float lastClickTime;  // 上一次点击时间

    // 初始Depth值（确保漫游相机默认显示）
    private int roamDepth = 1;
    private int inspectDepth = 0;

    void Start()
    {
        // 初始化Depth：漫游相机优先级更高
        
        // 通过更改Depth，来切换或保证正确的摄像机渲染
        roamCamera.depth = roamDepth;
        inspectCamera.depth = inspectDepth;
    }

    void Update()
    {
        // 双击模型切换
        if (Input.GetMouseButtonDown(0))
        {
            // 从漫游相机发射一条射线到鼠标点击位置
            Ray ray = roamCamera.ScreenPointToRay(Input.mousePosition);
            // 检测射线是否击中当前物体（挂载此脚本的模型）
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                // 判断是否为双击（两次点击时间间隔小于设定值）
                if (Time.time - lastClickTime < doubleClickInterval)
                {
                    ToggleInspectMode(true);  // 触发事件 进入查看模式
                }
                lastClickTime = Time.time;  // 更新最后一次点击时间
            }
        }

        // ESC返回漫游
        if (Input.GetKeyDown(KeyCode.Escape) && isInspecting)  // 处于查看模式 且 点击了ESC按键 
        {
            ToggleInspectMode(false);  // 触发事件 退出查看模式
        }
    }

    // 切换模式：通过Depth控制显示（值高的相机优先显示）
    public void ToggleInspectMode(bool enterInspect)    // 实现 切换模式的方法
    {
        isInspecting = enterInspect;  // 更新 是否为查看模式的 状态

        if (enterInspect)  
        {
            // 查看模式：查看相机Depth更高（显示）
            inspectCamera.depth = roamDepth + 1;
            roamCamera.depth = inspectDepth;
            roamController.enabled = false; // 禁用漫游
        }
        else
        {
            // 漫游模式：漫游相机Depth更高（显示）
            roamCamera.depth = roamDepth;
            inspectCamera.depth = inspectDepth;
            roamController.enabled = true; // 启用漫游
        }

        // 关键：不隐藏鼠标（无论哪种模式都显示）
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
} 