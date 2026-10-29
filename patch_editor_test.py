import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/Hooks.cs'
s = io.open(p, encoding='utf-8').read()

old = '''            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            int step = 0;'''
new = '''            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
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
            }'''
assert old in s
s = s.replace(old, new)
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
