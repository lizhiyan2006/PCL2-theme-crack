import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/CustomTheme.cs'
s = io.open(p, encoding='utf-8').read()

old = '''                if (c.Texture > 0)
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
                }'''

new = '''                if (srcProp != null)
                {
                    object val = c.Texture > 0 ? (object)(PCL.ModBase.PathImage + "Themes/" + c.Texture + ".png") : null;
                    srcProp.SetValue(img, val);
                }'''
assert old in s
s = s.replace(old, new)
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
