using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace PCLX
{
    internal sealed class CustomThemeConfig
    {
        public string ImagePath = "";
        public int Blur = 0;
        public int Brightness = 100;
        public int TintAlpha = 60;
        public int ColorCount = 2;
        public string Color1 = "#3A6EA5";
        public string Color2 = "#39C5BB";
        public string Color3 = "#F2C14E";
        public bool Gradient = true;
        public int Texture = 0;
        public int TextureOpacity = 50;
    }

    internal static class CustomTheme
    {
        private const int ThemeId = 15;

        private static string CfgPath
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PCL");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "CustomTheme.ini");
            }
        }

        public static CustomThemeConfig Load()
        {
            var c = new CustomThemeConfig();
            try
            {
                if (!File.Exists(CfgPath)) return c;
                foreach (var line in File.ReadAllLines(CfgPath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim();
                    string v = line.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "ImagePath": c.ImagePath = v; break;
                        case "Blur": c.Blur = Int(v, c.Blur); break;
                        case "Brightness": c.Brightness = Int(v, c.Brightness); break;
                        case "TintAlpha": c.TintAlpha = Int(v, c.TintAlpha); break;
                        case "ColorCount": c.ColorCount = Int(v, c.ColorCount); break;
                        case "Color1": c.Color1 = v; break;
                        case "Color2": c.Color2 = v; break;
                        case "Color3": c.Color3 = v; break;
                        case "Gradient": c.Gradient = v == "1"; break;
                        case "Texture": c.Texture = Int(v, c.Texture); break;
                        case "TextureOpacity": c.TextureOpacity = Int(v, c.TextureOpacity); break;
                    }
                }
            }
            catch (Exception ex) { Hooks.TryLog("读取自制主题失败：" + ex.Message); }
            return c;
        }

        private static int Int(string s, int def)
        {
            int r;
            return int.TryParse(s, out r) ? r : def;
        }

        public static void Save(CustomThemeConfig c)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("ImagePath=" + c.ImagePath);
                sb.AppendLine("Blur=" + c.Blur);
                sb.AppendLine("Brightness=" + c.Brightness);
                sb.AppendLine("TintAlpha=" + c.TintAlpha);
                sb.AppendLine("ColorCount=" + c.ColorCount);
                sb.AppendLine("Color1=" + c.Color1);
                sb.AppendLine("Color2=" + c.Color2);
                sb.AppendLine("Color3=" + c.Color3);
                sb.AppendLine("Gradient=" + (c.Gradient ? "1" : "0"));
                sb.AppendLine("Texture=" + c.Texture);
                sb.AppendLine("TextureOpacity=" + c.TextureOpacity);
                File.WriteAllText(CfgPath, sb.ToString(), new UTF8Encoding(true));
            }
            catch (Exception ex) { Hooks.TryLog("保存自制主题失败：" + ex.Message); }
        }

        public static List<Color> Palette(CustomThemeConfig c)
        {
            var list = new List<Color> { Parse(c.Color1), Parse(c.Color2) };
            if (c.ColorCount >= 3) list.Add(Parse(c.Color3));
            return list;
        }

        private static Color Parse(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return Color.FromRgb(0x3A, 0x6E, 0xA5); }
        }

        public static Color Mix(CustomThemeConfig c)
        {
            var list = Palette(c);
            double a = 0, r = 0, g = 0, b = 0;
            foreach (var col in list)
            {
                a += col.A; r += col.R; g += col.G; b += col.B;
            }
            int n = Math.Max(1, list.Count);
            return Color.FromArgb((byte)(a / n), (byte)(r / n), (byte)(g / n), (byte)(b / n));
        }

        public static void ToHsl(Color col, out double h, out double s, out double l)
        {
            double r = col.R / 255.0, g = col.G / 255.0, b = col.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2;
            double d = max - min;
            if (d < 1e-6) { h = 210; s = 0; return; }
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
            s *= 100;
            l *= 100;
        }

        public static readonly int[] TextureIds = { 5, 6, 7, 8, 9, 10, 11, 12, 13, 42 };

        public static string TextureName(int id)
        {
            switch (id)
            {
                case 5: return "玄素黑 · 波浪";
                case 6: return "铁杆粉 · 星点";
                case 7: return "神秘紫 · 枫叶";
                case 8: return "秋仪金 · 落叶";
                case 9: return "活跃橙 · 圆环";
                case 10: return "跳票红 · 笑脸";
                case 11: return "极客蓝 · 标记";
                case 12: return "滑稽彩 · 二进制";
                case 13: return "欧皇彩 · SSR";
                case 42: return "彩蛋 · 星雪";
                default: return "无";
            }
        }

        public static BitmapSource LoadPack(string rel)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(PCL.ModBase.PathImage + rel, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex) { Hooks.TryLog("加载纹理失败：" + ex.Message); return null; }
        }

        public static void ApplyTexture(CustomThemeConfig c)
        {
            try
            {
                var frm = PCL.ModMain.FrmMain;
                if (frm == null) return;
                var pi = FindProp(frm.GetType(), "ImgTitle");
                if (pi == null) return;
                var img = pi.GetValue(frm) as System.Windows.UIElement;
                if (img == null) return;

                var srcProp = FindProp(img.GetType(), "Source");
                if (srcProp != null)
                {
                    object val = c.Texture > 0 ? (object)(PCL.ModBase.PathImage + "Themes/" + c.Texture + ".png") : null;
                    srcProp.SetValue(img, val);
                }
                img.Opacity = Math.Max(0, Math.Min(100, c.TextureOpacity)) / 100.0;
            }
            catch (Exception ex) { Hooks.TryLog("应用纹理失败：" + ex.Message); }
        }

        // ---------------- 标题栏绘制 ----------------

        private static Grid _titleHost;
        private static readonly object HookLock = new object();


        private static System.Reflection.PropertyInfo FindProp(Type t, string name)
        {
            var list = new List<System.Reflection.PropertyInfo>();
            foreach (var pr in t.GetProperties(System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic))
            {
                if (pr.Name == name && pr.GetIndexParameters().Length == 0) list.Add(pr);
            }
            if (list.Count == 0) return null;
            list.Sort((a, b) => Depth(b.DeclaringType).CompareTo(Depth(a.DeclaringType)));
            return list[0];
        }

        private static int Depth(Type t)
        {
            int n = 0;
            while (t != null) { n++; t = t.BaseType; }
            return n;
        }

        public static void AttachTitleBar()
        {
            try
            {
                var frm = PCL.ModMain.FrmMain;
                if (frm == null) return;
                var pi = FindProp(frm.GetType(), "PanTitle");
                if (pi == null) return;
                var title = pi.GetValue(frm) as Grid;
                if (title == null) return;

                lock (HookLock)
                {
                    if (!ReferenceEquals(_titleHost, title))
                    {
                        _titleHost = title;
                        title.SizeChanged += (s, e) => { if (CurrentThemeIsCustom()) RenderTitleBar(Load()); };
                    }
                }
                RenderTitleBar(Load());
            }
            catch (Exception ex) { Hooks.TryLog("标题栏附加失败：" + ex.Message); }
        }

        private static bool CurrentThemeIsCustom()
        {
            try { return PCL.Settings.Get<int>("UiLauncherTheme") == ThemeId; }
            catch { return false; }
        }

        public static void RenderTitleBar(CustomThemeConfig c)
        {
            try
            {
                var title = _titleHost;
                if (title == null) return;
                double w = title.ActualWidth, h = title.ActualHeight;
                if (w < 2 || h < 2) return;

                Brush brush = BuildBrush(c, w, h);
                title.Background = brush;
                ApplyTexture(c);
            }
            catch (Exception ex) { Hooks.TryLog("标题栏绘制失败：" + ex.Message); }
        }

        private static Brush BuildBrush(CustomThemeConfig c, double w, double h)
        {
            var colors = Palette(c);
            Brush tint;
            if (c.Gradient && colors.Count >= 2)
            {
                var lg = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                for (int i = 0; i < colors.Count; i++)
                {
                    var col = Color.FromArgb((byte)Math.Max(0, Math.Min(255, c.TintAlpha)), colors[i].R, colors[i].G, colors[i].B);
                    lg.GradientStops.Add(new GradientStop(col, colors.Count == 1 ? 0 : (double)i / (colors.Count - 1)));
                }
                tint = lg;
            }
            else
            {
                var m = Mix(c);
                tint = new SolidColorBrush(Color.FromArgb((byte)Math.Max(0, Math.Min(255, c.TintAlpha)), m.R, m.G, m.B));
            }

            BitmapSource img = LoadImage(c.ImagePath);
            if (img == null) return tint;

            var grid = new Grid { Width = w, Height = h, ClipToBounds = true, Background = Brushes.Black };
            var image = new Image
            {
                Source = img,
                Stretch = Stretch.UniformToFill,
                Width = w,
                Height = h
            };
            if (c.Blur > 0)
                image.Effect = new BlurEffect { Radius = c.Blur, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Performance };
            grid.Children.Add(image);
            grid.Children.Add(new Rectangle { Width = w, Height = h, Fill = tint });

            int b = c.Brightness;
            if (b > 100)
                grid.Children.Add(new Rectangle { Width = w, Height = h, Fill = Brushes.White, Opacity = Math.Min(0.85, (b - 100) / 100.0) });
            else if (b < 100)
                grid.Children.Add(new Rectangle { Width = w, Height = h, Fill = Brushes.Black, Opacity = Math.Min(0.85, (100 - b) / 100.0) });

            grid.Measure(new Size(w, h));
            grid.Arrange(new Rect(0, 0, w, h));
            grid.UpdateLayout();

            var rtb = new RenderTargetBitmap((int)Math.Ceiling(w), (int)Math.Ceiling(h), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(grid);
            rtb.Freeze();
            return new ImageBrush(rtb) { Stretch = Stretch.Fill };
        }

        private static BitmapSource LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex) { Hooks.TryLog("加载背景图失败：" + ex.Message); return null; }
        }

        // ---------------- 设置窗口 ----------------

        public static void OpenEditor(FrameworkElement owner)
        {
            var c = Load();
            var win = new Window
            {
                Title = "自制主题",
                Width = 520,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = Brushes.White,
                FontFamily = new FontFamily("Microsoft YaHei UI")
            };
            try
            {
                var o = owner != null ? Window.GetWindow(owner) : null;
                if (o != null) win.Owner = o;
            }
            catch { }

            var root = new StackPanel { Margin = new Thickness(18) };

            // 背景图
            var rowImg = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var txtImg = new TextBox { Text = c.ImagePath, IsReadOnly = true, Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
            var btnPick = new Button { Content = "选择图片…", Width = 96, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
            var btnClear = new Button { Content = "清除", Width = 60, Height = 28, Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(btnPick, Dock.Right);
            DockPanel.SetDock(btnClear, Dock.Right);
            rowImg.Children.Add(btnPick);
            rowImg.Children.Add(btnClear);
            rowImg.Children.Add(txtImg);
            root.Children.Add(Label("标题栏背景图（留空则使用纯色/渐变）"));
            root.Children.Add(rowImg);

            // 模糊 / 亮度 / 透明度
            var sliderBlur = Slider(0, 40, c.Blur);
            var sliderBright = Slider(40, 160, c.Brightness);
            var sliderAlpha = Slider(0, 255, c.TintAlpha);
            root.Children.Add(SliderRow("高斯模糊半径", sliderBlur, c.Blur.ToString()));
            root.Children.Add(SliderRow("亮度 (%)", sliderBright, c.Brightness.ToString()));
            root.Children.Add(SliderRow("着色不透明度", sliderAlpha, c.TintAlpha.ToString()));

            // 颜色模式
            var combo = new ComboBox { Height = 28, Margin = new Thickness(0, 6, 0, 6) };
            combo.Items.Add("双色混合（各 1/2）");
            combo.Items.Add("三色混合（各 1/3）");
            combo.SelectedIndex = c.ColorCount >= 3 ? 1 : 0;
            root.Children.Add(Label("颜色模式"));
            root.Children.Add(combo);

            var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            var b1 = ColorButton(c.Color1);
            var b2 = ColorButton(c.Color2);
            var b3 = ColorButton(c.Color3);
            colorRow.Children.Add(b1);
            colorRow.Children.Add(b2);
            colorRow.Children.Add(b3);
            root.Children.Add(Label("颜色（点击选择）"));
            root.Children.Add(colorRow);

            root.Children.Add(Label("标题栏纹理（复用原版主题遮罩）"));
            var texCombo = new ComboBox { Height = 28, Margin = new Thickness(0, 2, 0, 4) };
            texCombo.Items.Add(new ComboBoxItem { Content = "无", Tag = 0 });
            foreach (var id in TextureIds)
                texCombo.Items.Add(new ComboBoxItem { Content = TextureName(id), Tag = id });
            texCombo.SelectedIndex = 0;
            for (int i = 0; i < texCombo.Items.Count; i++)
            {
                var it = texCombo.Items[i] as ComboBoxItem;
                if (it != null && (int)it.Tag == c.Texture) texCombo.SelectedIndex = i;
            }
            root.Children.Add(texCombo);

            var previewBorder = new Border
            {
                Height = 26,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Mix(c)),
                Margin = new Thickness(0, 2, 0, 4),
                ClipToBounds = true
            };
            var previewImg = new Image { Stretch = Stretch.UniformToFill };
            previewBorder.Child = previewImg;
            root.Children.Add(previewBorder);

            var texOpacity = Slider(0, 100, c.TextureOpacity);
            root.Children.Add(SliderRow("纹理不透明度", texOpacity, c.TextureOpacity.ToString()));

            Action updatePreview = () =>
            {
                var it = texCombo.SelectedItem as ComboBoxItem;
                int id = it == null ? 0 : (int)it.Tag;
                previewImg.Source = id > 0 ? LoadPack("Themes/" + id + ".png") : null;
                previewImg.Opacity = Math.Max(0, Math.Min(100, texOpacity.Value)) / 100.0;
            };
            texCombo.SelectionChanged += (s2, e2) => updatePreview();
            texOpacity.ValueChanged += (s2, e2) => { previewImg.Opacity = Math.Max(0, Math.Min(100, texOpacity.Value)) / 100.0; };
            updatePreview();

            var chkGradient = new CheckBox { Content = "颜色之间使用渐变过渡", IsChecked = c.Gradient, Margin = new Thickness(0, 4, 0, 0) };
            root.Children.Add(chkGradient);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var ok = new Button { Content = "确定", Width = 90, Height = 30, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "取消", Width = 90, Height = 30, IsCancel = true };
            bar.Children.Add(ok);
            bar.Children.Add(cancel);
            root.Children.Add(bar);

            btnPick.Click += (s, e) =>
            {
                var dlg = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.webp|所有文件|*.*",
                    Title = "选择标题栏背景图"
                };
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) txtImg.Text = dlg.FileName;
            };
            btnClear.Click += (s, e) => txtImg.Text = "";

            ok.Click += (s, e) =>
            {
                c.ImagePath = txtImg.Text.Trim();
                c.Blur = (int)sliderBlur.Value;
                c.Brightness = (int)sliderBright.Value;
                c.TintAlpha = (int)sliderAlpha.Value;
                c.ColorCount = combo.SelectedIndex == 1 ? 3 : 2;
                c.Color1 = b1.Tag as string ?? c.Color1;
                c.Color2 = b2.Tag as string ?? c.Color2;
                c.Color3 = b3.Tag as string ?? c.Color3;
                c.Gradient = chkGradient.IsChecked == true;
                var selTex = texCombo.SelectedItem as ComboBoxItem;
                c.Texture = selTex == null ? 0 : (int)selTex.Tag;
                c.TextureOpacity = (int)texOpacity.Value;
                Save(c);

                try { PCL.Settings.Set("UiLauncherTheme", ThemeId); } catch { }
                Hooks.ThemeApply(ThemeId);
                win.Close();
            };
            cancel.Click += (s, e) => win.Close();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            win.ShowDialog();
        }

        private static TextBlock Label(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 8, 0, 2), FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) };
        }

        private static Slider Slider(double min, double max, double val)
        {
            return new Slider { Minimum = min, Maximum = max, Value = val, Height = 26, VerticalAlignment = VerticalAlignment.Center };
        }

        private static UIElement SliderRow(string label, Slider slider, string val)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            var t = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5 };
            var v = new TextBlock { Text = val, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 12.5 };
            slider.ValueChanged += (s, e) => v.Text = ((int)slider.Value).ToString(CultureInfo.InvariantCulture);
            Grid.SetColumn(slider, 1);
            Grid.SetColumn(v, 2);
            g.Children.Add(t);
            g.Children.Add(slider);
            g.Children.Add(v);
            return g;
        }

        private static Button ColorButton(string hex)
        {
            var b = new Button
            {
                Width = 64,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Parse(hex)),
                Tag = hex,
                ToolTip = hex
            };
            b.Click += (s, e) =>
            {
                using (var dlg = new System.Windows.Forms.ColorDialog())
                {
                    dlg.FullOpen = true;
                    dlg.Color = System.Drawing.Color.FromArgb(Parse((string)b.Tag).R, Parse((string)b.Tag).G, Parse((string)b.Tag).B);
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string hx = "#" + dlg.Color.R.ToString("X2") + dlg.Color.G.ToString("X2") + dlg.Color.B.ToString("X2");
                        b.Tag = hx;
                        b.Background = new SolidColorBrush(Parse(hx));
                        b.ToolTip = hx;
                    }
                }
            };
            return b;
        }
    }
}
