import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/dist/说明.md'
s = io.open(p, encoding='utf-8').read()
s = s.replace('个性化工改页', '设置 → 个性化页')
s = s.replace('| 6 | 合规信息', '| 7 | 合规信息')
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
