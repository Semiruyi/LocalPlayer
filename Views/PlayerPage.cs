using System;
using System.Drawing;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace LocalPlayer.Views;

public class PlayerPage : UserControl
{
    private LibVLC? libVLC;
    private MediaPlayer? mediaPlayer;
    private VideoView? videoView;
    private Panel? rightPanel;
    private ListBox? episodeList;
    private Button? backButton;

    private string[] videoFiles = Array.Empty<string>();
    private string currentFolderPath = "";

    public event EventHandler? BackRequested;
    public event KeyEventHandler? KeyDownHandler;

    public PlayerPage()
    {
        Console.WriteLine("[PlayerPage] 构造函数开始");

        this.BackColor = Color.FromArgb(20, 20, 20);
        this.Dock = DockStyle.Fill;

        SetupUI();
        SetupVLC();

        Console.WriteLine("[PlayerPage] 初始化完成");
    }

    private void SetupUI()
    {
        // 左侧视频区域 (70%)
        videoView = new VideoView
        {
            Dock = DockStyle.None,
            BackColor = Color.Black
        };

        // 右侧选集面板 (30%)
        rightPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = (int)(this.Parent?.Width * 0.3 ?? 300),
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // 返回按钮
        backButton = new Button
        {
            Text = "← 返回",
            Font = new Font("微软雅黑", 12),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0, 122, 204),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(10, 10),
            Size = new Size(100, 35),
            Cursor = Cursors.Hand
        };
        backButton.FlatAppearance.BorderSize = 0;
        backButton.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

        // 选集列表
        episodeList = new ListBox
        {
            Location = new Point(10, 50),
            Size = new Size(rightPanel.Width - 20, 400),
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 11),
            BorderStyle = BorderStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        episodeList.SelectedIndexChanged += EpisodeList_SelectedIndexChanged;

        rightPanel.Controls.Add(backButton);
        rightPanel.Controls.Add(episodeList);

        this.Controls.Add(videoView);
        this.Controls.Add(rightPanel);

