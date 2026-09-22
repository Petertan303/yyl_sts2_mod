# -*- coding: utf-8 -*-
"""压缩卡面: 小图(res://yyl_sts2_mod/images/card_portraits/*.png)缩到宽500,
大图(big/)保留原尺寸; 两者 PNG 深度压缩(量化/优化择小)。
原始图先整体备份到仓库根 _card_art_backup_20260922/。
用法: C:/ProgramData/anaconda3/python.exe compress_card_portraits.py [--dry-run]
"""
import os, sys, io, shutil
from PIL import Image

REPO = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(REPO, 'yyl_sts2_mod', 'yyl_sts2_mod', 'images', 'card_portraits')
BK = os.path.join(REPO, '_card_art_backup_20260922')
SMALL_W = 500

def png_bytes(im, optimize_only=False):
    out = io.BytesIO()
    im.save(out, 'PNG', optimize=True)
    best = out.getvalue()
    if not optimize_only:
        try:
            q = im.quantize(colors=256, method=Image.FASTOCTREE)
            out2 = io.BytesIO()
            q.save(out2, 'PNG', optimize=True)
            if len(out2.getvalue()) < len(best):
                best = out2.getvalue()
        except Exception:
            pass
    return best

def process(path, resize_w):
    im = Image.open(path)
    if im.mode not in ('RGB', 'RGBA'):
        im = im.convert('RGBA')
    w, h = im.size
    if resize_w and w > resize_w:
        im = im.resize((resize_w, max(1, round(h * resize_w / w))), Image.LANCZOS)
    return im, png_bytes(im)

def main():
    dry = '--dry-run' in sys.argv
    groups = [('小图', ART), ('大图', os.path.join(ART, 'big'))]
    # 0) 备份(仅第一次)
    if not dry and not os.path.exists(BK):
        for label, src in groups:
            dst = os.path.join(BK, os.path.basename(src))
            shutil.copytree(src, dst)
        print('已备份原图 ->', BK)
    total_old = total_new = 0
    for label, src in groups:
        resize_w = SMALL_W if label == '小图' else 0
        names = sorted(f for f in os.listdir(src) if f.lower().endswith('.png'))
        old = sum(os.path.getsize(os.path.join(src, f)) for f in names)
        new = 0
        for f in names:
            fp = os.path.join(src, f)
            orig_size = os.path.getsize(fp)
            im, blob = process(fp, resize_w)
            new += len(blob)
            if not dry:
                open(fp, 'wb').write(blob)
            print('  %s %-28s %dx%d %s %6.1fKB -> %6.1fKB' % (
                label, f, im.size[0], im.size[1], im.mode,
                orig_size / 1024, len(blob) / 1024))
        total_old += old; total_new += new
        print('%s: %d 张, %.1fMB -> %.1fMB\n' % (label, len(names), old / 1e6, new / 1e6))
    print('合计: %.1fMB -> %.1fMB (%.0f%%)' % (
        total_old / 1e6, total_new / 1e6, 100 * total_new / max(total_old, 1)))

if __name__ == '__main__':
    main()
