using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 视频播放器UI控制器
/// 负责管理视频播放逻辑与UI交互的绑定
/// </summary>
public class VideoPlayerUIController : MonoBehaviour
{
    // 视频核心组件
    /// <summary>
    /// Unity视频播放器组件，负责加载和播放视频文件
    /// </summary>
    public VideoPlayer videoPlayer;
    /// <summary>
    /// 音频源组件，负责播放视频的声音（VideoPlayer本身不处理音频）
    /// </summary>
    public AudioSource audioSource;

    // UI控件
    /// <summary>
    /// 用于显示视频画面的RawImage组件
    /// </summary>
    public RawImage videoRawImage;
    /// <summary>
    /// 播放/暂停按钮 及对应图标
    /// </summary>
    public Button pauseButton;
    public Sprite pauseIsSprite;     // 视频开启图标
    public Sprite pauseNotSprite;    // 视频暂停图标
    /// <summary>
    /// 切换功能按钮（预留功能）
    /// </summary>
    public Button switchButton;
    /// <summary>
    /// 静音/取消静音按钮
    /// </summary>
    public Button volumeButton;
    public Sprite volumeOnSprite;     // 音量开启图标
    public Sprite volumeOffSprite;    // 音量关闭图标
    /// <summary>
    /// 显示当前播放时间的文本
    /// </summary>
    public Text currentTimeText;
    /// <summary>
    /// 显示视频总时长的文本
    /// </summary>
    public Text totalDurationText;
    /// <summary>
    /// 控制视频播放进度的滑块
    /// </summary>
    public Slider progressSlider;
    /// <summary>
    /// 控制音量大小的滑块
    /// </summary>
    public Slider volumeSlider;

    // 状态变量
    /// <summary>
    /// 标记视频是否正在播放
    /// </summary>
    private bool isPlaying = true;
    /// <summary>
    /// 标记是否静音
    /// </summary>
    private bool isMuted = false;
    /// <summary>
    /// 标记进度条是否正在被用户拖动
    /// 用于区分是用户操作还是代码自动更新滑块
    /// </summary>
    private bool isDraggingSlider = false;
    /// <summary>
    /// 记录上次通过进度条跳转的时间
    /// 用于限制更新频率，避免频繁触发视频跳转
    /// </summary>
    private float lastSeekTime;
    /// <summary>
    /// 进度条跳转的最小时间间隔（秒）
    /// 防止短时间内多次触发视频位置更新，优化性能
    /// </summary>
    private const float seekInterval = 0.1f;

