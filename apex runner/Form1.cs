using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using apex_runner.Properties;
using System.Security.Policy;
using System.Runtime.InteropServices;

namespace apex_runner
{
    public partial class Form1 : Form
    {
        // 添加 Windows API 函数声明
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 定义一些常量
        private const int HOTKEY_ID = 9000;
        private const string DEFAULT_SHORTCUT = "ControlKey + Menu + K";
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private bool isLoadingSettings = false;

        public Form1()
        {
            InitializeComponent();
            InitializeOneClickMenus();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;   
        }
        //让窗口可以拖拽
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        protected override void WndProc(ref Message message)
        {
            const int WM_HOTKEY = 0x0312;

            switch (message.Msg)
            {
                case WM_HOTKEY:
                    if (message.WParam.ToInt32() == HOTKEY_ID)
                    {
                        // 在这里处理热键事件
                        HandleHotKey();
                    }
                    break;
                
                case WM_NCHITTEST:
                    base.WndProc(ref message);
                    if ((int)message.Result == HTCLIENT)
                        message.Result = (IntPtr)HTCAPTION;
                    return;
            }
            
            base.WndProc(ref message);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            isLoadingSettings = true;
            //获取设置中的 uu 加速器路径\语音软件\ Steam 位置
            textBox1.Text = Settings.Default.uupath;
            textBox2.Text = Settings.Default.oopzpath;
            textBox3.Text = Settings.Default.steampath;

            // 加载保存的快捷键
            string savedShortcut = Settings.Default.shortcut;
            if (!string.IsNullOrEmpty(savedShortcut))
            {
                textBoxShortcut.Text = savedShortcut;
                BindShortcut(savedShortcut);
            }
            else
            {
                textBoxShortcut.Text = DEFAULT_SHORTCUT;
                BindShortcut(DEFAULT_SHORTCUT);
            }

            LoadAudioOutputDevices();
            checkBoxStartOptimize.Checked = Settings.Default.startOneClickOptimize;
            checkBoxCloseRestore.Checked = Settings.Default.closeOneClickRestore;

            isLoadingSettings = false;

            if (Settings.Default.startOneClickOptimize)
            {
                button1_Click(this, EventArgs.Empty);
            }
            else
            {
                //获取当前输入法状态并且标识
                if(InputMethod.CurrentMethod() == InputMethod.InputMethodType.Chinese)
                {
                    radioButton_chinese.Checked = true;
                }
                else
                {
                    radioButton_eng.Checked = true;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Settings.Default.closeOneClickRestore)
            {
                button2_Click(this, EventArgs.Empty);
            }

            base.OnFormClosing(e);
            // 注销所有热键
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            if (currentHandler != null)
            {
                this.KeyDown -= currentHandler;
                currentHandler = null;
            }
        }

        //禁用 Windows 键功能
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            if (radioButton2.Checked)
            {
                WindowsKey.Disable();
            }
            else if (radioButton1.Checked)
            {
                WindowsKey.Enable();
            }

            SaveLastSelection();
        }

        //切换英文输入法功能
        private void radioButton_eng_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            if (radioButton_eng.Checked)
            {
                InputMethod.ChangeToEnglish();
            }
            else if (radioButton_chinese.Checked)
            {
                InputMethod.ChangeToChinese();
            }

            SaveLastSelection();
        }

        //切换 alt shift 开关功能
        private void radioButtonaltshiftOff_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            if (radioButtonaltshiftOff.Checked)
            {
                AltShift.Disable();
            }
            else if (radioButtonaltshiftOn.Checked)
            {
                AltShift.Enable();
            }

