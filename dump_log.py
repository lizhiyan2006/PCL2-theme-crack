import io, sys, os

def dump(path, out=None, n=40):
    raw = io.open(path, 'rb').read()
    for enc in ('gbk', 'utf-8'):
        try:
            t = raw.decode(enc)
            break
        except Exception:
            t = raw.decode('latin1')
    lines = t.splitlines()[-n:]
    text = "\n".join(lines)
    if out:
        io.open(out, 'w', encoding='utf-8', newline='\n').write(text)
    else:
        sys.stdout.buffer.write(text.encode('utf-8'))

base = r'C:/Users/lizhiyan394/Documents/deepseek-harness/default-workspace/pcl-work/test'
dump(os.path.join(base, 'PCL', 'Log1.txt'), os.path.join(base, 'log_tail.txt'), 60)
try:
    dump(os.path.join(base, 'PCLX.log'), os.path.join(base, 'pclx_tail.txt'), 60)
except Exception:
    pass
print("done")
