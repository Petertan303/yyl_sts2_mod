# 角色 伊林 (`yyl_sts2_mod`) — 设计稿 v0.1

> 工程根: `C:\Users\Peter_Tan\RiderProjects\yyl_sts2_mod`
> 本文档为**内容与机制**设计提案,实现侧状态以 `Code/` 为准。

---

## 0. 体系总览

| 体系 | 定位 | 核心资源 / 标志 | 反馈环 |
|---|---|---|---|
| 炁 体系 | 主轴 | 炁(每点使**造成**的伤害 +10%) | 产炁(偏防)→ 耗炁(偏攻 / 运转) |
| 奶龙 体系 | 副轴 | "是奶龙" 标记 | 给敌人贴 tag → 针对性牌 / 遗物 → 击杀回血 |
| 阶段 buff | 副轴 | 逆生(1 / 2 / 3 段) | 累积 → 段位提升 → 强效 |
| 既有支撑 | (沿用) | 4 个 stance、金光咒(-2 受伤/层) | 不在本文档展开 |

---

## 1. 能力 (Power / Buff)

### 1.1 资源型

| 名称 | 效果 | 备注 |
|---|---|---|
| **炁 (Qi)** | 每点使**造成**的伤害 ×1.10。Stack: Counter | 当前 `Powers/Qi.cs` 实现为 0.02 / 层,**需改为 0.10 / 层** |
| **老农功 (OldFarm)** | 获得炁时,额外 +1 炁(原"获得 X"实际得 X+1)。Stack: Counter | 当前 `Powers/OldFarm.cs` 是空壳,需补完 |

### 1.2 触发型

| 名称 | 效果 | 备注 |
|---|---|---|
| **温养 (WenYang)** | **获得炁时**,对所有敌人造成 4 → 6 伤害 | 需新建 `IGainQi` 钩子 |
| **丹噬 (DanShi)** | **失去炁时**,获得 4 → 6 格挡 | 需新建 `ILoseQi` 钩子 |

### 1.3 阶段型

**逆生 (ReverseLife)** — 按 `Amount` 取最高段位:

| 段位 | 触发 | 效果 |
|---|---|---|
| 一重 (Amount ≥ 1) | 回合开始(每回合首次) | 当次获得格挡 ×2 |
| 二重 (Amount ≥ 2) | 回合开始 | 治疗 6 HP |
| 三重 (Amount ≥ 3) | 回合开始 | 获得 1 层无实体 |

- 当前 `Powers/ReverseLife.cs` 已实现三段 + 首次格挡 ×2,需要对照本表确认 `BeforeSideTurnStart` 触发顺序与每回合"首次格挡"flag 重置时机。
- Token 链: `ReverseLife1 → ReverseLife2 → ReverseLife3`(`Code/Cards/Token/`,既有)。

### 1.4 持续 debuff

| 名称 | 效果 | 备注 |
|---|---|---|
| **天师度 (TianShiDu)** Power | 回合结束时失去 2 点炁 / 层。Stack: Counter | 由 §2.4 的"天师度"卡牌施加,本身不主动获得 |

### 1.5 一次性 / 触发

| 名称 | 效果 | 备注 |
|---|---|---|
| **性命双全 (XingMingShuangQuan)** | "下一张牌打出 2 次" | 需新建"双发"机制;StS2 的 Echo Form 可参考 |

---

## 2. 卡牌 — 产炁(防御倾向)

| 名称 | 建议稀有度 | 费用 | 效果 | 备注 |
|---|---|---|---|---|
| **吐纳 (TuNa)** | Basic | TBD | 获得 1 → 2 炁,抽 1,**消耗** | 简易发动机 |
| **蓄势 (XuShi)** | Common | TBD | **下回合开始时**获得 2 → 3 炁 | 需"延迟 buff"机制 |
| **炁流源体 (QiLiuYuanTi)** | Rare | TBD(估 X) | 将当前炁**翻倍**,消耗 | X = 0 时为白板 |
| **天师度 (TianShiDuCard)** | Rare | 1 | 获得 10 炁 + 3 层金光咒 + 给予 1 层 `TianShiDu` Power | 类比 Wraith Form(正向+持续负面) |
| **马步 (MaBu)** | Common | TBD | 获得 3 → 5 格挡,获得 1 → 2 炁 | 防御 + 产炁 |

---

## 3. 卡牌 — 耗炁(攻击 / 运转倾向)

| 名称 | 建议稀有度 | 费用 | 效果 | 备注 |
|---|---|---|---|---|
| **掌心雷 (ZhangXinLei)** | Common | TBD | 失去 1 炁,**造成 ? 伤害(待补)** | 描述未完成 |
| **移穴 (YiXue)** | Uncommon | TBD | 获得 10 → 15 格挡;失去 1 炁;从**抽牌堆**选 1 张入手 | 检索 |
| **天火 (TianHuo)** | Rare | TBD | **失去所有炁**,对所有敌人造成 "失去值 × 14 → 18" 伤害 | 强清场,数值待校 |
| **散炁 (SanQi)** | Uncommon | TBD(估 0) | 失去 1 炁;获得 2 → 3 能量 | 能量引擎 |
| **通畅 (TongChang)** | Uncommon | TBD | 失去 1 炁;抽 3 → 4 张 | 过牌引擎 |

