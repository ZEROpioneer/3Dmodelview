using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

/// <summary>
/// 视频播放器UI控制器
/// 负责管理视频播放逻辑与UI交互的绑定
/// </summary>
public class VideoPlayerUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    // 新增：音量滑块的父级判定Panel
    public RectTransform volumeSliderPanel;  // 需要在Inspector中拖入Panel
    // 控制循环播放按钮
    public Toggle loopToggle; 

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
    /// 用于限制更新频率， /// 用于限制更新频率，避免频繁触发视频跳转
    /// </summary>
    private float lastSeekTime;
    /// <summary>
    /// 进度条跳转的最小时间间隔（秒）
    /// 防止短时间内多次触发视频位置更新，优化性能
    /// </summary>
    private const float seekInterval = 0.1f;
    /// <summary>
    /// 标记鼠标是否悬停在音量按钮上
    /// </summary>
    private bool isHoveringVolumeButton = false;
    // 新增：标记鼠标是否悬停在音量滑块的Panel上
    private bool isHoveringVolumePanel = false;
    // 新增：标记滑块是否被激活（仅通过音量按钮激活）
    private bool isVolumeSliderActivated = false;

    /// <summary>
    /// 初始化方法，在脚本启动时执行
    /// 用于设置视频播放器和UI事件绑定
    /// </summary>
    void Start()
    {
        // 初始化视频：开始预加载视频，准备完成后触发OnVideoPrepared方法
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        // 新增：绑定视频播放结束事件
        videoPlayer.loopPointReached += OnVideoEnded;  // 视频播放完毕时触发
        
        // 新增：初始化时同步循环开关状态（关键修复）
        videoPlayer.isLooping = loopToggle.isOn;  // 让视频播放器初始就遵循开关状态

        // 绑定按钮点击事件
        pauseButton.onClick.AddListener(TogglePlayPause);      // 播放/暂停按钮
        switchButton.onClick.AddListener(SwitchButtonFunction);// 切换功能按钮
        volumeButton.onClick.AddListener(ToggleMute);          // 静音按钮
        // 绑定开关点击事件
        loopToggle.onValueChanged.AddListener(OnLoopToggleChanged);  // 循环播放开关

        // 绑定进度条值变化事件（当滑块被拖动时触发）
        progressSlider.onValueChanged.AddListener(OnProgressSliderChanged);

        // 初始化音量滑块范围和初始值，并默认隐藏
        volumeSlider.minValue = 0;
        volumeSlider.maxValue = 1;
        volumeSlider.value = audioSource.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        volumeSlider.gameObject.SetActive(false);  // 默认隐藏音量滑块

        // 为音量按钮添加事件触发器（用于检测鼠标悬停）
        EventTrigger trigger = volumeButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = volumeButton.gameObject.AddComponent<EventTrigger>();
        }

        // 添加鼠标进入事件
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
        trigger.triggers.Add(enterEntry);

        // 添加鼠标离开事件
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
        trigger.triggers.Add(exitEntry);

        // 新增：为音量Panel添加事件触发器（核心修改）
        EventTrigger panelTrigger = volumeSliderPanel.GetComponent<EventTrigger>();
        if (panelTrigger == null)
        {
            panelTrigger = volumeSliderPanel.gameObject.AddComponent<EventTrigger>();
        }

        // 新增：Panel鼠标进入事件
        EventTrigger.Entry panelEnter = new EventTrigger.Entry();
        panelEnter.eventID = EventTriggerType.PointerEnter;
        panelEnter.callback.AddListener((data) => { isHoveringVolumePanel = true; });
        panelTrigger.triggers.Add(panelEnter);

        // 新增：Panel鼠标离开事件
        EventTrigger.Entry panelExit = new EventTrigger.Entry();
        panelExit.eventID = EventTriggerType.PointerExit;
        panelExit.callback.AddListener((data) => { isHoveringVolumePanel = false; });
        panelTrigger.triggers.Add(panelExit);
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
            progressSlider.value = (float)((double)videoPlayer.time / videoPlayer.length);
            UpdateCurrentTimeText();
        }

        // 核心修改：滑块显示逻辑
        // 1. 必须先通过音量按钮激活
        // 2. 鼠标在按钮上或Panel上时保持显示
        volumeSlider.gameObject.SetActive(isVolumeSliderActivated && (isHoveringVolumeButton || isHoveringVolumePanel));

        // 核心修改：失活条件（鼠标同时离开按钮和Panel）
        if (!isHoveringVolumeButton && !isHoveringVolumePanel)
        {
            isVolumeSliderActivated = false;  // 重置激活状态
        }
    }

    /// <summary>
    /// 视频准备完成后的回调方法
    /// 当视频加载完成可以播放时触发
    /// </summary>
    /// <param name="source">触发事件的VideoPlayer实例</param>
    private void OnVideoPrepared(VideoPlayer source)
    {
        UpdateTotalDurationText();
        progressSlider.maxValue = 1;
        progressSlider.value = 0;
        
        videoPlayer.Play();
        audioSource.Play();
        pauseButton.GetComponentInChildren<Text>().text = "暂停";
    }

    /// <summary>
    /// 切换播放/暂停状态
    /// 点击按钮时触发
    /// </summary>
    private void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            audioSource.Pause();
            pauseButton.GetComponentInChildren<Text>().text = "播放";
        }
        else
        {
            videoPlayer.Play();
            audioSource.Play();
            pauseButton.GetComponentInChildren<Text>().text = "暂停";
        }
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
        audioSource.mute = isMuted;

        // 更新音量按钮图标
        // volumeButton.image.sprite = isMuted ? volumeOffSprite : volumeOnSprite;
        // 更新按钮文本
        volumeButton.GetComponentInChildren<Text>().text = isMuted ? "取消静音" : "静音";
    }

    /// <summary>
    /// 进度条值变化时的处理方法
    /// 当用户拖动进度条或代码更新滑块位置时触发
    /// </summary>
    /// <param name="value">进度条的当前值（0-1之间）</param>
    private void OnProgressSliderChanged(float value)
    {
        if (videoPlayer.length <= 0) return;

        float targetTime = (float)(videoPlayer.length * value);
        
        if (isDraggingSlider)
        {
            if (Mathf.Abs(targetTime - lastSeekTime) > seekInterval || 
                Mathf.Abs(targetTime - (float)videoPlayer.time) > 1f)
            {
                videoPlayer.time = targetTime;
                lastSeekTime = targetTime;
                UpdateCurrentTimeText();
            }
        }
    }

    /// <summary>
    /// 进度条开始被拖动时的处理方法
    /// </summary>
    public void OnSliderBeginDrag()
    {
        isDraggingSlider = true;
        lastSeekTime = (float)videoPlayer.time;
    }

    /// <summary>
    /// 进度条结束拖动时的处理方法
    /// </summary>
    public void OnSliderEndDrag()
    {
        isDraggingSlider = false;
        float finalTime = (float)(videoPlayer.length * progressSlider.value);
        videoPlayer.time = finalTime;
    }

    /// <summary>
    /// 音量滑块值变化时的处理方法
    /// </summary>
    public void OnVolumeSliderChanged(float value)
    {
        audioSource.volume = value;
        // 音量变化时同步更新静音状态和图标
        if (value <= 0)
        {
            isMuted = true;
            audioSource.mute = true;
            volumeButton.image.sprite = volumeOffSprite;
            volumeButton.GetComponentInChildren<Text>().text = "取消静音";
        }
        else if (isMuted)
        {
            isMuted = false;
            audioSource.mute = false;
            volumeButton.image.sprite = volumeOnSprite;
            volumeButton.GetComponentInChildren<Text>().text = "静音";
        }
    }

    /// <summary>
    /// 更新当前播放时间的文本显示
    /// </summary>
    private void UpdateCurrentTimeText()
    {
        if (currentTimeText != null && videoPlayer.length > 0)
        {
            currentTimeText.text = FormatTime((float)videoPlayer.time);
        }
    }

    /// <summary>
    /// 更新视频总时长的文本显示
    /// </summary>
    private void UpdateTotalDurationText()
    {
        if (totalDurationText != null && videoPlayer.length > 0)
        {
            totalDurationText.text = FormatTime((float)videoPlayer.length);
        }
    }

    /// <summary>
    /// 时间格式化
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = (int)seconds / 60;
        int secs = (int)seconds % 60;
        return $"{minutes:D2}:{secs:D2}";
    }
    /// <summary>
    /// 循环播放开关状态变化时的回调
    /// 新增逻辑：若视频已播放完毕且开启循环，强制重新播放
    /// </summary>
    /// <param name="isLoop">开关是否开启（true为循环，false为不循环）</param>
    private void OnLoopToggleChanged(bool isLoop)
    {
        // 同步循环状态到视频播放器
        videoPlayer.isLooping = isLoop;

        // 核心新增逻辑：
        // 1. 当开启循环时
        // 2. 且视频当前已播放完毕（时间 >= 总时长）
        if (isLoop && videoPlayer.time >= videoPlayer.length - 0.1f) // 0.1f容错，避免精度问题
        {
            // 强制从头开始播放
            videoPlayer.time = 0;
            videoPlayer.Play();
        }

        Debug.Log($"循环播放状态已切换为：{isLoop}");
    }
    
    /// <summary>
    /// 视频播放到结尾时触发
    /// </summary>
    private void OnVideoEnded(VideoPlayer source)
    {
        // 如果循环开关开启，强制从头播放（兼容部分Unity版本的循环bug）
        if (loopToggle.isOn)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }
    /// <summary>
    /// 鼠标进入音量按钮时触发
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHoveringVolumeButton = true;
        isVolumeSliderActivated = true;  // 仅通过音量按钮激活滑块
    }

    /// <summary>
    /// 鼠标离开音量按钮时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHoveringVolumeButton = false;
    }

    /// <summary>
    /// 脚本销毁时执行的方法
    /// </summary>
    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }
}