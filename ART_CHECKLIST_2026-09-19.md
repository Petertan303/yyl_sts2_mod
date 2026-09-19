# yyl_sts2_mod 待绘制美术清单（更新于 2026-09-19 深夜）

> **Power 图标已全部补齐**（本轮 7 个新 Power + 上一批的清心 / 太极标记，均为 AI 生成的像素风圆形徽章，已放入 `images/powers/` 与 `big/`）。
> 下面只剩**卡面**：共 **43 张**待画（本轮新增 30 张 + 存量缺口 13 张）。

## 规格

| 类型 | 路径 | 尺寸 |
|---|---|---|
| 卡面 | `yyl_sts2_mod/images/card_portraits/<文件名>.png` | **547 × 408**（同现有 palm_thunder.png） |
| 大卡图 | `yyl_sts2_mod/images/card_portraits/big/<文件名>.png` | 放同一张图即可 |
| Power 图 | `yyl_sts2_mod/images/powers/<文件名>.png` + `big/` | 64 × 64 / 256 × 256 ✅ 已完成 |

缺失时游戏回退到通用占位图（`card.png` / `power.png`），不会报错。保存 PNG 后跑一次发布构建即生效（Godot 无头导入自动处理）。

## 本轮新增（30 张 + 状态牌，优先画）

| 中文名 | 文件名 | 画面建议 |
|---|---|---|
| 顶肘 | `elbow_strike.png` | 近身肘击，对方露出破绽 |
| 点穴 | — | 卡面仍需绘制（Power 图标已用观者 mod 的印记图标） |
| 侧踹 | `side_kick.png` | 侧身一脚，另一手架住 |
| 撩掌 | `rising_palm.png` | 由下向上的掌击，掌心泛金光 |
| 逗龙 | `tease_nailong.png` | 逗弄一只小龙，引开它的注意 |
| 崩拳 | `burst_fist.png` | 直拳轰出，气浪四散 |
| 破绽 | `opening.png` | 对手架势露出空隙，被标红圈 |
| 化劲 | `deflect_force.png` | 来袭的一击被引偏、卸到身侧 |
| 站桩 | `standing_post.png` | 马步站定，脚下生根 |
| 舒筋 | `relax_tendon.png` | 把身上的黑气搓成球丢向对方 |
| 封门 | `close_gate.png` | 双臂交叠封住门户，金光一闪 |
| 引光 | `yin_guang.png` | 牵引一缕金光入体 |
| 纳炁 | `intake_qi.png` | 张口吞纳青色气流 |
| 引炁 | `yin_qi.png` | 出拳瞬间，炁顺着拳势涌出 |
| 套步 | `shift_step.png` | 错步换位，甩掉一张废牌 |
| 抢步（暂时移出卡池，可暂缓绘制） | `quick_step.png` | 抢先进身的一小步 |
| 炁冲 | `qi_burst.png` | 炁凝于拳，一击爆发 |
| 借力 | `borrow_force.png` | 借对方虚弱之势反推回去 |
| 截脉 | `vein_cut.png` | 指尖截断对方气血走向 |
| 铁山靠 | `shoulder_strike.png` | 肩背撞入，同时护住自身 |
| 金光壁 | `golden_wall.png` | 一面金色光墙竖起 |
| 守势 | `guard_stance.png` | 收攻为守的架势：金光更盛、炁息收敛 |
| 拔罐 | `cupping.png` | 拔罐吸出黑气 |
| 卜卦 | `divination.png` | 掷卦查看牌堆顶 |
| 炁海 | `qi_sea.png` | 身周形成一片炁的海洋 |
| 五雷正法 | `five_thunder_law.png` | 五道雷同时落下 |
| 血雷 | `blood_thunder.png` | 血色雷霆，自身也带伤 |
| 燃炁 | `burn_qi.png` | 燃烧自身气血化为炁 |
| 行炁 | `move_qi.png` | 炁在经脉里流转成环 |
| 拉伤（状态牌） | `la_shang.png` | 拉伤的手臂／绷带，象征持续掉血 |

| 拾遗 | `shi_yi.png` | 从弃牌堆拾起一张牌，画面可画"手从 discard 捡牌"意象 |
| 炼化 | `lian_hua.png` | 手中牌化为金色光焰 |

## 存量缺口（13 张）

| 中文名 | 文件名 |
|---|---|
| 引雷（原「破煞」，已改名） | `yin_lei.png` |
| 连雷（已降为普通） | `chain_thunder.png` |
| 清心咒 | `clear_mind.png` |
| 涤荡 | `purge.png` |
| 太极 | `tai_chi.png` |
| 冰蚕寒功（原阴煞，图标文件名仍为 `yin_sha.png`） | `yin_sha.png` |
| 诛邪 | `zhu_xie.png` |
| 移穴 | `acupoint_shift.png` |
| 风后奇门 | `wind_array.png` |
| 辟邪剑法 | `warding_blade.png` |
| 白长虫 | `white_worm.png` |
| 天火 | `sky_fire.png` |
| 拉伤（状态牌） | `la_shang.png`（与上表同一张） |

> 完整卡表（含每张卡的费用／类型／效果／卡面文件名）见工程根 `CARD_LIST_2026-09-19.md`。
