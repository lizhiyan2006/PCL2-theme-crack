import io

p = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/tools/PCLX/CustomTheme.cs'
s = io.open(p, encoding='utf-8').read()

helper = '''
        private static System.Reflection.PropertyInfo FindProp(Type t, string name)
        {
            var list = new List<System.Reflection.PropertyInfo>();
            foreach (var pr in t.GetProperties(System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic))
            {
                if (pr.Name == name && pr.GetIndexParameters().Length == 0) list.Add(pr);
            }
            if (list.Count == 0) return null;
            list.Sort((a, b) => Depth(b.DeclaringType).CompareTo(Depth(a.DeclaringType)));
            return list[0];
        }

        private static int Depth(Type t)
        {
            int n = 0;
            while (t != null) { n++; t = t.BaseType; }
            return n;
        }

'''
s = s.replace('''        public static void AttachTitleBar()''', helper + '''        public static void AttachTitleBar()''')

s = s.replace('''                var pi = frm.GetType().GetProperty("PanTitle",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);''','''                var pi = FindProp(frm.GetType(), "PanTitle");''')

s = s.replace('''                var pi = frm.GetType().GetProperty("ImgTitle",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);''','''                var pi = FindProp(frm.GetType(), "ImgTitle");''')

s = s.replace('''                var srcProp = img.GetType().GetProperty("Source");''','''                var srcProp = FindProp(img.GetType(), "Source");''')

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok')
