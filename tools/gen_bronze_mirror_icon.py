# -*- coding: utf-8 -*-
"""生成遗物「铜镜」的三张图标 + Godot .import 文件。

像素画做法：先在 32x32 的网格上用有限色板逐格绘制（硬边、无抗锯齿），
再 NEAREST 放大到小图 94x94 / 大图 256x256；轮廓图 = 小图 alpha 的纯白剪影。
与 tools/gen_gilded_mirror_icon.py 的资源规格保持一致。
"""
from PIL import Image
import hashlib
import math
import os
import re

ROOT = r"C:/Users/Peter_Tan/RiderProjects/yyl_sts2_mod/yyl_sts2_mod/yyl_sts2_mod/images/relics"
RES = "res://yyl_sts2_mod/images/relics/"

# 有限色板：暗描边 / 青铜框 / 铜锈 / 镜面
OUTLINE = (24, 30, 26, 255)
BRONZE_DARK = (62, 74, 58, 255)
BRONZE = (108, 122, 84, 255)
BRONZE_LIGHT = (155, 166, 112, 255)
RIM = (48, 62, 52, 255)
PATINA_DARK = (52, 96, 84, 255)
PATINA = (78, 132, 112, 255)
PATINA_LIGHT = (120, 172, 142, 255)
GLASS = (156, 196, 166, 255)
GLINT = (222, 238, 208, 255)
SHADOW = (36, 66, 60, 255)

N = 32
CX = CY = 15.5

# 镜面上的铜锈斑（手工摆位，保证每次生成一致）
PATINA_SPOTS = [
    (10, 11), (11, 11), (10, 12), (21, 9), (22, 10), (21, 10),
    (12, 21), (13, 22), (12, 22), (22, 20), (21, 21), (20, 22),
    (16, 24), (17, 24), (8, 17), (9, 18),
]
# 左上反光弧
GLINT_PIXELS = [(10, 8), (9, 9), (8, 11), (8, 13), (9, 15), (11, 16), (13, 17)]


def build_grid():
    grid = [[None] * N for _ in range(N)]

    def put(x, y, color):
        if 0 <= x < N and 0 <= y < N:
            grid[y][x] = color

    for y in range(N):
        for x in range(N):
            d = math.hypot(x - CX, y - CY)
            if d > 15.0:
                continue
            if d > 13.2:
                c = OUTLINE
            elif d > 12.0:
                c = BRONZE_DARK
            elif d > 10.6:
                c = BRONZE_LIGHT if (x + y) % 3 else BRONZE
            elif d > 9.6:
                c = RIM
            elif d > 8.6:
                c = BRONZE_DARK
            elif d > 6.2:
                c = PATINA_DARK
            elif d > 3.5:
                c = PATINA
            else:
                c = GLASS
            # 放射状刻纹：外环上每隔 30° 压一道暗铜
            if 12.0 < d <= 13.2:
                ang = math.atan2(y - CY, x - CX)
                if abs(math.sin(ang * 6)) < 0.22:
                    c = BRONZE_DARK
            put(x, y, c)

    # 顶部挂钮（画在圆环之后，免得被外环覆盖）
    for y in range(1, 5):
        for x in range(13, 19):
            put(x, y, OUTLINE)
    for y in range(2, 4):
        for x in range(14, 18):
            put(x, y, BRONZE_LIGHT)
    put(15, 2, RIM)
    put(16, 2, RIM)

    # 镜面铜锈与底部阴影
    for x, y in PATINA_SPOTS:
        if grid[y][x] in (PATINA, PATINA_DARK, GLASS):
            put(x, y, PATINA_LIGHT if (x + y) % 2 else PATINA_DARK)
    for x, y in [(12, 23), (13, 23), (14, 24), (18, 24), (19, 23), (20, 23)]:
        if grid[y][x] in (PATINA, PATINA_DARK):
            put(x, y, SHADOW)
    for x, y in GLINT_PIXELS:
        if grid[y][x] in (PATINA, PATINA_DARK, GLASS, PATINA_LIGHT):
            put(x, y, GLINT)
    # 镜心一点菱形高光
    for x, y in ((15, 14), (14, 15), (15, 15), (16, 15), (15, 16)):
        put(x, y, GLINT)
    return grid


def render(size):
    grid = build_grid()
    base = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    px = base.load()
    for y in range(N):
        for x in range(N):
            if grid[y][x]:
                px[x, y] = grid[y][x]
    return base.resize((size, size), Image.NEAREST)


def write_import(src_file, import_path, template_path, uid):
    """复制模板 .import，替换 uid / ctex 路径 / source_file / dest_files。"""
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


small = render(94)
big = render(256)
outline = Image.new("RGBA", (94, 94), (255, 255, 255, 0))
outline.putalpha(small.getchannel("A"))

small.save(os.path.join(ROOT, "bronze_mirror.png"))
big.save(os.path.join(ROOT, "big", "bronze_mirror.png"))
outline.save(os.path.join(ROOT, "outline", "bronze_mirror_outline.png"))
print("已写出 3 张图标:", small.size, big.size, outline.size, "bbox", small.getbbox())

write_import(RES + "bronze_mirror.png",
             os.path.join(ROOT, "bronze_mirror.png.import"),
             os.path.join(ROOT, "peach_can.png.import"),
             "brnz3m1rr0r7a2")
write_import(RES + "outline/bronze_mirror_outline.png",
             os.path.join(ROOT, "outline", "bronze_mirror_outline.png.import"),
             os.path.join(ROOT, "outline", "peach_can_outline.png.import"),
             "brnz0utl1n3k7b")
write_import(RES + "big/bronze_mirror.png",
             os.path.join(ROOT, "big", "bronze_mirror.png.import"),
             os.path.join(ROOT, "big", "peach_can.png.import"),
             "brnzb1g9m1rr0r")