---

## 4. 卡牌 — 奶龙体系

### 4.1 白卡 (Basic)

| 名称 | 费用 | 效果 |
|---|---|---|
| **指认 (ZhiRen)** | TBD | 造成 6 → 9 伤害;若目标**是奶龙**,获得 1 能量 |
| **心防 (XinFang)** | TBD | 获得 3 → 5 格挡;**本回合**受来自奶龙的伤害 -50% |

### 4.2 蓝卡 (Common / Uncommon)

| 名称 | 稀有 | 费用 | 效果 |
|---|---|---|---|
| **破防 (PoFang)** | Uncommon | TBD | 造成 6 伤害 2 → 3 次;每次命中使"奶龙" -1 力量;**消耗** |
| **狂热 (KuangRe)** | Uncommon | TBD | 攻击奶龙时,额外 +4 伤害 |
| **投喂 (TouWei)** | TBD(估 Rare) | TBD | 给予所有奶龙 10 格挡;获得 2 → 3 炁 + 2 能量 |

### 4.3 金卡 (Rare / Ancient)

| 名称 | 稀有 | 费用 | 效果 |
|---|---|---|---|
| **大啖食粮 (DaDanShiLiang)** | Rare | TBD | 对所有奶龙造成 4 伤害;**回复等同伤害的生命** |
| **黑色幽默 (HeiSeYouMo)** | Ancient | TBD | 为奶龙回复 20 HP;获得 3 炁 + 1 层无实体 |

> "奶龙"判定需要统一的 `IsNailong(target)` 工具,目前 `NlPower` / `NlPowerPlus` 同时承担"tag + 击杀奖励"两种职责,见 §6。

---

## 5. 遗物

### 5.1 初始 (Starter)

| 名称 | 效果 | 备注 |
|---|---|---|
| **黄桃罐头 (NlCan1)** | 战斗开始时获得 3 炁;**将所有敌人视作奶龙** | 当前 `Relics/NlCan1.cs` 只贴 NlPower,**未给 3 炁**,需补 |

### 5.2 普通
*(待设计)*

### 5.3 罕见 (Uncommon)

| 名称 | 效果 |
|---|---|
| **破损的奶龙玩偶** | 奶龙对伊林造成的伤害 -30% |

### 5.4 稀有 (Rare)
*(待设计)*

### 5.5 先古 (Ancient)

| 名称 | 效果 | 备注 |
|---|---|---|
| **大瓶黄桃罐头 (`BigYellowPeachCan`)** | 战斗开始时获得 5 炁;每点炁 +20% 伤害;**将所有人(包括队友)视作奶龙** | 复用 `NlPowerPlus`(与 `NlCan2` 的"视所有人为奶龙"一致) |

---

## 6. 设计备注 / 待定

### 命名 / 概念冲突(已澄清)
1. **"天师度"** = 一张卡 + 一个 Power 的同名组合(类比 Wraith Form):
   - 卡牌 `TianShiDuCard`:打出时给 +10 炁 + 3 金光咒 + 给予 Power `TianShiDu`
   - Power `TianShiDu`(debuff,Counter):每回合结束 -2 炁 / 层
   - 后续可让 debuff 随回合数累加(目前是固定 -2)
2. **大瓶黄桃罐头** = `BigYellowPeachCan` 单一遗物:5 炁 + 每点炁 +20% 伤害 + 把所有人视作奶龙。复用 `NlPowerPlus`(与 `NlCan2` 一致)。

### 机制空白
3. **奶龙 tag 机制**:目前 `NlPower`(敌人 tag + 击杀回血 3)与 `NlPowerPlus`(所有人 tag + 击杀回血 6)职责混合。建议拆为:
   - `NailongTag` 纯标记 Power
   - `NailongKillBonus` 击杀奖励 Power(可叠)
   - 工具函数 `IsNailong(Creature) => HasAny<NailongTag>()`
4. **"获得 / 失去 炁"钩子**:目前 `yylHook` 没有对应通道,需要按 `IModifyScryAmount` / `AfterModifyingScryAmount` 的模式新增 `IGainQi` / `ILoseQi`(纯函数 / 修改值,可选 `AfterModifying` follow-up)。
5. **"下一张牌打 2 次"**:BaseLib / StS2 已提供"Echo"相关接口的可能;需要先确认 `BaseLib` 是否有现成 Power 或命令。

### 数值 / 平衡风险
6. **炁 10% / 层**:相比现有 2% / 层是 5 倍跃升,旧 build 数值不能直接迁移,需要重跑平衡。
7. **天火**:"失去所有炁 × 14 → 18"在 10 炁时为 140 → 180 群伤,即使去 stance 乘算仍过强。考虑:
   - 加"最多 5 炁"
   - 加"本回合不可再获炁"
   - 改为"失去 1 炁,造成 14 → 18 伤害"取消翻倍
