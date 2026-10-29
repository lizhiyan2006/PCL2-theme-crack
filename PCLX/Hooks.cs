using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PCLX
{
    /// <summary>
    /// Runtime hooks injected into the patched launcher.
    /// </summary>
    public static class Hooks
    {
        private static bool _inited;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;
            try
            {
                EventManager.RegisterClassHandler(
                    typeof(FrameworkElement),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnElementLoaded),
                    true);

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        new Action(ApplyWindowChrome), DispatcherPriority.Loaded);
                }
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PCLX_SELFTEST")))
                {
                    StartSelfTest();
                }
            }
            catch (Exception ex)
            {
                TryLog("Init 失败：" + ex);
            }
        }


        private static void StartSelfTest()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            int step = 0;
            if (Environment.GetEnvironmentVariable("PCLX_SELFTEST") == "editor")
            {
                timer.Tick += (s0, e0) =>
                {
                    timer.Stop();
                    try { CustomTheme.OpenEditor(PCL.ModMain.FrmMain); }
                    catch (Exception ex) { TryLog("编辑器打开失败：" + ex); }
                };
                timer.Start();
                return;
            }
            timer.Tick += (s, e) =>
            {
                step++;
                try
                {
                    switch (step)
                    {
                        case 1: Nav(2); break;
                        case 2: Nav(3); break;
                        case 3: NavOther(1); break;
                        case 4: timer.Stop(); TryLog("自检结束"); break;
                        default:
                            timer.Stop();
                            TryLog("自检结束");
                            break;
                    }
                }
                catch (Exception ex) { TryLog("自检步骤 " + step + " 失败：" + ex.Message); }
            };
            timer.Start();
        }

        private static void Nav(int sub)
        {
            var f = PCL.ModMain.FrmMain;
            TryLog("导航 Setup/" + sub);
            f.PageChange(new PCL.FormMain.PageStackData
            {
                Page = PCL.FormMain.PageType.Setup,
                Additional = (PCL.FormMain.PageSubType)sub
            }, (PCL.FormMain.PageSubType)sub);
        }

        private static void NavOther(int sub)
        {
            var f = PCL.ModMain.FrmMain;
            TryLog("导航 Other/" + sub);
            f.PageChange(new PCL.FormMain.PageStackData
            {
                Page = PCL.FormMain.PageType.Other,
                Additional = sub
            }, (PCL.FormMain.PageSubType)sub);
        }

        private static void ApplyWindowChrome()
        {
            try
            {
                var w = Application.Current != null ? Application.Current.MainWindow : null;
                if (w != null)
                {
                    w.Title = "Plain Craft Launcher (PCL) 个人定制版";
                    TryLog("窗口标题已设置");
                }
            }
            catch (Exception ex) { TryLog("ApplyWindowChrome 失败：" + ex); }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;
            string tn = fe.GetType().FullName;
            if (tn != "PCL.PageSetupUI" && tn != "PCL.PageSetupSystem" &&
                tn != "PCL.PageOtherAbout" && tn != "PCL.PageOtherLeft") return;

            fe.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    switch (tn)
                    {
                        case "PCL.PageSetupUI": ApplyThemeUnlock(fe); break;
                        case "PCL.PageSetupSystem": ApplySetupCleanup(fe); break;
                        case "PCL.PageOtherAbout": ApplyAboutCleanup(fe); break;
                        case "PCL.PageOtherLeft": ApplyOtherLeftCleanup(fe); break;
                    }
                }
                catch (Exception ex) { TryLog("页面处理失败 " + tn + "：" + ex); }
            }), DispatcherPriority.ContextIdle);
        }

        // ---------------- 主题解锁 ----------------


        private static void AddCustomThemeEntry(FrameworkElement page)
        {
            var grid = FindDeep(page, "PanLauncherTheme") as Grid;
            if (grid == null) return;
            if (FindDeep(grid, "RadioLauncherCustom") != null) return;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var rb = new PCL.MyRadioBox
            {
                Text = "自制主题",
                Name = "RadioLauncherCustom",
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(rb, 3);
            Grid.SetColumn(rb, 1);
            rb.Check += (s, e) =>
            {
                try { PCL.Settings.Set("UiLauncherTheme", 15); } catch (Exception ex) { TryLog("切到自制主题失败：" + ex.Message); }
            };
            grid.Children.Add(rb);

            var btn = new Button
            {
                Content = "设置…",
                Width = 62,
                Height = 26,
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(btn, 3);
            Grid.SetColumn(btn, 2);
            btn.Click += (s, e) => CustomTheme.OpenEditor(page);
            grid.Children.Add(btn);

            try
            {
                if (PCL.Settings.Get<int>("UiLauncherTheme") == 15) rb.Checked = true;
            }
            catch { }
        }

        private static void ApplyThemeUnlock(FrameworkElement page)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                DumpTree(page, 0, sb);
                TryLog("页面结构：" + sb.ToString());
                TryLog("FindName(CardLauncher)=" + (page.FindName("CardLauncher") != null) +
                       " deep(CardLauncher)=" + (FindDeep(page, "CardLauncher") != null) +
                       " deep(PanLauncherTheme)=" + (FindDeep(page, "PanLauncherTheme") != null) +
                       " deep(PanLauncherHide)=" + (FindDeep(page, "PanLauncherHide") != null));
            }
            catch (Exception ex) { TryLog("dump 失败：" + ex.Message); }

            int found = 0, enabled = 0;
            for (int i = 0; i <= 14; i++)
            {
                var rb = FindDeep(page, "RadioLauncherTheme" + i);
                if (rb == null) { TryLog("  theme" + i + " 未找到"); continue; }
                found++;
                rb.IsEnabled = true;
                rb.Opacity = 1.0;
                rb.ToolTip = null;
                if (rb.IsEnabled) enabled++;
            }
            var probe = FindDeep(page, "RadioLauncherTheme6");
            TryLog($"主题解锁：found={found} enabled={enabled} probe6={(probe == null ? "null" : probe.IsEnabled + "/" + probe.Visibility + "/" + probe.Opacity)}");
            Collapse(page, "RadioLauncherTheme5Gray");
            Collapse(page, "LabLauncherTheme5Unlock");
            Collapse(page, "LabLauncherTheme8Copy");
            Collapse(page, "LabLauncherTheme9Copy");
            Collapse(page, "LabLauncherTheme11Click");
            Collapse(page, "PanLauncherHide");
            try { AddCustomThemeEntry(page); } catch (Exception ex) { TryLog("自制主题入口失败：" + ex.Message); }
            TryLog("主题解锁已应用");
        }


        private static void DumpTree(DependencyObject root, int depth, System.Text.StringBuilder sb)
        {
            if (depth > 14) return;
            var fe = root as FrameworkElement;
            if (fe != null)
                sb.Append(fe.Name == "" ? fe.GetType().Name : fe.Name).Append(" ");
            int n = 0;
            try { n = VisualTreeHelper.GetChildrenCount(root); } catch { return; }
            for (int i = 0; i < n; i++)
                DumpTree(VisualTreeHelper.GetChild(root, i), depth + 1, sb);
        }


        // ---------------- 主题颜色 ----------------

        public static void ThemeApply(int newTheme)
        {
            try
            {
                var app = Application.Current;
                if (app != null && !app.Dispatcher.CheckAccess())
                {
                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try { ApplyThemeCore(newTheme); }
                        catch (Exception ex) { TryLog("ThemeApply(ui) 失败：" + ex); }
                    }), DispatcherPriority.Normal);
                    return;
                }
                ApplyThemeCore(newTheme);
            }
            catch (Exception ex) { TryLog("ThemeApply 失败：" + ex); }
        }

        private static void ApplyThemeCore(int newTheme)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var t = typeof(PCL.Settings).Assembly.GetType("PCL.ModSecret", true);
            if (t == null) return;
            Func<string, object> Get = n =>
            {
                var f = t.GetField(n, F);
                return f == null ? null : f.GetValue(null);
            };
            Action<string, object> Set = (n, v) =>
            {
                var f = t.GetField(n, F);
                if (f != null) f.SetValue(null, v);
            };

            int theme = newTheme;
            if (theme < 0) theme = PCL.Settings.Get<int>("UiLauncherTheme");
            var check = t.GetMethod("ThemeCheckOne", F);
            if (check == null || !(bool)check.Invoke(null, new object[] { theme })) theme = 0;

            int hue = 210, sat = 85, light = 0;
            if (theme == 15)
            {
                var cfg0 = CustomTheme.Load();
                var mix0 = CustomTheme.Mix(cfg0);
                double hh, ss, ll;
                CustomTheme.ToHsl(mix0, out hh, out ss, out ll);
                hue = (int)Math.Round(hh);
                sat = (int)Math.Round(Math.Max(0, Math.Min(100, ss)));
                light = 0;
                goto ThemeColorsReady;
            }
            switch (theme)
            {
                case 0: hue = 210; sat = 85; break;
                case 1: hue = 168; sat = 80; break;
                case 2: hue = 120; sat = 72; break;
                case 3: hue = 45; sat = 90; break;
                case 4: hue = 28; sat = 55; break;
                case 5: hue = 210; sat = 0; break;
                case 6: hue = 330; sat = 75; break;
                case 7: hue = 270; sat = 60; break;
                case 8: hue = 42; sat = 62; break;
                case 9: hue = 22; sat = 85; break;
                case 10: hue = 0; sat = 80; break;
                case 11: hue = 200; sat = 95; break;
                case 12: hue = (int)((DateTime.Now.TimeOfDay.TotalMilliseconds / 6000.0 * 360) % 360); sat = 90; break;
                case 13: hue = (int)((DateTime.Now.TimeOfDay.TotalMilliseconds / 9000.0 * 360) % 360); sat = 70 + Rnd(30); break;
                case 14:
                    hue = PCL.Settings.Get<int>("UiLauncherHue");
                    sat = PCL.Settings.Get<int>("UiLauncherSat");
                    light = PCL.Settings.Get<int>("UiLauncherLight") - 20;
                    break;
            }

        ThemeColorsReady:
            int dontClick = Get("ThemeDontClick") == null ? 0 : Convert.ToInt32(Get("ThemeDontClick"));
            if (dontClick == 1) light = 999;
            else if (dontClick == 2) { hue = Rnd(360); sat = 40 + Rnd(30); light = -20 + Rnd(40); }

            Set("ColorHue", hue);
            Set("ColorSat", sat);
            Set("ColorLightAdjust", light);
            Set("ThemeNow", theme);

            var c = new PCL.ModBase.MyColor();
            var c1 = new PCL.ModBase.MyColor().FromHSL2(hue, sat * 0.2, 25 + light * 0.3);
            var c2 = c.FromHSL2(hue, sat, 45 + light);
            var c3 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 55 + light);
            var c4 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 65 + light);
            var c5 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 80 + light * 0.4);
            var c6 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 91 + light * 0.1);
            var c7 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 95);
            var c8 = new PCL.ModBase.MyColor().FromHSL2(hue, sat, 97);
            var gray4 = (PCL.ModBase.MyColor)Get("ColorGray4");
            var bg0 = c4 * 0.4 + c5 * 0.4 + gray4 * 0.2;
            var bg1 = new PCL.ModBase.MyColor(190, c7);
            var semi = new PCL.ModBase.MyColor(1, c8);

            Set("Color1", c1); Set("Color2", c2); Set("Color3", c3); Set("Color4", c4);
            Set("Color5", c5); Set("Color6", c6); Set("Color7", c7); Set("Color8", c8);
            Set("ColorBg0", bg0); Set("ColorBg1", bg1); Set("ColorSemiTransparent", semi);

            var res = Application.Current.Resources;
            res["ColorBrush1"] = (System.Windows.Media.Brush)c1;
            res["ColorBrush2"] = (System.Windows.Media.Brush)c2;
            res["ColorBrush3"] = (System.Windows.Media.Brush)c3;
            res["ColorBrush4"] = (System.Windows.Media.Brush)c4;
            res["ColorBrush5"] = (System.Windows.Media.Brush)c5;
            res["ColorBrush6"] = (System.Windows.Media.Brush)c6;
            res["ColorBrush7"] = (System.Windows.Media.Brush)c7;
            res["ColorBrush8"] = (System.Windows.Media.Brush)c8;
            res["ColorBrushBg0"] = (System.Windows.Media.Brush)bg0;
            res["ColorBrushBg1"] = (System.Windows.Media.Brush)bg1;
            res["ColorObject1"] = (System.Windows.Media.Color)c1;
            res["ColorObject2"] = (System.Windows.Media.Color)c2;
            res["ColorObject3"] = (System.Windows.Media.Color)c3;
            res["ColorObject4"] = (System.Windows.Media.Color)c4;
            res["ColorObject5"] = (System.Windows.Media.Color)c5;
            res["ColorObject6"] = (System.Windows.Media.Color)c6;
            res["ColorObject7"] = (System.Windows.Media.Color)c7;
            res["ColorObject8"] = (System.Windows.Media.Color)c8;
            res["ColorObjectBg0"] = (System.Windows.Media.Color)bg0;
            res["ColorObjectBg1"] = (System.Windows.Media.Color)bg1;

            var main = t.GetMethod("ThemeRefreshMain", F);
            if (main != null) main.Invoke(null, null);

            if (theme == 15)
            {
                var app2 = Application.Current;
                if (app2 != null)
                    app2.Dispatcher.BeginInvoke(new Action(() => CustomTheme.AttachTitleBar()), DispatcherPriority.Loaded);
                else
                    CustomTheme.AttachTitleBar();
            }
            TryLog("主题已切换：" + theme + "（H=" + hue + ", S=" + sat + ", L=" + light + "）");
        }

        private static int Rnd(int max)
        {
            return (int)(DateTime.Now.Ticks % (max <= 0 ? 1 : max));
        }

        // ---------------- 设置页：移除更新/公告，增加下载服务器 ----------------

        private static void ApplySetupCleanup(FrameworkElement page)
        {
            Collapse(page, "BtnSystemUpdate");
            Collapse(page, "ComboSystemUpdate");
            Collapse(page, "ComboSystemActivity");
            CollapseText(page, "启动器更新");
            CollapseText(page, "启动器公告");

            SetupModSource(page);
            TryLog("设置页清理已应用");
        }

        private static void SetupModSource(FrameworkElement page)
        {
            var combo = FindDeep(page, "ComboDownloadMod") as ComboBox;
            if (combo == null) return;
            if (combo.Items.Count == 4 && combo.Tag is string && (string)combo.Tag == "pclx") { TryLog("自定义下载源项已存在"); return; }
            if (combo.Items.Count != 3) return;

            var item = new ComboBoxItem { Content = "自定义服务器（Mirror 兼容格式）" };
            combo.Items.Add(item);
            combo.Tag = "pclx";
            combo.ToolTip = "社区资源（CurseForge / Modrinth）的下载服务器。\n选择『自定义服务器』后可在弹窗中填写镜像地址。";

            bool userAction = false;
            combo.PreviewMouseDown += (s, e) => { userAction = true; };
            combo.DropDownOpened += (s, e) => { userAction = true; };

            TryLog("自定义下载源项已添加，当前项数=" + combo.Items.Count);
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedIndex != 3) return;
                if (!userAction) return;
                userAction = false;
                string cur = ReadCustomBase();
                string input = InputDialog.Show(page, "自定义下载服务器",
                    "请输入镜像服务器根地址（不含结尾斜杠）。\n" +
                    "该镜像需要兼容 MCIM 路径格式：\n" +
                    "  {base}/curseforge/...  {base}/modrinth/...  {base}/{file}\n" +
                    "留空则使用默认镜像。", cur);
                if (input == null)
                {
                    combo.SelectedIndex = 2;
                    return;
                }
                input = input.Trim().TrimEnd('/');
                if (input.Length > 0 && !input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("地址必须以 http:// 或 https:// 开头。", "无效的地址");
                    combo.SelectedIndex = 2;
                    return;
                }
                WriteCustomBase(input);
            };

            if (combo.SelectedIndex == 3)
                combo.ToolTip = "当前自定义服务器：" + (ReadCustomBase().Length == 0 ? "（未设置，使用默认镜像）" : ReadCustomBase());
        }

        // ---------------- 关于页：移除更新按钮，追加合规说明 ----------------

        private static void ApplyAboutCleanup(FrameworkElement page)
        {
            Collapse(page, "BtnAboutUpdate");

            var panel = FindDeep(page, "PanMain") as Panel;
            if (panel == null) return;
            if (panel.Tag is string && (string)panel.Tag == "pclx") return;
            panel.Tag = "pclx";

            var border = new Border
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(21, 16, 21, 16),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA))
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "Plain Craft Launcher (PCL) 个人定制版",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x14, 0x3C, 0x6E))
            });
            sp.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Text =
                    "本软件是第三方基于 PCL 独立进行的二次创作，与原作者无关。\n" +
                    "原始项目：Plain Craft Launcher 2（作者：龙腾猫跃，https://meloong.com/afd/a/LTCat）\n" +
                    "本版本基于 PCL 2.14.0.0 修改，遵循《PCL 分发有限许可》与《PCL 存储库合理使用指南》，仅供个人使用，禁止分发。\n\n" +
                    "本版本修改内容：\n" +
                    "· 解锁原需赞助的个性化主题；\n" +
                    "· 社区资源（CurseForge / Modrinth）可自定义下载服务器；\n" +
                    "· 移除启动器更新、公告、反馈与新功能投票功能。\n\n" +
                    "原始许可：https://shimo.im/docs/rGrd8pY8xWkt6ryW"
            });
            border.Child = sp;
            panel.Children.Add(border);
            TryLog("关于页合规卡片已添加");
        }

        // ---------------- 帮助页左侧：移除反馈与投票 ----------------

        private static void ApplyOtherLeftCleanup(FrameworkElement page)
        {
            Collapse(page, "ItemFeedback");
            Collapse(page, "ItemVote");
            TryLog("帮助页反馈/投票已隐藏");
        }

        // ---------------- 下载源 ----------------

        public static string DlSourceModGet(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            try
            {
                int mode = PCL.Settings.Get<int>("ToolDownloadMod");
                if (mode == 3)
                {
                    string b = ReadCustomBase();
                    if (b.Length > 0) return ReplaceHosts(url, b);
                }
            }
            catch { }
            return DefaultReplace(url);
        }

        private static string DefaultReplace(string url)
        {
            return url.Replace("api.modrinth.com", "mod.mcimirror.top/modrinth")
                      .Replace("staging-api.modrinth.com", "mod.mcimirror.top/modrinth")
                      .Replace("cdn.modrinth.com", "mod.mcimirror.top")
                      .Replace("api.curseforge.com", "mod.mcimirror.top/curseforge")
                      .Replace("edge.forgecdn.net", "mod.mcimirror.top")
                      .Replace("mediafilez.forgecdn.net", "mod.mcimirror.top")
                      .Replace("media.forgecdn.net", "mod.mcimirror.top");
        }

        private static string ReplaceHosts(string url, string b)
        {
            return url.Replace("https://api.modrinth.com", b + "/modrinth")
                      .Replace("https://staging-api.modrinth.com", b + "/modrinth")
                      .Replace("https://cdn.modrinth.com", b)
                      .Replace("https://api.curseforge.com", b + "/curseforge")
                      .Replace("https://edge.forgecdn.net", b)
                      .Replace("https://mediafilez.forgecdn.net", b)
                      .Replace("https://media.forgecdn.net", b);
        }

        // ---------------- 配置文件 ----------------

        private static string _cfgPath;
        private static string _cachedBase;
        private static DateTime _cachedTime = DateTime.MinValue;

        private static string CfgPath
        {
            get
            {
                if (_cfgPath != null) return _cfgPath;
                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PCL");
                    Directory.CreateDirectory(dir);
                    _cfgPath = Path.Combine(dir, "ModSource.ini");
                }
                catch
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCLX");
                    Directory.CreateDirectory(dir);
                    _cfgPath = Path.Combine(dir, "ModSource.ini");
                }
                return _cfgPath;
            }
        }

        private static string ReadCustomBase()
        {
            try
            {
                string p = CfgPath;
                if (!File.Exists(p)) return "";
                var t = File.GetLastWriteTimeUtc(p);
                if (t == _cachedTime && _cachedBase != null) return _cachedBase;
                string line = File.ReadAllLines(p, Encoding.UTF8)
                    .FirstOrDefault(l => l.StartsWith("CustomBase=", StringComparison.OrdinalIgnoreCase));
                _cachedBase = line == null ? "" : line.Substring("CustomBase=".Length).Trim();
                _cachedTime = t;
                return _cachedBase;
            }
            catch { return ""; }
        }

        private static void WriteCustomBase(string value)
        {
            try
            {
                File.WriteAllText(CfgPath, "CustomBase=" + value + Environment.NewLine, new UTF8Encoding(true));
                _cachedTime = File.GetLastWriteTimeUtc(CfgPath);
                _cachedBase = value;
            }
            catch (Exception ex) { TryLog("写入下载源配置失败：" + ex); }
        }

        // ---------------- 工具 ----------------

        private static void Collapse(FrameworkElement page, string name)
        {
            var el = FindDeep(page, name);
            if (el != null) el.Visibility = Visibility.Collapsed;
            else TryLog("  [collapse] 未找到 " + name);
        }

        internal static FrameworkElement FindDeep(DependencyObject root, string name)
        {
            if (root == null) return null;
            var fe = root as FrameworkElement;
            if (fe != null && fe.Name == name) return fe;
            int n = 0;
            try { n = VisualTreeHelper.GetChildrenCount(root); } catch { n = 0; }
            for (int i = 0; i < n; i++)
            {
                var r = FindDeep(VisualTreeHelper.GetChild(root, i), name);
                if (r != null) return r;
            }
            foreach (var ch in LogicalTreeHelper.GetChildren(root))
            {
                var dep = ch as DependencyObject;
                if (dep != null)
                {
                    var r = FindDeep(dep, name);
                    if (r != null) return r;
                }
            }
            return null;
        }

        private static void CollapseText(DependencyObject root, string text)
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var tb = child as TextBlock;
                if (tb != null && tb.Text == text)
                {
                    tb.Visibility = Visibility.Collapsed;
                    continue;
                }
                CollapseText(child, text);
            }
        }

        internal static void TryLog(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PCLX.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    internal static class InputDialog
    {
        public static string Show(FrameworkElement owner, string title, string hint, string initial)
        {
            var win = new Window
            {
                Title = title,
                Width = 560,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };
            try
            {
                var o = owner != null ? Window.GetWindow(owner) : null;
                if (o != null) win.Owner = o;
            }
            catch { }

            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(new TextBlock
            {
                Text = hint,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 12.5
            });
            var tb = new TextBox { Text = initial ?? "", Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
            root.Children.Add(tb);
            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };
            var ok = new Button { Content = "确定", Width = 90, Height = 30, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "取消", Width = 90, Height = 30, IsCancel = true };
            string result = null;
            ok.Click += (s, e) => { result = tb.Text; win.Close(); };
            cancel.Click += (s, e) => { win.Close(); };
            btns.Children.Add(ok);
            btns.Children.Add(cancel);
            root.Children.Add(btns);
            win.Content = root;
            win.Loaded += (s, e) => { tb.Focus(); tb.SelectAll(); };
            win.ShowDialog();
            return result;
        }
    }
}
