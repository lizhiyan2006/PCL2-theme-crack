import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/CustomTheme.cs'
s = io.open(p, encoding='utf-8').read()

# helper to load pack resources (used by preview + apply)
s = s.replace('''        public static void ApplyTexture(CustomThemeConfig c)''','''        public static BitmapSource LoadPack(string rel)
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

        public static void ApplyTexture(CustomThemeConfig c)''')

# editor: insert texture block before gradient checkbox
old = '''            var chkGradient = new CheckBox { Content = "颜色之间使用渐变过渡", IsChecked = c.Gradient, Margin = new Thickness(0, 4, 0, 0) };
            root.Children.Add(chkGradient);'''

new = '''            root.Children.Add(Label("标题栏纹理（复用原版主题遮罩）"));
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
            root.Children.Add(chkGradient);'''
assert old in s
s = s.replace(old, new)

# save texture settings on OK
s = s.replace('''                c.Gradient = chkGradient.IsChecked == true;
                Save(c);''','''                c.Gradient = chkGradient.IsChecked == true;
                var selTex = texCombo.SelectedItem as ComboBoxItem;
                c.Texture = selTex == null ? 0 : (int)selTex.Tag;
                c.TextureOpacity = (int)texOpacity.Value;
                Save(c);''')

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