        this.Resize += PlayerPage_Resize;
    }

    private void SetupVLC()
    {
        libVLC = new LibVLC();
        mediaPlayer = new MediaPlayer(libVLC);

        if (videoView != null)
        {
            videoView.MediaPlayer = mediaPlayer;
        }

        Console.WriteLine("[VLC] 初始化完成");
    }

    private void PlayerPage_Resize(object? sender, EventArgs e)
    {
        if (videoView != null && rightPanel != null)
        {
            videoView.Size = new Size(this.ClientSize.Width - rightPanel.Width, this.ClientSize.Height);
            videoView.Location = new Point(0, 0);

            rightPanel.Height = this.ClientSize.Height;
            episodeList!.Height = this.ClientSize.Height - 120;
        }
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        // 只记录功能键，避免刷屏
        if (IsFunctionKey(e.KeyCode))
        {
            Console.WriteLine($"[PlayerPage] 处理按键: {e.KeyCode}");
        }

        bool handled = true;

        switch (e.KeyCode)
        {
            case Keys.Space:
                Console.WriteLine("[空格键] ✓ 暂停/播放");
                TogglePlayPause();
                break;

            case Keys.Left:
                Console.WriteLine("[左键] ✓ 后退5秒");
                SeekBackward(5000);
                break;

            case Keys.Right:
                Console.WriteLine("[右键] ✓ 前进5秒");
                SeekForward(5000);
                break;

            case Keys.Up:
                Console.WriteLine("[上键] ✓ 增加音量");
                IncreaseVolume(10);
                break;

            case Keys.Down:
                Console.WriteLine("[下键] ✓ 减少音量");
                DecreaseVolume(10);
                break;

            case Keys.F:
                Console.WriteLine("[F键] ✓ 切换全屏");
                ToggleFullScreen();
                break;

            case Keys.Escape:
                Console.WriteLine("[ESC键] ✓ 退出全屏");
                ExitFullScreen();
                break;

            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    // 判断是否是功能键
    private bool IsFunctionKey(Keys keyCode)
    {
        return keyCode == Keys.Left || keyCode == Keys.Right ||
               keyCode == Keys.Up || keyCode == Keys.Down ||
               keyCode == Keys.Space || keyCode == Keys.F ||
               keyCode == Keys.Escape || keyCode == Keys.M;
    }
    // 前进/后退方法
    private void SeekForward(long milliseconds)
    {
        if (mediaPlayer == null || mediaPlayer.Length <= 0)
        {
            Console.WriteLine("[VLC] ✗ 无法跳转：播放器未就绪或视频未加载");
            return;
        }

        long currentTime = mediaPlayer.Time;
        long totalLength = mediaPlayer.Length;
        long newTime = Math.Min(totalLength, currentTime + milliseconds);

        Console.WriteLine($"[VLC] 前进: {FormatTime(currentTime)} -> {FormatTime(newTime)} (前进 {milliseconds / 1000}秒)");
        mediaPlayer.Time = newTime;
    }

    private void SeekBackward(long milliseconds)
    {
        if (mediaPlayer == null || mediaPlayer.Length <= 0)
        {
            Console.WriteLine("[VLC] ✗ 无法跳转：播放器未就绪或视频未加载");
            return;
        }

        long currentTime = mediaPlayer.Time;
        long newTime = Math.Max(0, currentTime - milliseconds);

        Console.WriteLine($"[VLC] 后退: {FormatTime(currentTime)} -> {FormatTime(newTime)} (后退 {milliseconds / 1000}秒)");
        mediaPlayer.Time = newTime;
    }

    // 音量控制
    private void IncreaseVolume(int amount)
    {
        if (mediaPlayer == null) return;

        int newVolume = Math.Min(100, mediaPlayer.Volume + amount);
        mediaPlayer.Volume = newVolume;
        Console.WriteLine($"[VLC] 音量增加: {mediaPlayer.Volume - amount}% -> {newVolume}%");
    }

    private void DecreaseVolume(int amount)
    {
        if (mediaPlayer == null) return;

        int newVolume = Math.Max(0, mediaPlayer.Volume - amount);
        mediaPlayer.Volume = newVolume;
        Console.WriteLine($"[VLC] 音量减少: {mediaPlayer.Volume + amount}% -> {newVolume}%");
    }

    private void ToggleMute()
    {
        if (mediaPlayer == null) return;

        mediaPlayer.Mute = !mediaPlayer.Mute;
        Console.WriteLine($"[VLC] 静音: {(mediaPlayer.Mute ? "开启" : "关闭")}");
    }

    // 全屏控制
    private void ToggleFullScreen()
    {
        var form = this.FindForm();
        if (form != null)
        {
            form.WindowState = form.WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            Console.WriteLine($"[窗口] 全屏切换: {form.WindowState}");
        }
    }

    private void ExitFullScreen()
    {
        var form = this.FindForm();
        if (form != null && form.WindowState == FormWindowState.Maximized)
        {
            form.WindowState = FormWindowState.Normal;
            Console.WriteLine("[窗口] 退出全屏");
        }
    }

    // 剧集切换
    private void PlayNextEpisode()
    {
        if (episodeList == null || episodeList.Items.Count == 0) return;

        int nextIndex = episodeList.SelectedIndex + 1;
        if (nextIndex < episodeList.Items.Count)
        {
            episodeList.SelectedIndex = nextIndex;
            Console.WriteLine($"[选集] 切换到下一集: 第 {nextIndex + 1} 集");
        }
        else
        {
            Console.WriteLine("[选集] 已经是最后一集");
        }
    }

    private void PlayPreviousEpisode()
    {
        if (episodeList == null || episodeList.Items.Count == 0) return;

        int prevIndex = episodeList.SelectedIndex - 1;
        if (prevIndex >= 0)
        {
            episodeList.SelectedIndex = prevIndex;
            Console.WriteLine($"[选集] 切换到上一集: 第 {prevIndex + 1} 集");
        }
        else
        {
            Console.WriteLine("[选集] 已经是第一集");
        }
    }

    // 辅助方法：格式化时间
    private string FormatTime(long milliseconds)
    {
        TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    public void TogglePlayPause()
    {
        if (mediaPlayer == null)
        {
            Console.WriteLine("[VLC] ✗ mediaPlayer 为空，无法切换播放状态");
            return;
        }

        if (mediaPlayer.IsPlaying)
        {
            Console.WriteLine("[VLC] ⏸ 暂停播放");
            mediaPlayer.Pause();
        }
        else
        {
            Console.WriteLine("[VLC] ▶ 恢复播放");
            mediaPlayer.Play();
        }
    }

    public void LoadFolder(string folderPath, string folderName)
    {
        currentFolderPath = folderPath;
        Console.WriteLine($"[PlayerPage] 加载文件夹: {folderPath}");

        // 扫描视频文件
        videoFiles = Services.VideoScanner.GetVideoFiles(folderPath);
        Console.WriteLine($"[PlayerPage] 找到 {videoFiles.Length} 个视频文件");

        // 填充选集列表
        episodeList!.Items.Clear();
        for (int i = 0; i < videoFiles.Length; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(videoFiles[i]);
            episodeList.Items.Add($"{i + 1:00}. {fileName}");
        }

        // 自动播放第一集
        if (videoFiles.Length > 0)
        {
            PlayVideo(videoFiles[0]);
            episodeList.SelectedIndex = 0;
        }
    }

    private void PlayVideo(string filePath)
    {
        if (mediaPlayer == null || libVLC == null)
        {
            Console.WriteLine("[VLC] ✗ mediaPlayer 或 libVLC 为空，无法播放");
            return;
        }

        Console.WriteLine($"[VLC] 开始播放: {Path.GetFileName(filePath)}");
        var media = new Media(libVLC, filePath);
        mediaPlayer.Play(media);
    }

    private void EpisodeList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (episodeList!.SelectedIndex >= 0 && episodeList.SelectedIndex < videoFiles.Length)
        {
            Console.WriteLine($"[选集] 切换到第 {episodeList.SelectedIndex + 1} 集");
            PlayVideo(videoFiles[episodeList.SelectedIndex]);
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Console.WriteLine("[PlayerPage] 正在销毁资源...");
        mediaPlayer?.Stop();
        mediaPlayer?.Dispose();
        libVLC?.Dispose();
        base.OnHandleDestroyed(e);
    }
}