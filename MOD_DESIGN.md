# 角色 伊林 (`yyl_sts2_mod`) — 设计稿 v0.2

> 工程根: `C:\Users\Peter_Tan\RiderProjects\yyl_sts2_mod`
> 本文档为**内容与机制**设计提案,实现侧状态以 `Code/` 为准。
> **v0.2 (2026-09-14)**: 新增 §7「先古卡 / 附魔 / 局外成长」, 并修正 §1.1 / §3 / §4.3 / §6 中与实现脱节的条目。

---

## 0. 体系总览

| 体系 | 定位 | 核心资源 / 标志 | 反馈环 |
|---|---|---|---|
| 炁 体系 | 主轴 | 炁(每点使**造成**的伤害 +10%) | 产炁(偏防)→ 耗炁(偏攻 / 运转) |
| 奶龙 体系 | 副轴 | "是奶龙" 标记 | 给敌人贴 tag → 针对性牌 / 遗物 → 击杀回血 / 累计养龙层数 |
| 阶段 buff | 副轴 | 逆生(1 / 2 / 3 段) | 累积 → 段位提升 → 强效 |
| 先古卡 | 支线 | 掌心雷 → 白长虫, 辟邪剑法 | 由先古 NPC 赠予 / 由初始牌升级而成, 详见 §7 |
| 局外成长 | 长线 | 附魔「炁脉」, 养龙层数 | 跨战斗保留, 详见 §7.3 / §7.4 |
| 既有支撑 | (沿用) | 4 个 stance、金光咒(-2 受伤/层) | 不在本文档展开 |

---

## 1. 能力 (Power / Buff)

### 1.1 资源型

| 名称 | 效果 | 备注 |
|---|---|---|
| **炁 (Qi)** | 每点使**造成**的伤害 ×1.10。Stack: Counter | 当前 `Powers/Qi.cs` 实现为 0.02 / 层,**需改为 0.10 / 层** |
| **老农功 (RusticRoot)** | 获得炁时,额外 +1 炁(原"获得 X"实际得 X+1)。Stack: Counter | **v0.2: 显示名改为「养炁」** —— 「老农功」这个名字交给炁婴那张卡了。当前无任何卡引用, 属孤儿 Power |

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
| ~~掌心雷 (ZhangXinLei)~~ | — | — | **v0.2: 这一条作废** | 原「耗炁线的低段位起手雷」不再重建; 「掌心雷」这个名字交给初始牌「阳五雷」继承, 详见 §7.1 |
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
| **黑色幽默 (DarkHumor)** | Rare | 2 | 为奶龙回复 20 HP;获得 3 炁 + 1 → 2 层无实体 | **v0.2 修正**: 稀有度是 Rare 不是 Ancient |

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

| 名称 | 效果 |
|---|---|
| **养龙 (NailongRearing)** | 每击杀一个[奶龙], 本遗物 +1 层(最多 10 层); 每层使你攻击[奶龙]时额外造成 1 点伤害。跨战斗累计, 详见 §7.4 |

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
- ~~**所有新卡统一 1 费**~~ → **v0.2 已作废**: 费用已按稀有度重排 (低稀有度常见 0 费, 高稀有度 2–3 费)。
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
| `Cards/Rare/PeasantDrill.cs` 老农功卡 | ✅ v0.2 重做: 不再是占位, 改为 2 费稀有技能「炁婴」(见 §7.1) |
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

---

## 7. v0.2 新增: 先古卡 / 附魔 / 局外成长

> 本节替代之前散落各处的"待设计"。以下内容**已实现并编译通过 (0 warning / 0 error)**。

### 7.1 雷法线: 掌心雷 → 白长虫

原作里阳五雷与阴五雷互斥、不能同时使用, 所以这两张牌不做成两张并列的牌, 而是**同一张牌的先后形态**。

| 名称 | 稀有度 | 费 | 效果 |
|---|---|---|---|
| **掌心雷 (PalmThunder)** | Basic (初始牌) | 1 | 对所有敌人造成 1 → 2 点伤害 5 次, 挂 1 层易伤 + 1 层虚弱; 有金光护体则消耗 1 层使本次伤害 +1 |
| **白长虫 (WhiteWorm)** | **Ancient** | 1 | 对所有敌人造成 2 → 3 点伤害 5 次 (**无视格挡**), 挂 1 → 2 层易伤 + 虚弱; 金光护体规则同上 |

- 初始牌组把原来的 `SolarThunder` 换成 `PalmThunder`(`Code/Character/yyl_sts2_mod.cs`)。
- **升级即变形**: 掌心雷被升级时, 由 `Code/Patches/PalmThunderUpgradePatch.cs` 在 `CardCmd.Upgrade` 之后调用 `CardCmd.TransformTo<WhiteWorm>`。StS2 没有声明式的"升级成另一张牌"接口(只有 `MaxUpgradeLevel` 控制层数), 所以用了 Harmony 后置补丁; 变形失败只记警告并保留普通升级结果。
- **无视格挡**的实现注记: `AttackCommand` 没有 `WithValueProp`, 伤害的 `ValueProp` 只能在声明伤害变量时给 —— 用 `WithCalculatedDamage("Damage", 2, _ => 0m, ValueProp.Unblockable, 1, 0)`(变量名故意仍叫 `Damage`, 卡面写法与其它卡一致)。
- ⚠ **待实机验证**: 升级变形是否与营火/事件/遗物的所有升级入口兼容。

