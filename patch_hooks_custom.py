import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/Hooks.cs'
s = io.open(p, encoding='utf-8').read()

# 1) theme 15 palette
old = '''            int hue = 210, sat = 85, light = 0;
            switch (theme)
            {'''
new = '''            int hue = 210, sat = 85, light = 0;
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
            {'''
assert old in s
s = s.replace(old, new)

old2 = '''            int dontClick = Get("ThemeDontClick") == null ? 0 : Convert.ToInt32(Get("ThemeDontClick"));'''
new2 = '''        ThemeColorsReady:
            int dontClick = Get("ThemeDontClick") == null ? 0 : Convert.ToInt32(Get("ThemeDontClick"));'''
assert old2 in s
s = s.replace(old2, new2)

# 2) attach custom title bar after refresh
old3 = '''            var main = t.GetMethod("ThemeRefreshMain", F);
            if (main != null) main.Invoke(null, null);
            TryLog("主题已切换：" + theme + "（H=" + hue + ", S=" + sat + ", L=" + light + "）");'''
new3 = '''            var main = t.GetMethod("ThemeRefreshMain", F);
            if (main != null) main.Invoke(null, null);

            if (theme == 15)
            {
                var app2 = Application.Current;
                if (app2 != null)
                    app2.Dispatcher.BeginInvoke(new Action(() => CustomTheme.AttachTitleBar()), DispatcherPriority.Loaded);
                else
                    CustomTheme.AttachTitleBar();
            }
            TryLog("主题已切换：" + theme + "（H=" + hue + ", S=" + sat + ", L=" + light + "）");'''
assert old3 in s
s = s.replace(old3, new3)

# 3) add custom theme radio + settings button on the UI page
old4 = '''            Collapse(page, "PanLauncherHide");
            TryLog("主题解锁已应用");'''
new4 = '''            Collapse(page, "PanLauncherHide");
            try { AddCustomThemeEntry(page); } catch (Exception ex) { TryLog("自制主题入口失败：" + ex.Message); }
            TryLog("主题解锁已应用");'''
assert old4 in s
s = s.replace(old4, new4)

method = '''
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
'''
s = s.replace('''        private static void ApplyThemeUnlock(FrameworkElement page)''', method + '''
        private static void ApplyThemeUnlock(FrameworkElement page)''')

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