            SaveLastSelection();
        }

        //手抖多点出来的,懒得删了
        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        //一键优化 开启游戏模式
        private void button1_Click(object sender, EventArgs e)
        {
            ApplyOneClickSelection(
                Settings.Default.optimizeWinKeyDisabled,
                Settings.Default.optimizeInputEnglish,
                Settings.Default.optimizeAltShiftDisabled,
                Settings.Default.optimizeAudioDeviceId);
            SaveLastSelection();
        }

        //一键恢复 开启正常电脑模式
        private void button2_Click(object sender, EventArgs e)
        {
            ApplyOneClickSelection(
                Settings.Default.restoreWinKeyDisabled,
                Settings.Default.restoreInputEnglish,
                Settings.Default.restoreAltShiftDisabled,
                Settings.Default.restoreAudioDeviceId);
            SaveLastSelection();
        }

        //textbox1 在修改时自动保存加速器路径
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.uupath = textBox1.Text;
            Settings.Default.Save();
        }
        //textbox2 在修改时自动保存路径
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.oopzpath = textBox2.Text;
            Settings.Default.Save();
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.steampath = textBox3.Text;
            Settings.Default.Save();
        }
        //右下关于按钮
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Apex Runner\n" +
                "版本号: v2.0\n" +
                "项目地址: https://github.com/wood02/apex_runner\n\n" +
                "这个程序是免费的。\n" +
                "如果它对你有帮助，欢迎在 GitHub 点一个 Star。");
        }
        
        //右下角链接 
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl("https://github.com/wood02/apex_runner");
        }

        //用于启动加速器的启动函数
        static void StartProgram(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("程序路径不存在或无法启动，请检查路径是否正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 创建一个新的进程启动信息
                ProcessStartInfo startInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 启动程序
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法启动程序：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //用于开启网页链接的函数
        static void OpenUrl(string url)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("无法打开URL：" + ex.Message);
            }
        }

        //开启加速器 图片按钮
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text;
            StartProgram(path);
        }
        //开启语音软件
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            string path = textBox2.Text;
            StartProgram(path);
        }
        //开起 Steam
        private void pictureBox_steam_Click(object sender, EventArgs e)
        {
            string path = textBox3.Text;
            StartProgram(path);
        }


        //打开显示设置
        private void pictureBox4_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开Windows的显示设置
                Process.Start("ms-settings:display");
                Console.WriteLine("显示设置已打开。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("无法打开显示设置: " + ex.Message);
            }
        }
        //双击开启音频设置
        private void pictureBox4_DoubleClick(object sender, EventArgs e)
        {
            Process.Start("ms-settings:apps-volume");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        //点击 apex 大图 启动steam
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        bool IsFloded = true;
        private void button3_Click(object sender, EventArgs e)
        {
            double dpi = GetDpiPercent();
            double dpi00 = dpi / 100; //这个参数用来计算展开和收起时,嗯对于hi DPI 屏幕的影响
            if (IsFloded)
            {
                this.Height = Convert.ToInt32(490 * dpi00);
                IsFloded = false;
                button3.Text = "收起路径设置";
            }
            else
            {
                this.Height = Convert.ToInt32(310 * dpi00);
                IsFloded = true;
                button3.Text = "展开路径设置";

            }

        }
        //模拟缩小操作
        private void button4_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        //关闭按钮
        private void exitbutton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //在窗体被激活时,同时将窗体变成常规状态,这样在尝试开启多个实力时
        //窗体不会以最小化状态被激活(视觉效果就是不会弹出来)
        private void Form1_Activated(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }


        // private void Form1_KeyDown(object sender, KeyEventArgs e)
        // {
            //// 监控具体按键
            ////switch e.KeyCode 并执行功能 (模拟点击)
            ////1. enter 执行 button1
            ////2. BackSpace 执行 button2
            ////3. u 执行 picturebox2
            ////4. d 执行 picturebox4
            ////5. v 执行 picturebox4 双击功能
            ////6. s 执行 pictureBox_steam 
            ////7. o 执行 pictureBox3
            //// 使用 switch 语句监控具体按键
            //switch (e.KeyCode)
            //{
            //    case Keys.Enter:
            //        // 模拟点击 button1
            //        button1.PerformClick();
            //        break;

            //    case Keys.Back:
            //        // 模拟点击 button2
            //        button2.PerformClick();
            //        break;

            //    case Keys.U:
            //        // 模拟点击 pictureBox2
            //        pictureBox2_Click(sender, EventArgs.Empty);
            //        break;

            //    case Keys.D:
            //        // 模拟点击 pictureBox4
            //        pictureBox4_Click(sender, EventArgs.Empty);
            //        break;

            //    case Keys.V:
            //        // 模拟双击 pictureBox4
            //        pictureBox4_DoubleClick(sender, EventArgs.Empty);
            //        break;

            //    case Keys.S:
            //        // 模拟点击 pictureBox_steam
            //        pictureBox_steam_Click(sender, EventArgs.Empty);
            //        break;

            //    case Keys.O:
            //        // 模拟点击 pictureBox3
            //        pictureBox3_Click(sender, EventArgs.Empty);
            //        break;

            //    default:
            //        // 处理其他按键
            //        break;
            //}
        // }
        //获取当前屏幕 DPI
        public double GetDpiPercent()
        {
            double dpiX, dpiY;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }

            // 默认DPI为96（100%），计算DPI百分比
            double dpiPercentageX = (dpiX / 96) * 100;
            double dpiPercentageY = (dpiY / 96) * 100;
            return dpiPercentageX;
            //MessageBox.Show($"DPI 百分比: {dpiPercentageX}% (水平), {dpiPercentageY}% (垂直)");
        }

        //kill ahk
        private void button5_Click(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("AutoHotkey")
                .Concat(Process.GetProcessesByName("kasusa_util"))
                .ToArray();

            if (processes.Length == 0)
            {
                MessageBox.Show("没有找到 AutoHotkey 或 kasusa_util 进程。");
                return;
            }

            DialogResult result = MessageBox.Show(
                "将终止 " + processes.Length + " 个 AutoHotkey/kasusa_util 进程，确定继续吗？",
                "确认终止进程",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            int killedCount = 0;
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    killedCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法终止 " + process.ProcessName + ": " + ex.Message);
                }
            }

            MessageBox.Show("成功终止了 " + killedCount + " 个进程。");
        }

        private bool isRecordingShortcut = false;
        private bool isLoadingAudioOutputDevices = false;
        private List<string> currentKeys = new List<string>();

        private void buttonShortcut_Click(object sender, EventArgs e)
        {
            // 开始记录快捷键
            isRecordingShortcut = true;
            currentKeys.Clear();
            textBoxShortcut.Text = "按下快捷键...";
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isRecordingShortcut) return;

            string keyName = e.KeyCode.ToString();
            if (!currentKeys.Contains(keyName))
            {
                currentKeys.Add(keyName);
            }

            // 更新显示
            textBoxShortcut.Text = string.Join(" + ", currentKeys);
            e.Handled = true;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (!isRecordingShortcut) return;

            // 当所有按键都释放时，结束记录
            if (!e.Control && !e.Alt && !e.Shift && 
                e.KeyCode != Keys.ControlKey && 
                e.KeyCode != Keys.ShiftKey && 
                e.KeyCode != Keys.Menu)
            {
                isRecordingShortcut = false;
                this.KeyPreview = false;
                this.KeyDown -= Form1_KeyDown;
                this.KeyUp -= Form1_KeyUp;

                string shortcutStr = string.Join(" + ", currentKeys);
                SaveShortcut(shortcutStr);
            }

            e.Handled = true;
        }

        private int clickCount = 0;
        private KeyEventHandler currentHandler = null;
        private ContextMenuStrip optimizeMenu;
        private ContextMenuStrip restoreMenu;
        private ToolStripMenuItem optimizeWinEnableItem;
        private ToolStripMenuItem optimizeWinDisableItem;
        private ToolStripMenuItem optimizeInputChineseItem;
        private ToolStripMenuItem optimizeInputEnglishItem;
        private ToolStripMenuItem optimizeAltShiftEnableItem;
        private ToolStripMenuItem optimizeAltShiftDisableItem;
        private ToolStripMenuItem optimizeAudioOffItem;
        private ToolStripMenuItem optimizeAudioOnItem;
        private ToolStripMenuItem restoreWinEnableItem;
        private ToolStripMenuItem restoreWinDisableItem;
        private ToolStripMenuItem restoreInputChineseItem;
        private ToolStripMenuItem restoreInputEnglishItem;
        private ToolStripMenuItem restoreAltShiftEnableItem;
        private ToolStripMenuItem restoreAltShiftDisableItem;
        private ToolStripMenuItem restoreAudioOffItem;
        private ToolStripMenuItem restoreAudioOnItem;

        private void InitializeOneClickMenus()
        {
            optimizeMenu = new ContextMenuStrip();
            optimizeWinEnableItem = CreateOneClickPairItem("Win 键: 启用", "opt_win", false);
            optimizeWinDisableItem = CreateOneClickPairItem("Win 键: 禁用", "opt_win", true);
            optimizeInputChineseItem = CreateOneClickPairItem("输入法: 中文", "opt_input", false);
            optimizeInputEnglishItem = CreateOneClickPairItem("输入法: 英文", "opt_input", true);
            optimizeAltShiftEnableItem = CreateOneClickPairItem("Alt+Shift: 启用", "opt_altshift", false);
            optimizeAltShiftDisableItem = CreateOneClickPairItem("Alt+Shift: 禁用", "opt_altshift", true);
            optimizeAudioOffItem = CreateOneClickPairItem("声音输出: 设备1", "opt_audio", false);
            optimizeAudioOnItem = CreateOneClickPairItem("声音输出: 设备2", "opt_audio", true);
            optimizeMenu.Items.AddRange(new ToolStripItem[]
            {
                optimizeWinEnableItem,
                optimizeWinDisableItem,
                new ToolStripSeparator(),
                optimizeInputChineseItem,
                optimizeInputEnglishItem,
                new ToolStripSeparator(),
                optimizeAltShiftEnableItem,
                optimizeAltShiftDisableItem,
                new ToolStripSeparator(),
                optimizeAudioOffItem,
                optimizeAudioOnItem
            });

            restoreMenu = new ContextMenuStrip();
            restoreWinEnableItem = CreateOneClickPairItem("Win 键: 启用", "res_win", false);
            restoreWinDisableItem = CreateOneClickPairItem("Win 键: 禁用", "res_win", true);
            restoreInputChineseItem = CreateOneClickPairItem("输入法: 中文", "res_input", false);
            restoreInputEnglishItem = CreateOneClickPairItem("输入法: 英文", "res_input", true);
            restoreAltShiftEnableItem = CreateOneClickPairItem("Alt+Shift: 启用", "res_altshift", false);
            restoreAltShiftDisableItem = CreateOneClickPairItem("Alt+Shift: 禁用", "res_altshift", true);
            restoreAudioOffItem = CreateOneClickPairItem("声音输出: 设备1", "res_audio", false);
            restoreAudioOnItem = CreateOneClickPairItem("声音输出: 设备2", "res_audio", true);
            restoreMenu.Items.AddRange(new ToolStripItem[]
            {
                restoreWinEnableItem,
                restoreWinDisableItem,
                new ToolStripSeparator(),
                restoreInputChineseItem,
                restoreInputEnglishItem,
                new ToolStripSeparator(),
                restoreAltShiftEnableItem,
                restoreAltShiftDisableItem,
                new ToolStripSeparator(),
                restoreAudioOffItem,
                restoreAudioOnItem
            });

            optimizeMenu.Opening += OneClickMenu_Opening;
            restoreMenu.Opening += OneClickMenu_Opening;
            NormalizeOneClickPairSettings();
            UpdateOneClickPairMenuChecks();
            button1.ContextMenuStrip = optimizeMenu;
            button2.ContextMenuStrip = restoreMenu;
        }

        private void OneClickMenu_Opening(object sender, CancelEventArgs e)
        {
            NormalizeOneClickPairSettings();
            UpdateOneClickPairMenuChecks();
        }

        private ToolStripMenuItem CreateOneClickPairItem(string text, string groupKey, bool value)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Tag = new OneClickMenuTag(groupKey, value);
            item.Click += OneClickPairItem_Click;
            return item;
        }

        private void OneClickPairItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            OneClickMenuTag tag = item == null ? null : item.Tag as OneClickMenuTag;
            if (tag == null)
            {
                return;
            }

            SaveOneClickPairSetting(tag.GroupKey, tag.Value);
            UpdateOneClickPairMenuChecks();
            Settings.Default.Save();
        }

        private void SaveOneClickPairSetting(string groupKey, bool value)
        {
            if (groupKey == "opt_win")
            {
                Settings.Default.optimizeWinKeyDisabled = value;
            }
            else if (groupKey == "opt_input")
            {
                Settings.Default.optimizeInputEnglish = value;
            }
            else if (groupKey == "opt_altshift")
            {
                Settings.Default.optimizeAltShiftDisabled = value;
            }
            else if (groupKey == "opt_audio")
            {
                SetOneClickAudioDevice(true, value);
            }
            else if (groupKey == "res_win")
            {
                Settings.Default.restoreWinKeyDisabled = value;
            }
            else if (groupKey == "res_input")
            {
                Settings.Default.restoreInputEnglish = value;
            }
            else if (groupKey == "res_altshift")
            {
                Settings.Default.restoreAltShiftDisabled = value;
            }
            else if (groupKey == "res_audio")
            {
                SetOneClickAudioDevice(false, value);
            }
            SyncLegacyOneClickSettings();
        }

        private void SetOneClickAudioDevice(bool isOptimizeMenu, bool secondDeviceSelected)
        {
            AudioOutputDevice selectedDevice = GetAudioDeviceForMenuValue(secondDeviceSelected);

            if (selectedDevice == null)
            {
                return;
            }

            if (isOptimizeMenu)
            {
                Settings.Default.optimizeAudioDeviceId = selectedDevice.Id;
            }
            else
            {
                Settings.Default.restoreAudioDeviceId = selectedDevice.Id;
            }
        }

        private void UpdateOneClickPairMenuChecks()
        {
            if (optimizeWinEnableItem == null)
            {
                return;
            }

            optimizeWinEnableItem.Checked = !Settings.Default.optimizeWinKeyDisabled;
            optimizeWinDisableItem.Checked = Settings.Default.optimizeWinKeyDisabled;
            optimizeInputChineseItem.Checked = !Settings.Default.optimizeInputEnglish;
            optimizeInputEnglishItem.Checked = Settings.Default.optimizeInputEnglish;
            optimizeAltShiftEnableItem.Checked = !Settings.Default.optimizeAltShiftDisabled;
            optimizeAltShiftDisableItem.Checked = Settings.Default.optimizeAltShiftDisabled;
            UpdateAudioMenuLabelsAndChecks();

            restoreWinEnableItem.Checked = !Settings.Default.restoreWinKeyDisabled;
            restoreWinDisableItem.Checked = Settings.Default.restoreWinKeyDisabled;
            restoreInputChineseItem.Checked = !Settings.Default.restoreInputEnglish;
            restoreInputEnglishItem.Checked = Settings.Default.restoreInputEnglish;
            restoreAltShiftEnableItem.Checked = !Settings.Default.restoreAltShiftDisabled;
            restoreAltShiftDisableItem.Checked = Settings.Default.restoreAltShiftDisabled;
        }

        private void NormalizeOneClickPairSettings()
        {
            if (string.IsNullOrEmpty(Settings.Default.optimizeAudioDeviceId) ||
                string.IsNullOrEmpty(Settings.Default.restoreAudioDeviceId))
            {
                AudioOutputDevice firstDevice = GetAudioDeviceForMenuValue(false);
                AudioOutputDevice secondDevice = GetAudioDeviceForMenuValue(true);
                if (string.IsNullOrEmpty(Settings.Default.optimizeAudioDeviceId) && firstDevice != null)
                {
                    Settings.Default.optimizeAudioDeviceId = firstDevice.Id;
                }
                if (string.IsNullOrEmpty(Settings.Default.restoreAudioDeviceId))
                {
                    if (secondDevice != null) Settings.Default.restoreAudioDeviceId = secondDevice.Id;
                    else if (firstDevice != null) Settings.Default.restoreAudioDeviceId = firstDevice.Id;
                }
            }

            SyncLegacyOneClickSettings();
            Settings.Default.Save();
        }

        private void UpdateAudioMenuLabelsAndChecks()
        {
            AudioOutputDevice firstDevice = GetAudioDeviceForMenuValue(false);
            AudioOutputDevice secondDevice = GetAudioDeviceForMenuValue(true);

            optimizeAudioOffItem.Text = "声音输出: " + GetAudioMenuText(firstDevice, "设备1");
            optimizeAudioOnItem.Text = "声音输出: " + GetAudioMenuText(secondDevice, "设备2");
            restoreAudioOffItem.Text = optimizeAudioOffItem.Text;
            restoreAudioOnItem.Text = optimizeAudioOnItem.Text;

            EnsureOneClickAudioDeviceDefaults(firstDevice, secondDevice);

            optimizeAudioOffItem.Checked = firstDevice != null && Settings.Default.optimizeAudioDeviceId == firstDevice.Id;
            optimizeAudioOnItem.Checked = secondDevice != null && Settings.Default.optimizeAudioDeviceId == secondDevice.Id;
            restoreAudioOffItem.Checked = firstDevice != null && Settings.Default.restoreAudioDeviceId == firstDevice.Id;
            restoreAudioOnItem.Checked = secondDevice != null && Settings.Default.restoreAudioDeviceId == secondDevice.Id;
        }

        private string GetAudioMenuText(AudioOutputDevice device, string fallback)
        {
            return device == null ? fallback : GetAudioDeviceDisplayName(device.Name);
        }

        private void EnsureOneClickAudioDeviceDefaults(AudioOutputDevice firstDevice, AudioOutputDevice secondDevice)
        {
            if (string.IsNullOrEmpty(Settings.Default.optimizeAudioDeviceId) && firstDevice != null)
            {
                Settings.Default.optimizeAudioDeviceId = firstDevice.Id;
            }

            if (string.IsNullOrEmpty(Settings.Default.restoreAudioDeviceId))
            {
                if (secondDevice != null) Settings.Default.restoreAudioDeviceId = secondDevice.Id;
                else if (firstDevice != null) Settings.Default.restoreAudioDeviceId = firstDevice.Id;
            }
        }

        private void SyncLegacyOneClickSettings()
        {
            Settings.Default.optimizeWinKey = true;
            Settings.Default.optimizeInputMethod = true;
            Settings.Default.optimizeAltShift = true;
            Settings.Default.optimizeAudioOutput = !string.IsNullOrEmpty(Settings.Default.optimizeAudioDeviceId);
            Settings.Default.restoreWinKey = true;
            Settings.Default.restoreInputMethod = true;
            Settings.Default.restoreAltShift = true;
            Settings.Default.restoreAudioOutput = !string.IsNullOrEmpty(Settings.Default.restoreAudioDeviceId);
            Settings.Default.audioSwitchEnabled = Settings.Default.optimizeAudioOutput;
        }

        private void ApplyOneClickSelection(bool winKeyDisabled, bool inputEnglish, bool altShiftDisabled, string audioDeviceId)
        {
            if (winKeyDisabled) radioButton2.Checked = true;
            else radioButton1.Checked = true;

            if (inputEnglish) radioButton_eng.Checked = true;
            else radioButton_chinese.Checked = true;

            if (altShiftDisabled) radioButtonaltshiftOff.Checked = true;
            else radioButtonaltshiftOn.Checked = true;

            if (!string.IsNullOrEmpty(audioDeviceId))
            {
                SetAudioOutputDefault(audioDeviceId, false);
            }
        }

        private class OneClickMenuTag
        {
            public string GroupKey { get; private set; }
            public bool Value { get; private set; }

            public OneClickMenuTag(string groupKey, bool value)
            {
                GroupKey = groupKey;
                Value = value;
            }
        }

        private bool BindShortcut(string shortcutStr)
        {
            uint modifiers;
            Keys mainKey;
            string errorMessage;

            if (!TryParseShortcut(shortcutStr, out modifiers, out mainKey, out errorMessage))
            {
                MessageBox.Show(errorMessage, "快捷键无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 如果已有处理程序,先移除
            if (currentHandler != null)
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                this.KeyDown -= currentHandler;
                currentHandler = null;
            }

            // 注册全局热键
            if (!RegisterHotKey(this.Handle, HOTKEY_ID, modifiers, (uint)mainKey))
            {
                MessageBox.Show("热键注册失败！可能是该热键已被其他程序占用。");
                return false;
            }

            // 更新label8显示
            label8.Text = "当前快捷键: " + shortcutStr;

            // 保存当前的按键设置
            currentHandler = (sender, e) =>
            {
                bool match = e.KeyCode == mainKey;
                if ((modifiers & MOD_CONTROL) != 0 && (Control.ModifierKeys & Keys.Control) == 0) match = false;
                if ((modifiers & MOD_ALT) != 0 && (Control.ModifierKeys & Keys.Alt) == 0) match = false;
                if ((modifiers & MOD_SHIFT) != 0 && (Control.ModifierKeys & Keys.Shift) == 0) match = false;

                if (match)
                {
                    HandleHotKey();
                    e.Handled = true;
                }
            };

            this.KeyDown += currentHandler;
            return true;
        }

        private bool TryParseShortcut(string shortcutStr, out uint modifiers, out Keys mainKey, out string errorMessage)
        {
            modifiers = 0;
            mainKey = Keys.None;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(shortcutStr))
            {
                errorMessage = "快捷键不能为空。";
                return false;
            }

            string[] keys = shortcutStr.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(key => key.Trim())
                .Where(key => !string.IsNullOrEmpty(key))
                .ToArray();

            foreach (string key in keys)
            {
                if (key == "ControlKey" || key == "Control" || key == "Ctrl")
                {
                    modifiers |= MOD_CONTROL;
                }
                else if (key == "Alt" || key == "Menu")
                {
                    modifiers |= MOD_ALT;
                }
                else if (key == "ShiftKey" || key == "Shift")
                {
                    modifiers |= MOD_SHIFT;
                }
                else
                {
                    Keys parsedKey;
                    if (!Enum.TryParse(key, true, out parsedKey))
                    {
                        errorMessage = "无法识别快捷键: " + key;
                        return false;
                    }

                    mainKey = parsedKey;
                }
            }

            if (mainKey == Keys.None)
            {
                errorMessage = "快捷键需要包含一个普通按键，例如 K。";
                return false;
            }

            return true;
        }

        private void SaveShortcut(string shortcutStr)
        {
            if (!BindShortcut(shortcutStr))
            {
                return;
            }

            Settings.Default.shortcut = shortcutStr;
            Settings.Default.Save();
        }

        private void LoadAudioOutputDevices()
        {
            isLoadingAudioOutputDevices = true;
            ClearAudioOutputRadioButtons();

            try
            {
                List<AudioOutputDevice> devices = AudioOutputDevice.GetActiveRenderDevices();
                List<AudioOutputDevice> visibleDevices = GetVisibleAudioOutputDevices(
                    devices,
                    Settings.Default.audioDeviceId,
                    Settings.Default.optimizeAudioDeviceId,
                    Settings.Default.restoreAudioDeviceId);
                ApplyPreferredAudioDeviceOrder(visibleDevices);
                BindAudioOutputRadioButton(radioButtonAudio1, visibleDevices.Count > 0 ? visibleDevices[0] : null);
                BindAudioOutputRadioButton(radioButtonAudio2, visibleDevices.Count > 1 ? visibleDevices[1] : null);
                UpdateOneClickPairMenuChecks();

                SelectAudioOutputDevice(Settings.Default.audioDeviceId);

                if (GetSelectedAudioOutputDevice() == null)
                {
                    RadioButton radioButton = GetFirstAudioOutputRadioButton();
                    if (radioButton != null)
                    {
                        radioButton.Checked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法读取声音输出设备: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isLoadingAudioOutputDevices = false;
            }
        }

        private List<AudioOutputDevice> GetVisibleAudioOutputDevices(
            List<AudioOutputDevice> devices,
            string selectedDeviceId,
            string optimizeAudioDeviceId,
            string restoreAudioDeviceId)
        {
            List<AudioOutputDevice> visibleDevices = new List<AudioOutputDevice>();

            AddAudioDeviceById(visibleDevices, devices, selectedDeviceId);
            AddAudioDeviceById(visibleDevices, devices, optimizeAudioDeviceId);
            AddAudioDeviceById(visibleDevices, devices, restoreAudioDeviceId);

            foreach (AudioOutputDevice device in devices)
            {
                if (visibleDevices.Count >= 2)
                {
                    break;
                }

                if (!ContainsAudioDevice(visibleDevices, device.Id))
                {
                    visibleDevices.Add(device);
                }
            }

            return visibleDevices;
        }

        private void ApplyPreferredAudioDeviceOrder(List<AudioOutputDevice> devices)
        {
            int headsetIndex = FindAudioDeviceIndex(devices, "头戴");
            int marshallIndex = FindAudioDeviceIndex(devices, "马歇尔");
            if (marshallIndex < 0)
            {
                marshallIndex = FindAudioDeviceIndex(devices, "Marshall");
            }

            if (headsetIndex >= 0 && marshallIndex >= 0 && headsetIndex < marshallIndex)
            {
                AudioOutputDevice headsetDevice = devices[headsetIndex];
                devices[headsetIndex] = devices[marshallIndex];
                devices[marshallIndex] = headsetDevice;
            }
        }

        private int FindAudioDeviceIndex(List<AudioOutputDevice> devices, string namePart)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddAudioDeviceById(List<AudioOutputDevice> visibleDevices, List<AudioOutputDevice> devices, string deviceId)
        {
            if (visibleDevices.Count >= 2 || string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            foreach (AudioOutputDevice device in devices)
            {
                if (device.Id == deviceId && !ContainsAudioDevice(visibleDevices, device.Id))
                {
                    visibleDevices.Add(device);
                    break;
                }
            }
        }

        private bool ContainsAudioDevice(List<AudioOutputDevice> devices, string deviceId)
        {
            foreach (AudioOutputDevice device in devices)
            {
                if (device.Id == deviceId)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearAudioOutputRadioButtons()
        {
            BindAudioOutputRadioButton(radioButtonAudio1, null);
            BindAudioOutputRadioButton(radioButtonAudio2, null);
        }

        private void BindAudioOutputRadioButton(RadioButton radioButton, AudioOutputDevice device)
        {
            radioButton.Tag = device;
            radioButton.Checked = false;
            radioButton.Visible = device != null;
            radioButton.Text = device == null ? "设备" : GetAudioDeviceDisplayName(device.Name);
        }

        private RadioButton GetFirstAudioOutputRadioButton()
        {
            if (radioButtonAudio1.Visible)
            {
                return radioButtonAudio1;
            }

            if (radioButtonAudio2.Visible)
            {
                return radioButtonAudio2;
            }

            return null;
        }

        private string GetAudioDeviceDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 4)
            {
                return name;
            }

            return name.Substring(0, 4);
        }

        private void SelectAudioOutputDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            RadioButton[] radioButtons = new[] { radioButtonAudio1, radioButtonAudio2 };
            foreach (RadioButton radioButton in radioButtons)
            {
                AudioOutputDevice device = radioButton == null ? null : radioButton.Tag as AudioOutputDevice;
                if (device != null && device.Id == deviceId)
                {
                    radioButton.Checked = true;
                    return;
                }
            }
        }

        private AudioOutputDevice GetSelectedAudioOutputDevice()
        {
            RadioButton[] radioButtons = new[] { radioButtonAudio1, radioButtonAudio2 };
            foreach (RadioButton radioButton in radioButtons)
            {
                if (radioButton != null && radioButton.Checked)
                {
                    return radioButton.Tag as AudioOutputDevice;
                }
            }

            return null;
        }

        private AudioOutputDevice GetAudioDeviceForMenuValue(bool secondDeviceSelected)
        {
            RadioButton radioButton = secondDeviceSelected ? radioButtonAudio2 : radioButtonAudio1;
            return radioButton.Tag as AudioOutputDevice;
        }

        private void SaveSelectedAudioOutputDevice()
        {
            AudioOutputDevice device = GetSelectedAudioOutputDevice();
            if (device == null)
            {
                return;
            }

            Settings.Default.audioDeviceId = device.Id;
            Settings.Default.Save();
        }

        private void audioOutputRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings || isLoadingAudioOutputDevices)
            {
                return;
            }

            RadioButton radioButton = sender as RadioButton;
            if (radioButton == null || !radioButton.Checked)
            {
                return;
            }

            SaveSelectedAudioOutputDevice();
            SaveLastSelection();
        }

        private void SetSelectedAudioOutputDefault(bool showSuccessMessage)
        {
            AudioOutputDevice device = GetSelectedAudioOutputDevice();
            if (device == null)
            {
                MessageBox.Show("请先选择一个声音输出设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                AudioOutputDevice.SetDefault(device.Id);
                SaveSelectedAudioOutputDevice();
                if (showSuccessMessage)
                {
                    MessageBox.Show("已将默认声音输出切换为: " + device.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法设置默认声音输出: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetAudioOutputDefault(string deviceId, bool showSuccessMessage)
        {
            if (!SelectAudioOutputDeviceIfVisible(deviceId))
            {
                return;
            }

            SetSelectedAudioOutputDefault(showSuccessMessage);
        }

        private bool SelectAudioOutputDeviceIfVisible(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return false;
            }

            RadioButton[] radioButtons = new[] { radioButtonAudio1, radioButtonAudio2 };
            foreach (RadioButton radioButton in radioButtons)
            {
                AudioOutputDevice device = radioButton.Tag as AudioOutputDevice;
                if (device != null && device.Id == deviceId)
                {
                    radioButton.Checked = true;
                    return true;
                }
            }

            return false;
        }

        private void checkBoxStartOptimize_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            Settings.Default.startOneClickOptimize = checkBoxStartOptimize.Checked;
            Settings.Default.Save();
        }

        private void checkBoxCloseRestore_CheckedChanged(object sender, EventArgs e)
        {
            if (isLoadingSettings)
            {
                return;
            }

            Settings.Default.closeOneClickRestore = checkBoxCloseRestore.Checked;
            Settings.Default.Save();
        }

        private void ApplyLastSelection()
        {
            bool restoreWinKeyDisabled = Settings.Default.winKeyDisabled;
            bool restoreInputMethodEnglish = Settings.Default.inputMethodEnglish;
            bool restoreAltShiftDisabled = Settings.Default.altShiftDisabled;
            bool restoreAudioSwitchEnabled = Settings.Default.optimizeAudioOutput;
            string restoreAudioDeviceId = Settings.Default.audioDeviceId;

            isLoadingSettings = true;
            try
            {
                radioButton2.Checked = restoreWinKeyDisabled;
                radioButton1.Checked = !restoreWinKeyDisabled;
                radioButton_eng.Checked = restoreInputMethodEnglish;
                radioButton_chinese.Checked = !restoreInputMethodEnglish;
                radioButtonaltshiftOff.Checked = restoreAltShiftDisabled;
                radioButtonaltshiftOn.Checked = !restoreAltShiftDisabled;
                SelectAudioOutputDevice(restoreAudioDeviceId);
            }
            finally
            {
                isLoadingSettings = false;
            }

            if (restoreWinKeyDisabled)
            {
                WindowsKey.Disable();
            }
            else
            {
                WindowsKey.Enable();
            }

            if (restoreInputMethodEnglish)
            {
                InputMethod.ChangeToEnglish();
            }
            else
            {
                InputMethod.ChangeToChinese();
            }

            if (restoreAltShiftDisabled)
            {
                AltShift.Disable();
            }
            else
            {
                AltShift.Enable();
            }

            if (restoreAudioSwitchEnabled)
            {
                SetSelectedAudioOutputDefault(false);
            }

            SaveLastSelection();
        }

        private void SaveLastSelection()
        {
            if (isLoadingSettings)
            {
                return;
            }

            Settings.Default.winKeyDisabled = radioButton2.Checked;
            Settings.Default.inputMethodEnglish = radioButton_eng.Checked;
            Settings.Default.altShiftDisabled = radioButtonaltshiftOff.Checked;
            Settings.Default.audioSwitchEnabled = Settings.Default.optimizeAudioOutput;
            SaveSelectedAudioOutputDevice();
            Settings.Default.Save();
        }

        private void HandleHotKey()
        {
            // 确保窗口在前台
            if (!this.IsActive)
            {
                this.Activate();
                // 给窗口一点时间来激活
                Thread.Sleep(50);
            }

            switch (clickCount % 3)
            {
                case 0:
                    button1_Click(this, EventArgs.Empty);
                    break;
                case 1:
                    button2_Click(this, EventArgs.Empty);
                    break;
                case 2:
                    button1_Click(this, EventArgs.Empty);
                    break;
            }
            clickCount++;
        }

        // 添加一个IsActive属性
        private bool IsActive
        {
            get
            {
                return Form.ActiveForm == this;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SaveShortcut(textBoxShortcut.Text);
        }
    }
}