8. **通畅 1 炁换 3 → 4 抽**:在配合老农功、温养、10% 增伤下,单回合"刷牌 + 增伤"循环过强,需加"本回合抽牌上限 +N"而非直接抽。
9. **散炁 0 费** 与基类 0 费钩子的兼容性需验证。
10. **天师度卡(改名后)单回合 +10 炁 + 3 金光**:即使回 -2 仍有 8 炁 = +80% 伤害,需考虑 cd 或禁止叠加。

### 待补字段
11. 多张卡牌**费用**未指定(掌心雷 / 吐纳 / 蓄势 / 散炁 / 通畅 / 移穴 / 天火 / 马步 / 天师度 / 蓄势 / 投喂 / 指认 / 心防 / 破防 / 狂热 / 大啖食粮 / 黑色幽默)。
12. **稀有度**只对部分卡有建议,部分未指定(天师度卡 / 投喂 / 大啖食粮的具体稀有度)。
13. 中间档位遗物(普通 / 罕见非奶龙 / 稀有)仍空。
14. **效果范围**有歧义(投喂的"所有奶龙"指敌人还是所有?——按奶龙体系应为所有被标记的生物)。

### 工程现状参考
- `Code/Powers/Qi.cs` **已改 10% / 层**
- `Code/Powers/ReverseLife.cs` 三段已实现,需对照 §1.3
- `Code/Powers/OldFarm.cs` **已填充**:实现 `IGainQi`,获得炁时 +Amount
- `Code/Relics/NlCan1.cs` **已加 3 炁**(战前给 3 炁 + 贴 NlPower)
- `Code/Patches/MultiDamage.cs` 已提供"加 / 乘算"钩子桥;新 buff 可直接实现 `IModifyDamageMultiplicative` / `IModifyDamageAdditive`
- `Code/Patches/ModifyBlockAdditiveCompability.cs` 整文件注释,block 侧需依赖 BaseLib(≥ 3.3.5)直接 hook
- `Code/Commands/ScryCmd.cs` 整文件注释,本设计未使用 scry

### 费用/数值约定(临时)
- **所有新卡统一 1 费**,数值由用户后续手动调整。
- 原始设计为 X 费的(炁流源体)按 1 费实现。

### v0.1 实现状态(对照本设计)
| 模块 | 状态 |
|---|---|
| `IGainQi` / `ILoseQi` 钩子 + `yylHook` 派发 | ✅ |
| `yylCmd.GainQi` / `LoseQi` 辅助命令 | ✅ |
| `Qi` 10%/层 | ✅ |
| `OldFarm` 获得炁时 +Amount | ✅ |
| `WenYang` 获得炁群伤 | ✅ |
| `DanShi` 失去炁格挡 | ✅ |
| `TianShiDu` Power(debuff) | ⚠ 用了 `AfterSideTurnEnd` 钩子,**需验证方法名** |
| `XuShi` Power(下回合触发) | ✅ |
| `XingMingShuangQuan` Power | ⚠ 仅占位,"打 2 次"逻辑待补 |
| 5 产炁卡 (TuNa/XuShi/QiLiuYuanTi/TianShiDuCard/MaBu) | ✅ |
| 5 耗炁卡 (ZhangXinLei/YiXue/TianHuo/SanQi/TongChang) | ⚠ YiXue 的"从抽牌堆选 1 张"未实现 |
| 7 奶龙卡 (ZhiRen/XinFang/PoFang/KuangRe/TouWei/DaDanShiLiang/HeiSeYouMo) | ⚠ XinFang 的"本回合受奶龙伤害-50%"未实现 |
| `BigYellowPeachCan` 遗物 | ✅ |
| `Cards/Uncommon/OldFarm.cs` 老农功卡(WIP 占位) | ⚠ 保留不动,可能后续删 |
| 本地化(eng/cards/powers/relics) | ✅ 已加新条目 |
| 资源(图片/场景) | ⏳ 用户后续 |

### 已知的 API 风险(本机 StS2 未装,无法脱机验证)
- 继承基类是否真有 `AfterSideTurnEnd` / `BeforeCombatStart` 钩子 → 需要看 sts2.dll
- `Owner.Player` 转换我已避免(用 `combatState.Players.FirstOrDefault(p => p.Creature == Owner)`)
- `ValueProp.None` 可能不存在,可换成 `default(ValueProp)` 或 0
- `PlayerCmd.GainBlock(amount, player)` 签名按 `GainEnergy` 类比,可能需 `GainBlock(amount, player, ctx)` 等
- `PlayerCmd.Draw(player, amount)` 签名同理
- `CreatureCmd.GainBlock(creature, amount)` 同理
- `PowerCmd.Remove(this, ctx)` 是猜测,可能签名不同
- `CompatibilityCreatureCmd.Damage` 返回 `IEnumerable<DamageResult>`,`DamageResult.Amount` 是猜测字段名
