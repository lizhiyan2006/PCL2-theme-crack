import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/dist/说明.md'
s = io.open(p, encoding='utf-8').read()

marker = '| 2 | 社区资源'
new_row = ('| 2 | 新增「自制主题」：标题栏使用自选背景图 + 可调高斯模糊/亮度/着色透明度；'
           '支持双色（1/2）或三色（1/3）混合，可开启渐变过渡，颜色可自选 | '
           '个性化工改页新增「自制主题」单选项与「设置…」按钮；标题栏用 `RenderTargetBitmap` 把'
           '「模糊后的背景图 + 着色层 + 亮度层」合成为画刷，窗口尺寸变化时重建；'
           '配置保存在 `PCL\\CustomTheme.ini` |\n')
assert marker in s
s = s.replace(marker, new_row + marker, 1)

s = s.replace('| 2 | 社区资源（CurseForge', '| 3 | 社区资源（CurseForge')
s = s.replace('| 3 | 移除启动器更新', '| 4 | 移除启动器更新')
s = s.replace('| 4 | 移除公告', '| 5 | 移除公告')
s = s.replace('| 5 | 移除反馈 / 新功能投票', '| 6 | 移除反馈 / 新功能投票')

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
