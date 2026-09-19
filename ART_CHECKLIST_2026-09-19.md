# yyl_sts2_mod 待绘制美术清单（2026-09-19）

> 共 **13 张卡面 + 2 个 Power 图标**。本轮新增卡 7+2 张，其余 6 张为存量缺口。
> 全部缺失时游戏会回退到通用占位图（`card.png` / `power.png`），不会报错，但所有缺失卡长得一样。

## 规格

| 类型 | 路径 | 建议尺寸 | 说明 |
|---|---|---|---|
| 卡面 | `yyl_sts2_mod/images/card_portraits/<文件名>.png` | **547 × 408**（与现有 palm_thunder.png 一致，约 4:3） | 大图目录 `big/` 放同一张图即可（现有 big 与小图同尺寸同内容） |
| Power 图 | `yyl_sts2_mod/images/powers/<文件名>.png` | **64 × 64** | 大图 `big/` 目录放 **256 × 256** 同内容 |

文件名必须与下表完全一致（小写 snake_case，**注意 golden_ward_card 这类带 Card 后缀的特例**，下表已按引擎实际读取的规则生成）。保存 PNG 后无需手动 import：下次发布构建时 Godot 无头导入会自动处理。

## 卡面（13 张）

| # | 中文卡名 | 类名 | 文件名 | 画面建议 |
|---|---|---|---|---|
| 1 | 破煞（新） | PoSha | `po_sha.png` | 金色雷光劈开一团黑煞之气，消耗牌的决绝感 |
| 2 | 阴煞（新） | YinSha | `yin_sha.png` | 阴冷黑雾缠绕数道人影，全员虚弱的压迫感 |
| 3 | 清心咒（新） | ClearMind | `clear_mind.png` | 一张澄澈符纸泛起淡金光晕，邪祟在外 |
| 4 | 涤荡（新） | Purge | `purge.png` | 清水/光流冲刷身上的污黑纹理 |
| 5 | 连雷（新） | ChainThunder | `chain_thunder.png` | 一道主雷分叉跳向侧面的次要目标 |
| 6 | 太极（新） | TaiChi | `tai_chi.png` | 阴阳鱼回旋，一方的攻被引向另一侧 |
| 7 | 诛邪（新） | ZhuXie | `zhu_xie.png` | 审判之剑贯落，低血量者同斩 |
| 8 | 辟邪剑法 | WardingBlade | `warding_blade.png` | 一柄古剑，剑身缠红色辟邪纹路 |
| 9 | 白长虫 | WhiteWorm | `white_worm.png` | 一条通体惨白的小虫，阴雷内伤感 |
| 10 | 龙虎山正一雷法 | DragonTigerRite | `dragon_tiger_rite.png` | 黄符引雷，龙虎山符箓风 |
| 11 | 天火 | SkyFire | `sky_fire.png` | 天穹裂开，倾泻而下的炽白天火 |
| 12 | 移穴 | AcupointShift | `acupoint_shift.png` | 人体穴位图上光点沿经络游走 |
| 13 | 风后奇门 | WindArray | `wind_array.png` | 八奇技罗盘阵图，风纹环绕 |

## Power 图标（2 个）

| # | 中文 | 类名 | 文件名（64×64 / big 256×256） | 画面建议 |
|---|---|---|---|---|
| 1 | 清心（新） | PurityVeil | `purity_veil.png` | 一圈淡金光罩护住心口 |
| 2 | 太极（新） | TaiChiMark | `tai_chi_mark.png` | 小型阴阳鱼标记 |

## 备注

- 上表"新"= 2026-09-19 新增：破煞 / 阴煞 / 清心咒 / 涤荡 / 连雷 / 太极 / 诛邪 + 清心 / 太极。
- 存量缺口的 6 张卡面（#8–#13）此前一直用占位图，可一并补上。
- 绘制完成后把文件放进对应目录，再跑一次发布构建（或游戏内启用 mod 的资源重导入）即可生效。
