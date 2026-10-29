import io, os

base = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/smoke'
os.makedirs(base + '/PCL', exist_ok=True)
img = base + '/bg.png'
lines = [
    'ImagePath=' + img,
    'Blur=14',
    'Brightness=105',
    'TintAlpha=70',
    'ColorCount=3',
    'Color1=#B8862F',
    'Color2=#E8C56A',
    'Color3=#7A4E12',
    'Gradient=1',
    'Texture=8',
    'TextureOpacity=65',
]
io.open(base + '/PCL/CustomTheme.ini', 'w', encoding='utf-8', newline='\r\n').write('\n'.join(lines) + '\n')
print(io.open(base + '/PCL/CustomTheme.ini', encoding='utf-8').read())