    /// <summary>
    /// 初始化方法，在脚本启动时执行
    /// 用于设置视频播放器和UI事件绑定
    /// </summary>
    void Start()
    {
        // 初始化视频：开始预加载视频，准备完成后触发OnVideoPrepared方法
        videoPlayer.Prepare();
        /*
         * videoPlayer.Prepare();
         * 作用：开始异步加载视频资源（包括视频画面和关联的音频数据）。
         * 特点：
         * 这是一个异步操作，不会阻塞主线程（游戏仍能正常运行）。
         * 视频较大时，加载需要一定时间（取决于视频大小和设备性能）。
         * 调用后，VideoPlayer 会进入 "准备中" 状态，此时还不能播放视
         */
        videoPlayer.prepareCompleted += OnVideoPrepared;
        /*
         * videoPlayer.prepareCompleted += OnVideoPrepared;
         * 作用：注册一个 "准备完成" 的回调函数（事件监听）。
         * 细节说明：
         * prepareCompleted 是 VideoPlayer 的一个事件（类似 "通知"），当视频加载完成并可以播放时会自动触发。
         * += OnVideoPrepared 表示：当 prepareCompleted 事件触发时，执行 OnVideoPrepared 这个方法。
         * 这是一种 "异步回调" 模式，确保视频加载完成后才执行后续操作（如开始播放、更新 UI 等）。
         */

        // 绑定按钮点击事件
        pauseButton.onClick.AddListener(TogglePlayPause);      // 播放/暂停按钮
        switchButton.onClick.AddListener(SwitchButtonFunction);// 切换功能按钮
        volumeButton.onClick.AddListener(ToggleMute);          // 静音按钮

        // 绑定进度条值变化事件（当滑块被拖动时触发）
        progressSlider.onValueChanged.AddListener(OnProgressSliderChanged);

        // 初始化音量滑块范围和初始值
        volumeSlider.minValue = 0;                   // 最小音量（静音）
        volumeSlider.maxValue = 1;                   // 最大音量（100%）
        volumeSlider.value = audioSource.volume;     // 设置初始值为当前音频音量
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged); // 绑定音量变化事件
    }

    /// <summary>
    /// 每帧更新方法
    /// 用于在视频播放时同步更新进度条和时间显示
    /// </summary>
    void Update()
    {
        // 当视频正在播放且进度条未被拖动时，更新进度条（由视频播放位置驱动）
        if (videoPlayer.isPlaying && !isDraggingSlider)
        {
            // 计算当前进度（0-1之间）：当前时间 / 总时长
            progressSlider.value = (float)((double)videoPlayer.time / videoPlayer.length);
            // 更新当前播放时间的文本显示
            UpdateCurrentTimeText();
        }
    }

    /// <summary>
    /// 视频准备完成后的回调方法
    /// 当视频加载完成可以播放时触发
    /// </summary>
    /// <param name="source">触发事件的VideoPlayer实例</param>
    private void OnVideoPrepared(VideoPlayer source)
    {
        // 更新总时长显示
        UpdateTotalDurationText();
        // 初始化进度条最大值（0-1范围）
        progressSlider.maxValue = 1;
        progressSlider.value = 0;
        
        // 开始播放视频和音频
        videoPlayer.Play();
        audioSource.Play();
        // 更新暂停按钮文本为"暂停"
        pauseButton.GetComponentInChildren<Text>().text = "暂停";
    }

    /// <summary>
    /// 切换播放/暂停状态
    /// 点击按钮时触发
    /// </summary>
    private void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)  // 判断当前视频播放状态
        {
            // 当前正在播放：暂停视频和音频
            videoPlayer.Pause();
            audioSource.Pause();
            // 更新按钮文本为"播放"
            pauseButton.GetComponentInChildren<Text>().text = "播放";
        }
        else
        {
            // 当前已暂停：继续播放视频和音频
            videoPlayer.Play();
            audioSource.Play();
            // 更新按钮文本为"暂停"
            pauseButton.GetComponentInChildren<Text>().text = "暂停";
        }
        // 更新播放状态标记
        isPlaying = videoPlayer.isPlaying;
    }

    /// <summary>
    /// 切换按钮的功能实现（预留）
    /// 目前仅打印日志，可根据需求扩展
    /// </summary>
    private void SwitchButtonFunction()
    {
        Debug.Log("切换按钮被点击");
    }

    /// <summary>
    /// 切换静音状态
    /// 点击音量按钮时触发
    /// </summary>
    private void ToggleMute()
    {
        // 切换静音状态（取反）
        isMuted = !isMuted;
        // 应用静音状态到音频源
        audioSource.mute = isMuted;
        // 更新按钮文本（静音状态显示"取消静音"，反之显示"静音"）
        volumeButton.GetComponentInChildren<Text>().text = isMuted ? "取消静音" : "静音";
    }

    /// <summary>
    /// 进度条值变化时的处理方法
    /// 当用户拖动进度条或代码更新滑块位置时触发
    /// </summary>
    /// <param name="value">进度条的当前值（0-1之间）</param>
    private void OnProgressSliderChanged(float value)
    {
        // 视频未加载完成（总时长为0）时不处理
        if (videoPlayer.length <= 0) return;

        // 根据进度条值计算目标播放时间（总时长 × 进度值）
        float targetTime = (float)(videoPlayer.length * value);
        
        // 仅在用户拖动进度条时处理（避免代码自动更新时触发跳转）
        if (isDraggingSlider)
        {
            // 限制更新频率：
            // 1. 与上次跳转时间间隔超过0.1秒，或
            // 2. 目标时间与当前播放时间差异超过1秒（强制更新）
            if (Mathf.Abs(targetTime - lastSeekTime) > seekInterval || 
                Mathf.Abs(targetTime - (float)videoPlayer.time) > 1f)
            {
                // 跳转到目标时间
                videoPlayer.time = targetTime;
                // 记录本次跳转时间
                lastSeekTime = targetTime;
                // 实时更新时间显示
                UpdateCurrentTimeText();
            }
        }
        // 非拖动状态（如代码自动更新滑块时）不执行任何操作
    }

    /// <summary>
    /// 进度条开始被拖动时的处理方法
    /// 需要通过EventTrigger组件手动关联BeginDrag事件
    /// </summary>
    public void OnSliderBeginDrag()
    {
        // 标记为正在拖动状态
        isDraggingSlider = true;
        // 记录开始拖动时的视频时间
        lastSeekTime = (float)videoPlayer.time;
    }

    /// <summary>
    /// 进度条结束拖动时的处理方法
    /// 需要通过EventTrigger组件手动关联EndDrag事件
    /// </summary>
    public void OnSliderEndDrag()
    {
        // 标记为结束拖动状态
        isDraggingSlider = false;
        // 拖动结束时强制同步一次播放位置（确保精确）
        float finalTime = (float)(videoPlayer.length * progressSlider.value);
        videoPlayer.time = finalTime;
    }

    /// <summary>
    /// 音量滑块值变化时的处理方法
    /// 当用户拖动音量滑块时触发
    /// </summary>
    /// <param name="value">音量值（0-1之间）</param>
    public void OnVolumeSliderChanged(float value)
    {
        // 应用音量值到音频源
        audioSource.volume = value;
    }

    /// <summary>
    /// 更新当前播放时间的文本显示
    /// 格式化为"分:秒"（例如：02:35）
    /// </summary>
    private void UpdateCurrentTimeText()
    {
        // 确保文本组件存在且视频已加载
        if (currentTimeText != null && videoPlayer.length > 0)
        {
            currentTimeText.text = FormatTime((float)videoPlayer.time);
        }
    }

    /// <summary>
    /// 更新视频总时长的文本显示
    /// 格式化为"分:秒"
    /// </summary>
    private void UpdateTotalDurationText()
    {
        // 确保文本组件存在且视频已加载
        if (totalDurationText != null && videoPlayer.length > 0)
        {
            totalDurationText.text = FormatTime((float)videoPlayer.length);
        }
    }

    /// <summary>
    /// 将秒数格式化为"分:秒"字符串
    /// 确保分钟和秒数都是两位数（不足补0）
    /// </summary>
    /// <param name="seconds">需要格式化的秒数</param>
    /// <returns>格式化后的字符串（例如：01:05）</returns>
    private string FormatTime(float seconds)
    {
        int minutes = (int)seconds / 60;   // 计算分钟数
        int secs = (int)seconds % 60;      // 计算剩余秒数
        return $"{minutes:D2}:{secs:D2}"; // 格式化为两位数（D2表示两位数，不足补0）
    }

    /// <summary>
    /// 脚本销毁时执行的方法
    /// 用于移除事件监听，防止内存泄漏
    /// </summary>
    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }
}