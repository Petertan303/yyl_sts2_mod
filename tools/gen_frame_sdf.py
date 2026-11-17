# -*- coding: utf-8 -*-
"""为每个角色动画帧生成金光特效用的 SDF 贴图 (golden_sdf_<帧名>.png)。

用法: 用带 scipy/Pillow 的 python 直接运行 (例: C:/ProgramData/anaconda3/python.exe)。
修改立绘 PNG 后重跑本脚本, 再 publish 即可。

公式 (由现有 images/ui/golden_sdf.png 反推, MAE 0.0011, 见 .workbuddy/memory/2026-11-04.md):
  源图 = 该帧立绘 alpha>0, 原尺寸居中放进 241x190 帧画布,
  再整体居中放进 385x334 SDF 画布 (四周各 72px),
  sd = edt(~canvas) - edt(canvas)          # 内部为负, 外部为正, 单位 px
  d  = clip(0.5 + sd/144, 0, 1)            # 0.5=轮廓, 外扩 n px -> 0.5+n/144
  写 8bit 灰度 PNG。

不同尺寸帧 (fallen.png 171x188) 先按自身中心对齐到 241x190 帧画布 —— 与
AnimatedSprite2D 默认 centered=true 的逐帧居中行为一致。

运行时由 Code/Powers/GoldenAegis.cs 的 SyncFrameSdf 按当前帧切换三层金光的 Texture。
增加新帧贴图时: 把文件名加进 FRAMES, 重跑脚本 (未生成对应 SDF 的帧会回退 idle)。
"""
from PIL import Image
import numpy as np
from scipy.ndimage import distance_transform_edt
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSET = os.path.join(ROOT, "yyl_sts2_mod", "yyl_sts2_mod")
UI = os.path.join(ASSET, "images", "ui")
CHARUI = os.path.join(ASSET, "images", "charui")

CANVAS_W, CANVAS_H = 385, 334
FRAME_W, FRAME_H = 241, 190
PAD_X, PAD_Y = (CANVAS_W - FRAME_W) // 2, (CANVAS_H - FRAME_H) // 2
DIST_SCALE = 144.0
THRESH = 0

# yylCharacter.tscn 的 SpriteFrames 里出现的全部立绘帧
FRAMES = ["yyl", "cast1", "cast2", "cast3", "fallen",
          "hurt", "hurt1", "hurt2", "hurt3", "hurt4"]


def make_sdf(name):
    im = Image.open(os.path.join(CHARUI, name + ".png")).convert("RGBA")
    a = np.asarray(im)[:, :, 3] > THRESH
    h, w = a.shape
    frame = np.zeros((FRAME_H, FRAME_W), dtype=bool)
    ox, oy = (FRAME_W - w) // 2, (FRAME_H - h) // 2
    frame[oy:oy + h, ox:ox + w] = a
    canvas = np.zeros((CANVAS_H, CANVAS_W), dtype=bool)
    canvas[PAD_Y:PAD_Y + FRAME_H, PAD_X:PAD_X + FRAME_W] = frame
    sd = distance_transform_edt(~canvas) - distance_transform_edt(canvas)
    d = np.clip(0.5 + sd / DIST_SCALE, 0.0, 1.0)
    return Image.fromarray((d * 255).astype(np.uint8), mode="L"), d


def main():
    ref_path = os.path.join(UI, "golden_sdf.png")
    if os.path.exists(ref_path):
        ref = np.asarray(Image.open(ref_path)).astype(np.float32) / 255.0
        _, idle = make_sdf("yyl")
        print("idle 帧与 golden_sdf.png 的 MAE: %.5f (应 < 0.002)" % np.abs(idle - ref).mean())
    for name in FRAMES:
        img, d = make_sdf(name)
        out = os.path.join(UI, "golden_sdf_" + name + ".png")
        img.save(out)
        ys, xs = np.where(d < 0.5)
        print("  %-22s 内部bbox x[%d..%d] y[%d..%d]" % (
            os.path.basename(out), xs.min(), xs.max(), ys.min(), ys.max()))


if __name__ == "__main__":
    main()