### 7.2 辟邪剑法 (WardingBlade)

| 名称 | 稀有度 | 费 | 效果 |
|---|---|---|---|
| **辟邪剑法** | **Ancient** (先古卡的定位 = 由先古 NPC 赠予) | 2 | 造成 8 → 11 点伤害; 若目标带有任一负面状态(**易伤 / 虚弱 / 中毒 / 奶龙**)则本次伤害翻倍 |

- 「辟邪」= 专克邪祟: 目标越脏, 这一剑越重。和铺 debuff 的牌(掌心雷 / 风后奇门 / 破防 / 投喂)天然闭环。
- NPC 建议: **坦克斯 (TANX)** —— 他的台词本来就在找武器("使炁的小弟, 挑件正经的武器吧!!")。

### 7.3 附魔体系 (局外资源)

参考原版死灵法师的**禁忌魔典**(战斗结束后移除卡牌): 把"一次战斗的结算"变成对牌组的永久改造。这里改造成**附魔**而不是移除。

| 组件 | 名称 | 说明 |
|---|---|---|
| 卡牌 | **开脉 (MeridianOpening)** | 1 费能力牌 (Rare)。战斗结束后, 选择牌组中的 1 → 2 张牌附魔「炁脉」 |
| 能力 | **开脉 (MeridianOpening)** | `AfterCombatEnd(CombatRoom)` 里调 `CardSelectCmd.FromDeckForEnchantment` + `CardCmd.Enchant` |
| 附魔 | **炁脉 (QiMeridian)** | 被附魔的牌**打出时额外获得 1 点炁**。实现为 `BaseLib.Abstracts.CustomEnchantmentModel.OnPlay` |

为什么是「附魔 + 炁」: 原版角色的金币 / 药水 / 最大生命 / 牌组移除都已被占用; **炁是本角色独有的资源**, 而附魔是 StS2 原生就支持"永久写进牌组"的机制(BaseLib 提供 `CustomEnchantmentModel`)。两者结合就是本 mod 的局外资源。

- 附魔的本地化写在**新文件 `localization/eng/enchantments.json`**(表名 `enchantments`)。
- ⚠ **待实机验证**: 附魔的 `OnPlay` 是否会被"被附魔牌被打出"这一时机调用; 若不被调用, 改为用 `EnchantDamageAdditive` / `EnchantBlockAdditive`(改动约 2 行)。

### 7.4 养龙 (局外成长, 遗物)

| 名称 | 稀有度 | 效果 |
|---|---|---|
| **养龙 (NailongRearing)** | **Rare** | 每击杀一个[奶龙], 本遗物 +1 层 (**最多 10 层**); 每层使你攻击[奶龙]时额外造成 1 点伤害 |

- 跨战斗累计的成长型遗物。起始遗物「黄桃罐头」把所有敌人视作奶龙, 所以层数实际就是累计击杀数。
- **必须有上限**: 计数器永久保留, 无上限到后期会变成一回合秒杀(用户判断"有点超模", 故做成稀有遗物 + 10 层封顶)。
- 实现在 `Code/Relics/NailongRearing.cs`: `RelicRarity.Rare` + `IsStackable`/`ShowCounter` 覆盖 + `AbstractModel.AfterDeath` 钩子 + `IModifyDamageAdditive`。

### 7.5 本轮的 API 结论(踩坑记录)

| 需求 | 结论 |
|---|---|
| 从别的 mod 抄代码缺 using? | 用 `System.Reflection.Metadata` 读 `sts2.dll` 元数据直接查命名空间/可见性(virtual 与否也能查)。实例: `VakuuCardSelector` 在 `MegaCrit.Sts2.Core.Models.Relics`; `ModelDb.Enchantment<T>()` 可用 |
| 让攻击无视格挡 | `AttackCommand` 无 `WithValueProp`; 只能在声明伤害变量时给 props(`WithCalculatedDamage`) |
| 升级成另一张牌 | **无原生接口**; 需 Harmony 补丁 |
| 战斗结束做点什么 | `AbstractModel.AfterCombatEnd(CombatRoom room)`(卡 / 能力 / 遗物都能覆写) |
| 跨战斗计数的遗物 | `RelicModel.StackCount` + `IncrementStackCount()` + `IsStackable` + `ShowCounter`(引擎自动存档) |
| 自定义附魔 | `BaseLib.Abstracts.CustomEnchantmentModel`; 施加用 `CardCmd.Enchant`(同步, 返回附魔实例) |
| 遗物本地化 | `.title` / `.description` / **`.flavor` 三个键缺一不可**(STS001 分析器会报错) |
