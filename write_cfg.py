import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/smoke/PCL/CustomTheme.ini'
img = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/smoke/bg.png'
lines = [
    'ImagePath=' + img,
    'Blur=18',
    'Brightness=110',
    'TintAlpha=80',
    'ColorCount=2',
    'Color1=#3A6EA5',
    'Color2=#39C5BB',
    'Color3=#F2C14E',
    'Gradient=1',
]
io.open(p, 'w', encoding='utf-8', newline='\r\n').write('\n'.join(lines) + '\n')
print(io.open(p, encoding='utf-8').read())
