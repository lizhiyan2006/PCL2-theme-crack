import io

# 1) rebuild doc text
p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/dist/说明.md'
s = io.open(p, encoding='utf-8').read()
s = s.replace(
    '支持双色（1/2）或三色（1/3）混合，可开启渐变过渡，颜色可自选',
    '支持双色（1/2）或三色（1/3）混合，可开启渐变过渡，颜色可自选；'
    '可叠加原版主题的遮罩纹理（玄素黑波浪、铁杆粉星点、神秘紫枫叶、秋仪金落叶、活跃橙圆环、跳票红笑脸、极客蓝标记、滑稽彩二进制、欧皇彩 SSR、彩蛋星雪），纹理不透明度可调')
s = s.replace('7f663aa1d843be49555d0415ec08b0c5d628279635431c02a869ded2c2fa78d1', 'HASH_PLACEHOLDER')
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('doc ok')
