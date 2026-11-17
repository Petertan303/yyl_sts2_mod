# -*- coding: utf-8 -*-
"""生成遗物「鎏金镜」的三张图标 + Godot .import 文件。
小图 94x94 / 大图 256x256 用同一套归一化坐标绘制（硬边像素风），
轮廓图为小图 alpha 的纯白剪影（与现有遗物 outline 规格一致）。
"""
from PIL import Image, ImageDraw
import math, hashlib, os, re

ROOT = r"C:/Users/Peter_Tan/RiderProjects/yyl_sts2_mod/yyl_sts2_mod/yyl_sts2_mod/images/relics"
RES = "res://yyl_sts2_mod/images/relics/"

OUTLINE = (36, 28, 20, 255)
FRAME_DARK = (122, 74, 24, 255)
FRAME_LIGHT = (201, 150, 47, 255)
RIM = (107, 63, 18, 255)
RAY = (184, 121, 31, 255)
KNOB = (58, 40, 22, 255)
GRAD = [
    (255, 247, 210, 255), (255, 236, 164, 255), (250, 214, 112, 255),
    (240, 188, 72, 255), (224, 160, 52, 255), (205, 134, 38, 255),
    (186, 112, 30, 255), (168, 96, 26, 255),
]
HL = (255, 252, 232, 255)
HL2 = (255, 240, 186, 255)


def make(size):
    s = size / 94.0
    def p(v):
        return int(round(v * s))

    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = p(47)

    # 外圈 12 道放射刻纹（呼应金光的放射状辉光）
    w = max(2, p(3))
    for k in range(12):
        a = 2 * math.pi * k / 12 - math.pi / 2
        r0, r1 = p(34), p(42)
        d.line([cx + math.cos(a) * r0, cy + math.sin(a) * r0,
                cx + math.cos(a) * r1, cy + math.sin(a) * r1],
               fill=RAY, width=w)

    # 镜顶挂钮
    d.rectangle([p(42), p(5), p(52), p(18)], fill=OUTLINE)
    d.rectangle([p(44), p(7), p(50), p(15)], fill=FRAME_LIGHT)
    d.rectangle([p(46), p(8), p(48), p(13)], fill=KNOB)

    # 铜框：深描边 → 暗铜 → 亮铜 → 内圈阴影
    d.ellipse([p(12), p(12), p(82), p(82)], fill=OUTLINE)
    d.ellipse([p(15), p(15), p(79), p(79)], fill=FRAME_DARK)
    d.ellipse([p(18), p(18), p(76), p(76)], fill=FRAME_LIGHT)
    d.ellipse([p(21), p(21), p(73), p(73)], fill=RIM)

    # 镜面：8 级金色色带（中心亮、边缘沉），保留像素环带感
    r = p(23)
    pix = img.load()
    for y in range(p(24), p(71) + 1):
        for x in range(p(24), p(71) + 1):
            dist = math.hypot(x - cx, y - cy)
            if dist > r:
                continue
            idx = min(len(GRAD) - 1, int(dist / r * len(GRAD)))
            pix[x, y] = GRAD[idx]

    # 镜面左上一段弧形反光（单弧，避免画成钟表指针）
    d.arc([p(27), p(27), p(67), p(67)], start=192, end=288,
          fill=HL2, width=max(2, p(3)))
    d.arc([p(30), p(30), p(64), p(64)], start=205, end=268,
          fill=HL, width=max(2, p(2)))

    # 中央光点（镜心聚光）
    d.polygon([(p(47), p(43)), (p(51), p(47)), (p(47), p(51)), (p(43), p(47))],
              fill=(255, 250, 222, 255))
    d.polygon([(p(47), p(45)), (p(49), p(47)), (p(47), p(49)), (p(45), p(47))],
              fill=(255, 255, 246, 255))
    return img


def write_import(src_file, import_path, template_path, uid):
    """复制模板 .import，替换 uid / ctex 路径 / source_file / dest_files。
    Godot 的 ctex 文件名 = basename + '-' + md5(source_file 资源路径) + '.ctex'。
    """
    h = hashlib.md5(src_file.encode()).hexdigest()
    base = os.path.basename(src_file)
    ctex = "res://.godot/imported/%s-%s.ctex" % (base, h)
    t = open(template_path, encoding="utf-8").read()
    t = re.sub(r"uid://[a-z0-9]+", "uid://" + uid, t)
    t = re.sub(r'path="res://\.godot/imported/[^"]+"', 'path="%s"' % ctex, t)
    t = re.sub(r'source_file="[^"]+"', 'source_file="%s"' % src_file, t)
    t = re.sub(r'dest_files=\["[^"]+"\]', 'dest_files=["%s"]' % ctex, t)
    with open(import_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(t)
    print("  import ->", import_path, "|", ctex)


small = make(94)
big = make(256)
outline = Image.new("RGBA", (94, 94), (255, 255, 255, 0))
outline.putalpha(small.getchannel("A"))

small.save(os.path.join(ROOT, "gilded_mirror.png"))
big.save(os.path.join(ROOT, "big", "gilded_mirror.png"))
outline.save(os.path.join(ROOT, "outline", "gilded_mirror_outline.png"))
print("已写出 3 张图标:", small.size, big.size, outline.size, "bbox", small.getbbox())

write_import(RES + "gilded_mirror.png",
             os.path.join(ROOT, "gilded_mirror.png.import"),
             os.path.join(ROOT, "peach_can.png.import"),
             "gmdm1rr0r7a2x")
write_import(RES + "outline/gilded_mirror_outline.png",
             os.path.join(ROOT, "outline", "gilded_mirror_outline.png.import"),
             os.path.join(ROOT, "outline", "peach_can_outline.png.import"),
             "gmoutl1n3k7bq")
write_import(RES + "big/gilded_mirror.png",
             os.path.join(ROOT, "big", "gilded_mirror.png.import"),
             os.path.join(ROOT, "big", "peach_can.png.import"),
             "gmbig2m1rr0r9")
