using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LocalPlayer.Views;

namespace LocalPlayer;

public class MainForm : Form
{
    private MainPage mainPage;
    private PlayerPage? playerPage;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainForm()
    {
        this.Text = "LocalPlayer";
        this.StartPosition = FormStartPosition.Manual;
        this.BackColor = Color.FromArgb(20, 20, 20);
        
        // KeyPreview 现在可以不需要了，因为 ProcessCmdKey 会处理
        // this.KeyPreview = true;
        
        Console.WriteLine("[MainForm] 初始化完成，使用 ProcessCmdKey 处理键盘");
        
        SetSizeToScreenPercent(0.65);
        this.CenterToScreen();
        this.MinimumSize = new Size(800, 500);
        
        mainPage = new MainPage();
        mainPage.Dock = DockStyle.Fill;
        
        // 订阅事件
        mainPage.FolderSelected += MainPage_FolderSelected;
        
        this.Controls.Add(mainPage);
        this.Load += MainForm_Load;
    }

    // 统一使用 ProcessCmdKey 处理所有键盘事件
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // 只在播放页面显示时处理键盘事件
        if (playerPage != null && playerPage.Visible)
        {
            // 记录按键（避免刷屏，只记录功能键）
            if (IsFunctionKey(keyData))
            {
                Console.WriteLine($"[MainForm.ProcessCmdKey] 功能键: {keyData}");
            }
            
            // 创建 KeyEventArgs 并传递给 PlayerPage
            KeyEventArgs e = new KeyEventArgs(keyData);
            playerPage.HandleKeyDown(e);
            
            // 如果 PlayerPage 处理了该按键（通过检查 e.Handled），我们也返回 true
            if (e.Handled)
            {
                return true;
            }
        }
        
        // 调用基类方法处理其他按键
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // 判断是否是功能键（用于日志过滤）
    private bool IsFunctionKey(Keys keyData)
    {
        return keyData == Keys.Left || keyData == Keys.Right || 
               keyData == Keys.Up || keyData == Keys.Down || 
               keyData == Keys.Space || keyData == Keys.F ||
               keyData == Keys.Escape || keyData == Keys.M ||
               keyData == Keys.J || keyData == Keys.L ||
               keyData == Keys.N || keyData == Keys.P ||
               keyData == Keys.PageUp || keyData == Keys.PageDown;
    }

    // 事件处理方法
    private void MainPage_FolderSelected(object? sender, string folderPath, string folderName)
    {
        Console.WriteLine($"[MainForm] 选择文件夹: {folderPath}");
        
        // 移除旧播放页
        if (playerPage != null)
        {
            this.Controls.Remove(playerPage);
            playerPage.Dispose();
        }
        
        // 创建新播放页
        playerPage = new PlayerPage();
        playerPage.Dock = DockStyle.Fill;
        playerPage.BackRequested += PlayerPage_BackRequested;
        
        // 加载文件夹
        playerPage.LoadFolder(folderPath, folderName);
        
        // 切换显示
        mainPage.Visible = false;
        this.Controls.Add(playerPage);
        
        // 确保焦点在窗体上
        this.Focus();
        Console.WriteLine("[MainForm] 已切换到播放页面");
    }

    private void PlayerPage_BackRequested(object? sender, EventArgs e)
    {
        Console.WriteLine("[MainForm] 返回主页面");
        
        // 返回首页
        if (playerPage != null)
        {
            this.Controls.Remove(playerPage);
            playerPage.Dispose();
            playerPage = null;
        }
        mainPage.Visible = true;
    }

    private void SetSizeToScreenPercent(double percent)
    {
        Screen screen = Screen.FromControl(this);
        Rectangle workingArea = screen.WorkingArea;
        
        int width = (int)(workingArea.Width * percent);
        int height = (int)(workingArea.Height * percent);
        
        this.Size = new Size(width, height);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        int useDarkMode = 1;
        DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
        
        Console.WriteLine("[MainForm] 窗体加载完成");
    }
}