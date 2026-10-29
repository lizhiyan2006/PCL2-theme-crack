using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PCLSetup
{
    internal static class Program
    {
        internal const string AppName = "Plain Craft Launcher (PCL) 个人定制版";
        internal const string ShortName = "PCL 个人定制版";
        internal const string Version = "1.0.0 (based on PCL 2.14.0.0)";
        internal const string Publisher = "第三方修改，原作者：龙腾猫跃";
        internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\PCLCustomEdition";
        internal static string TargetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\PCL-Custom");

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                foreach (var a in args)
                {
                    if (a.StartsWith("/D=", StringComparison.OrdinalIgnoreCase) ||
                        a.StartsWith("-D=", StringComparison.OrdinalIgnoreCase))
                        TargetDir = a.Substring(3).Trim('"');
                }
                Log("start args=" + string.Join(" ", args) + " target=" + TargetDir);
                if (HasArg(args, "/uninstall") || HasArg(args, "-uninstall"))
                {
                    Uninstall();
                    return 0;
                }
                if (HasArg(args, "/S") || HasArg(args, "-silent"))
                {
                    Install(null);
                    return 0;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var f = new SetupForm())
                {
                    Application.Run(f);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR " + ex);
                if (!(HasArg(args, "/S") || HasArg(args, "-silent")))
                    MessageBox.Show("安装程序出错：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static void Log(string msg)
        {
            try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pclsetup.log"), DateTime.Now.ToString("HH:mm:ss.fff ") + msg + Environment.NewLine); } catch { }
        }

        private static bool HasArg(string[] args, string a)
        {
            foreach (var s in args)
                if (string.Equals(s, a, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static void Install(IProgress progress)
        {
            Directory.CreateDirectory(TargetDir);
            Step(progress, 10, "正在释放主程序…");
            Extract("app.exe", Path.Combine(TargetDir, "Plain Craft Launcher 2.exe"));
            Step(progress, 55, "正在释放运行组件…");
            Extract("PCLX.dll", Path.Combine(TargetDir, "PCLX.dll"));

            string selfCopy = Path.Combine(TargetDir, "Uninstall.exe");
            try { File.Copy(Application.ExecutablePath, selfCopy, true); } catch { }

            Step(progress, 75, "正在创建快捷方式…");
            string exe = Path.Combine(TargetDir, "Plain Craft Launcher 2.exe");
            CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortName + ".lnk"), exe);
            try
            {
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortName);
                Directory.CreateDirectory(startMenu);
                CreateShortcut(Path.Combine(startMenu, ShortName + ".lnk"), exe);
                CreateShortcut(Path.Combine(startMenu, "卸载 " + ShortName + ".lnk"), selfCopy, "/uninstall");
            }
            catch (Exception ex) { Log("开始菜单创建失败：" + ex.Message); }

            Step(progress, 90, "正在写入卸载信息…");
            WriteUninstallEntry(exe);

            Step(progress, 100, "安装完成");
            Log("install ok -> " + TargetDir);
        }

        private static void Step(IProgress p, int v, string text)
        {
            if (p != null) p.Report(v, text);
        }

        private static void Extract(string resourceName, string dest)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (s == null) throw new Exception("安装包损坏，缺少资源：" + resourceName);
                using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                    s.CopyTo(fs);
            }
        }

        private static void CreateShortcut(string lnkPath, string target, string arguments = "")
        {
            try
            {
                var t = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(t);
                dynamic sc = shell.CreateShortcut(lnkPath);
                sc.TargetPath = target;
                sc.Arguments = arguments;
                sc.WorkingDirectory = Path.GetDirectoryName(target);
                sc.IconLocation = target + ",0";
                sc.Description = AppName;
                sc.Save();
            }
            catch { }
        }

        private static void WriteUninstallEntry(string exe)
        {
            try { using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                if (k == null) return;
                k.SetValue("DisplayName", AppName);
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", Publisher);
                k.SetValue("InstallLocation", TargetDir);
                k.SetValue("DisplayIcon", exe);
                string uninst = Path.Combine(TargetDir, "Uninstall.exe");
                k.SetValue("UninstallString", "\"" + uninst + "\" /uninstall");
                k.SetValue("QuietUninstallString", "\"" + uninst + "\" /uninstall /S");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            } } catch (Exception ex) { Log("卸载信息写入失败：" + ex.Message); }
        }

        internal static void Uninstall()
        {
            if (MessageBox.Show("确定要卸载 " + AppName + " 吗？\n\n注意：PCL 的设置保存在注册表中，游戏与启动器数据不会被删除。",
                    "卸载确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }

            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortName + ".lnk");
            try { if (File.Exists(desktop)) File.Delete(desktop); } catch { }
            try
            {
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortName);
                if (Directory.Exists(startMenu)) Directory.Delete(startMenu, true);
            }
            catch { }

            // 通过临时批处理在自身退出后删除安装目录
            string bat = Path.Combine(Path.GetTempPath(), "pcl_uninstall_" + Guid.NewGuid().ToString("N") + ".bat");
            File.WriteAllText(bat,
                "@echo off\r\n" +
                "ping -n 3 127.0.0.1 >nul\r\n" +
                "rd /s /q \"" + TargetDir + "\"\r\n" +
                "del /f /q \"" + bat + "\"\r\n");
            Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"") { WindowStyle = ProcessWindowStyle.Hidden });
        }
    }

    internal interface IProgress
    {
        void Report(int percent, string text);
    }

    internal sealed class SetupForm : Form, IProgress
    {
        private readonly Label _status;
        private readonly ProgressBar _bar;
        private readonly Button _install;
        private readonly Button _cancel;
        private readonly CheckBox _runAfter;

        public SetupForm()
        {
            Text = "安装 " + Program.AppName;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 260);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F);

            var title = new Label
            {
                Text = Program.AppName,
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 60, 110),
                AutoSize = true,
                Location = new Point(24, 22)
            };
            Controls.Add(title);

            Controls.Add(new Label
            {
                Text = "版本：" + Program.Version + "\n安装位置：" + Program.TargetDir +
                       "\n\n本软件为第三方基于 PCL 的修改版，遵循《PCL 分发有限许可》，仅供个人使用。",
                AutoSize = false,
                Size = new Size(470, 80),
                Location = new Point(26, 60),
                ForeColor = Color.FromArgb(60, 60, 60)
            });

            _bar = new ProgressBar { Location = new Point(26, 150), Size = new Size(468, 16), Minimum = 0, Maximum = 100 };
            Controls.Add(_bar);

            _status = new Label { Location = new Point(26, 172), Size = new Size(468, 22), Text = "准备就绪" };
            Controls.Add(_status);

            _runAfter = new CheckBox { Text = "安装完成后立即运行", Location = new Point(26, 200), AutoSize = true, Checked = true };
            Controls.Add(_runAfter);

            _install = new Button { Text = "安装", Size = new Size(100, 32), Location = new Point(286, 214) };
            _cancel = new Button { Text = "取消", Size = new Size(100, 32), Location = new Point(394, 214) };
            _install.Click += (s, e) =>
            {
                _install.Enabled = false;
                _cancel.Enabled = false;
                try
                {
                    Program.Install(this);
                    _install.Text = "完成";
                    _install.Enabled = true;
                    _install.Click += (s2, e2) => { Close(); };
                    if (_runAfter.Checked)
                        Process.Start(Path.Combine(Program.TargetDir, "Plain Craft Launcher 2.exe"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("安装失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _install.Enabled = true;
                    _cancel.Enabled = true;
                }
            };
            _cancel.Click += (s, e) => Close();
            Controls.Add(_install);
            Controls.Add(_cancel);

            // 安装后写入 Uninstall.exe 副本，供卸载入口使用
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { };
        }

        public void Report(int percent, string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, string>(Report), percent, text);
                return;
            }
            _bar.Value = Math.Max(0, Math.Min(100, percent));
            _status.Text = text;
            Application.DoEvents();
        }
    }
}
