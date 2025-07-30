using UnityEngine;

public class ModelInspectTrigger : MonoBehaviour
{
    [Header("关联相机")]
    public Camera inspectCamera; // 查看相机
    public Camera roamCamera;    // 漫游相机
    public CameraMovement roamController; // 漫游控制器

    private bool isInspecting = false;
    private float doubleClickInterval = 0.3f;
    private float lastClickTime;

    // 初始Depth值（确保漫游相机默认显示）
    private int roamDepth = 1;
    private int inspectDepth = 0;

    void Start()
    {
        // 初始化Depth：漫游相机优先级更高
        roamCamera.depth = roamDepth;
        inspectCamera.depth = inspectDepth;
    }

    void Update()
    {
        // 双击模型切换
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = roamCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                if (Time.time - lastClickTime < doubleClickInterval)
                {
                    ToggleInspectMode(true);
                }
                lastClickTime = Time.time;
            }
        }

        // ESC返回漫游
        if (Input.GetKeyDown(KeyCode.Escape) && isInspecting)
        {
            ToggleInspectMode(false);
        }
    }

    // 切换模式：通过Depth控制显示（值高的相机优先显示）
    public void ToggleInspectMode(bool enterInspect)
    {
        isInspecting = enterInspect;

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