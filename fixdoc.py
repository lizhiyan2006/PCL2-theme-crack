import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/dist/说明.md'
lines = io.open(p, encoding='utf-8').read().split('\n')
for i, l in enumerate(lines):
    if l.startswith('| 1 |'):
        lines[i] = ('| 1 | 解锁全部个性化主题并恢复换色（官方版把颜色计算裁掉了） | '
                    '`ModSecret.ThemeCheckOne` 恒为真、`CurrentRank` 提升；'
                    '`ThemeRefresh` 改为调用 `PCLX.Hooks.ThemeApply` 重新实现配色；'
                    '运行时启用全部主题单选框并移除「赞助解锁」遮罩 |')
io.open(p, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines))
print('ok')
