import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/CustomTheme.cs'
s = io.open(p, encoding='utf-8').read()

# config fields
s = s.replace('''        public bool Gradient = true;''','''        public bool Gradient = true;
        public int Texture = 0;
        public int TextureOpacity = 50;''')

# load
s = s.replace('''                        case "Gradient": c.Gradient = v == "1"; break;''','''                        case "Gradient": c.Gradient = v == "1"; break;
                        case "Texture": c.Texture = Int(v, c.Texture); break;
                        case "TextureOpacity": c.TextureOpacity = Int(v, c.TextureOpacity); break;''')

# save
s = s.replace('''                sb.AppendLine("Gradient=" + (c.Gradient ? "1" : "0"));''','''                sb.AppendLine("Gradient=" + (c.Gradient ? "1" : "0"));
                sb.AppendLine("Texture=" + c.Texture);
                sb.AppendLine("TextureOpacity=" + c.TextureOpacity);''')

# texture names + apply
s = s.replace('''        // ---------------- 标题栏绘制 ----------------''','''        public static readonly int[] TextureIds = { 5, 6, 7, 8, 9, 10, 11, 12, 13, 42 };

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

        public static void ApplyTexture(CustomThemeConfig c)
        {
            try
            {
                var frm = PCL.ModMain.FrmMain;
                if (frm == null) return;
                var pi = frm.GetType().GetProperty("ImgTitle",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (pi == null) return;
                var img = pi.GetValue(frm) as System.Windows.UIElement;
                if (img == null) return;

                var srcProp = img.GetType().GetProperty("Source");
                if (c.Texture > 0)
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(PCL.ModBase.PathImage + "Themes/" + c.Texture + ".png", UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    if (srcProp != null) srcProp.SetValue(img, bmp);
                }
                else
                {
                    if (srcProp != null) srcProp.SetValue(img, null);
                }
                img.Opacity = Math.Max(0, Math.Min(100, c.TextureOpacity)) / 100.0;
            }
            catch (Exception ex) { Hooks.TryLog("应用纹理失败：" + ex.Message); }
        }

        // ---------------- 标题栏绘制 ----------------''')

# call ApplyTexture in RenderTitleBar (after background set)
s = s.replace('''                Brush brush = BuildBrush(c, w, h);
                title.Background = brush;''','''                Brush brush = BuildBrush(c, w, h);
                title.Background = brush;
                ApplyTexture(c);''')

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
