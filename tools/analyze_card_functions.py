# -*- coding: utf-8 -*-
"""
伊林卡牌功能分类分析。

标签体系（多选，一张卡可同时属于多类）：
  出数值      直接造成伤害 / 获得格挡 / 回复 / 使敌人失去生命等直接数值
  助攻        放大其他卡或队友的数值：易伤、虚弱、削弱(降力量/降攻)、翻倍、
              减伤辅助(净化/免疫/移除负面)、给队友资源、给敌人挂标记联动
  费用操控    获得能量、减少费用、打出其他卡（含额外回合）
  产炁 / 耗炁
  产金光 / 耗金光
  补牌 / 平牌 / 亏牌   运转三态：以「打出后手牌数 - 打出前手牌数」计
              （本卡离手 -1；净增=补牌，持平=平牌，净减=亏牌）
  启动        一次性给予大额过牌 / 费用 / 资源（帮助启动，非常规循环）
  局外成长    跨战斗的牌组/遗物成长
  战斗内成长  卡自身随战斗进程变强（降费/累计计数/叠层）

统计口径（2026-11-07 起，结构比例一律按奖励池算）：
  全量 97        所有卡牌类
  可获得 83       = 97 − 衍生3 − 任务1 − 状态1 − 雪藏9（含 4 种初始牌与 3 张联机卡）
  奖励池 79       = 83 − 4 种初始牌（Basic 池不进卡牌奖励）★ 推荐用于结构对比
  单人奖励池 76   = 79 − 3 张联机限定（单人模式不出现）
  原来说的「可获得 83」把固定起手牌和联机卡也算进了分母，会低估「抽到不能直接用的牌」
  的比例；跨池对比原版 wiki 全表时，也要注意原版全表含 3–4 张初始牌。

两套分布别混用：
  - 功能标签 = 多选，一卡多标签，各类占比之和约 167%，本来就不等于 100%；
  - 要看「加起来正好 100%」的结构分布，用第二节的互斥主类划分 + 第三节费用分布。

校验：与 cards.json 的全部卡名对齐（缺漏即报错），保证 97 张全覆盖。
用法: "C:/ProgramData/anaconda3/python.exe" tools/analyze_card_functions.py
输出: docs/CARD_FUNCTION_ANALYSIS.md
"""

import json
import re
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
ASSET_ROOT = REPO_ROOT / "yyl_sts2_mod" / "yyl_sts2_mod"
ZHS_JSON = ASSET_ROOT / "localization" / "zhs" / "cards.json"
OUTPUT = REPO_ROOT / "docs" / "CARD_FUNCTION_ANALYSIS.md"

# ---------------------------------------------------------------- 功能标签数据
# 键 = 卡名（与 cards.json 的 title 一致）；值 = 标签列表
FUNCS = {
    # ---- 基础 Basic（12，含 2026-11-02 雪藏的站桩） ----
    "打击": ["出数值"],
    "防御": ["出数值"],
    "侧踹": ["出数值"],
    "抢步": ["补牌"],
    "借力": ["出数值"],
    "掌心雷": ["出数值", "助攻", "耗金光"],
    "撩掌": ["出数值"],
    "点穴": ["出数值", "战斗内成长"],
    "纳炁": ["产炁"],
    "金光咒": ["出数值", "产金光"],
    "东尼之霸": ["出数值"],
    # ---- 普通 Common（24） ----
    "崩拳": ["出数值", "费用操控"],
    "头脑风暴": ["出数值", "费用操控", "战斗内成长"],
    "封门": ["出数值"],
    "套步": ["补牌"],
    "引光": ["产金光", "亏牌"],
    "引炁": ["产炁"],
    "心防": ["出数值", "助攻"],
    "截脉": ["出数值", "助攻"],
    "散炁": ["耗炁", "费用操控"],
    "清心咒": ["出数值", "助攻"],
    "炁冲": ["出数值", "耗炁"],
    "炁化金光": ["耗炁", "产金光", "出数值"],
    "破绽": ["助攻"],
    "舒筋": ["助攻"],
    "蓄势": ["产炁"],
    "运势": ["平牌"],
    "连雷": ["出数值"],
    "逃离": ["出数值"],
    "逗龙": ["出数值", "平牌"],
    "阿威十八式": ["出数值"],
    "顶肘": ["出数值"],
    "马步": ["出数值", "产炁"],
    "化劲": ["出数值"],
    "寸劲": ["出数值"],
    # ---- 罕见 Uncommon（32） ----
    "传功": ["耗炁", "助攻"],
    "挡灾": ["助攻"],
    "移穴": ["出数值", "耗炁", "平牌"],
    "站桩": ["出数值", "耗金光"],
    "返照": ["出数值", "耗金光", "产金光"],
    "磨镜成砖": ["出数值"],
    "丹噬": ["出数值", "战斗内成长"],
    "拔罐": ["助攻"],
    "养炁": ["产炁"],
    "温养": ["出数值"],
    "炁海": ["产炁"],
    "卜卦": ["耗炁", "补牌"],
    "吐纳": ["产炁", "平牌"],
    "通畅": ["耗炁", "补牌"],
    "金光壁": ["产金光"],
    "金光护体源": ["产金光"],
    "炼化": ["产金光", "亏牌"],
    "守势": ["助攻"],
    "指认": ["费用操控", "助攻"],
    "拾遗": ["平牌", "费用操控"],
    "涤荡": ["助攻"],
    "狂热": ["助攻"],
    "铁山靠": ["出数值"],
    "风后奇门": ["助攻", "补牌"],
    "食炁": ["出数值", "耗炁"],
    "奶龙无影脚": ["出数值"],
    "生生不息": ["出数值", "战斗内成长"],
    "瞪视": ["出数值", "助攻"],
    "破防": ["出数值", "助攻"],
    "思维膨大": ["出数值", "战斗内成长"],
    "太极": ["耗炁"],
    "引雷": ["出数值", "助攻"],
    "回收": ["费用操控", "亏牌"],
    # ---- 稀有 Rare（22） ----
    "共同狩猎": ["助攻"],
    "冰蚕寒功": ["助攻"],
    "大啖食粮": ["出数值"],
    "天师度": ["产炁", "产金光", "耗炁", "启动"],
    "天火": ["出数值", "耗炁"],
    "开脉": ["局外成长", "产炁"],
    "投喂": ["产炁", "费用操控", "亏牌"],
    "明镜止水": ["费用操控", "补牌", "启动"],
    "燃炁": ["产炁", "平牌"],
    "生旺死衰": ["费用操控", "亏牌"],
    "祖炁形态": ["助攻"],
    "神游": ["耗炁", "费用操控", "补牌", "启动"],
    "老农功": ["费用操控"],
    "血雷": ["出数值", "补牌"],
    "行炁": ["补牌"],
    "诛邪": ["出数值"],
    "逆生一重": ["助攻", "平牌"],
    "金光化炁": ["产炁", "耗金光"],
    "驭龙": ["助攻"],
    "黑色幽默": ["产炁", "助攻"],
    "五雷正法": ["出数值", "耗金光"],
    "炁流源体": ["产炁"],
    # ---- 先古 Ancient（2） ----
    "白长虫": ["出数值", "助攻", "耗金光"],
    "辟邪剑法": ["出数值"],
    # ---- 衍生 Token（3） ----
    "蛇花咬": ["出数值", "助攻"],
    "逆生二重": ["出数值", "平牌"],
    "逆生三重": ["助攻"],
    # ---- 任务 Quest（1） ----
    "蛇花": ["局外成长"],
    # ---- 状态 Status（1） ----
    "拉伤": [],  # 负面状态牌，不计功能标签
}

TAG_ZHS = {
    "出数值": "直接出数值（伤/格挡/回复等）",
    "助攻": "帮助其他卡/队友出数值（增幅·辅助）",
    "费用操控": "加费/伪加费（能量·减费·打出其他卡）",
    "产炁": "产炁",
    "耗炁": "耗炁",
    "产金光": "产金光",
    "耗金光": "耗金光",
    "补牌": "运转·补牌（打出后手牌更多）",
    "平牌": "运转·平牌（手牌数持平）",
    "亏牌": "运转·亏牌（打出后手牌更少）",
    "启动": "启动卡（一次性大额过牌/费用/资源）",
    "局外成长": "局外成长（跨战斗）",
    "战斗内成长": "战斗内成长（自身越打越强）",
}
TAG_ORDER = list(TAG_ZHS.keys())

# ---- 可获得口径（游戏实际能拿到/看到的卡）----
# 排除四类（用户 2026-11-02 确认）：
#   衍生 Token×3（蛇花咬/逆生二重/逆生三重）、任务×1（蛇花）、状态×1（拉伤）、
#   雪藏×9（Basic 池不进奖励且不在 StartingDeck：东尼之霸/侧踹/借力/抢步/撩掌/点穴/纳炁/站桩/回收）
# 注：游戏图鉴默认显示 88 张 = 常规四稀有度 89 − 东尼之霸(shouldShowInCardLibrary=false)；
#     先古 2 张与特殊稀有度 5 张靠图鉴筛选开关（_ancientsFilter/_miscPoolFilter）显示。
# 卡池变动时与 gen_card_list.py 的雪藏判定同步。
EXCLUDED = {
    "蛇花咬", "逆生二重", "逆生三重",   # Token
    "蛇花",                              # Quest
    "拉伤",                              # Status
    "东尼之霸", "侧踹", "借力", "抢步", "撩掌", "点穴", "纳炁", "站桩", "回收",  # 雪藏（站桩/回收 2026-11-02 雪藏）
}

TOKEN = {"蛇花咬", "逆生二重", "逆生三重"}
QUEST = {"蛇花"}
STATUS = {"拉伤"}
SNOW_HIDDEN = EXCLUDED - TOKEN - QUEST - STATUS

# 真初始牌组（StartingDeck 里的 4 种）：Basic 池不进卡牌奖励，不算奖励池分母。
STARTING_DECK = {"打击", "防御", "掌心雷", "金光咒"}
# 联机限定（MultiplayerConstraint.MultiplayerOnly）：单人模式奖励池里不出现。
MULTIPLAYER_ONLY = {"传功", "挡灾", "共同狩猎"}

# 互斥主类：按优先级命中一次即归类，用于「合计 100%」的结构分布。
PRIMARY_GROUPS = [
    ("直接出数值", lambda t: "出数值" in t),
    ("增幅/辅助", lambda t: "助攻" in t),
    ("费用/能量", lambda t: "费用操控" in t),
    ("炁/金光资源", lambda t: any(x in t for x in ("产炁", "耗炁", "产金光", "耗金光"))),
    ("运转·手牌", lambda t: any(x in t for x in ("补牌", "平牌", "亏牌"))),
    ("启动/成长", lambda t: any(x in t for x in ("启动", "局外成长", "战斗内成长"))),
    ("其他/无标签", lambda t: True),
]
COST_ORDER = ["0", "1", "2", "3", "X"]


def pool_label(name: str) -> str:
    if name in TOKEN:
        return "衍生"
    if name in QUEST:
        return "任务"
    if name in STATUS:
        return "状态"
    if name in SNOW_HIDDEN:
        return "雪藏"
    if name in STARTING_DECK:
        return "初始"
    if name in MULTIPLAYER_ONLY:
        return "联机"
    return "奖励池"


def primary_group(tags: list[str]) -> str:
    for name, hit in PRIMARY_GROUPS:
        if hit(tags):
            return name
    return "其他/无标签"


def load_costs() -> dict[str, str]:
    """从卡牌源码构造函数抓费用，返回 {中文卡名: 费用}（复用 gen_card_list 的解析器）。"""
    import gen_card_list as G

    zhs = json.loads(ZHS_JSON.read_text(encoding="utf-8"))
    costs: dict[str, str] = {}
    for path in sorted(G.CARDS_DIR.rglob("*.cs")):
        info = G.parse_card_file(path)
        if not info:
            continue
        key = f"YYL_STS2_MOD-{G.pascal_to_upper_snake(info['class'])}.title"
        title = zhs.get(key)
        if title:
            costs[title] = info["cost"]
    missing = sorted(set(FUNCS) - set(costs))
    if missing:
        raise SystemExit(f"费用解析缺卡：{missing}")
    return costs


def pct(n: int, d: int) -> str:
    return f"{n * 100 / d:.1f}%"


def main() -> None:
    d = json.loads(ZHS_JSON.read_text(encoding="utf-8"))
    titles = {v for k, v in d.items() if k.endswith(".title")}

    # 校验：数据覆盖全部卡名，且没有多余键
    missing = sorted(titles - FUNCS.keys())
    extra = sorted(FUNCS.keys() - titles)
    if missing or extra:
        print("⚠ 对不齐！")
        for t in missing:
            print("  缺标签:", t)
        for t in extra:
            print("  多余键:", t)
        raise SystemExit(1)

    costs = load_costs()
    pools = {
        "全量": dict(FUNCS),
        "可获得": {k: v for k, v in FUNCS.items() if k not in EXCLUDED},
        "奖励池": {k: v for k, v in FUNCS.items()
                if k not in EXCLUDED and k not in STARTING_DECK},
        "单人奖励池": {k: v for k, v in FUNCS.items()
                  if k not in EXCLUDED and k not in STARTING_DECK
                  and k not in MULTIPLAYER_ONLY},
    }
    n_all = len(pools["全量"])
    n_avail = len(pools["可获得"])
    n_reward = len(pools["奖励池"])
    n_solo = len(pools["单人奖励池"])

    def counts(tag: str) -> dict[str, int]:
        return {k: sum(1 for t in p.values() if tag in t) for k, p in pools.items()}

    out = []
    out.append("# 伊林卡牌功能分类分析（自动生成）")
    out.append("")
    out.append("> 由 `tools/analyze_card_functions.py` 生成（标签数据内嵌于脚本，改标签后重跑）。")
    out.append("> 卡池变动后运行：`\"C:/ProgramData/anaconda3/python.exe\" tools/analyze_card_functions.py`")
    out.append("")
    out.append("## 〇、统计口径（2026-11-07 起）")
    out.append("")
    out.append("**结构比例一律以奖励池为分母。** 四个口径逐层相扣：")
    out.append("")
    out.append("| 口径 | 张数 | 计算 |")
    out.append("|---|---|---|")
    out.append(f"| 全量 | {n_all} | 所有卡牌类（含衍生/任务/状态/雪藏） |")
    out.append(f"| 可获得 | {n_avail} | {n_all} − 衍生3 − 任务1 − 状态1 − 雪藏9 |")
    out.append(f"| **奖励池** | **{n_reward}** | {n_avail} − 4 种初始牌（打击/防御/掌心雷/金光咒；Basic 池不进奖励）★ 推荐 |")
    out.append(f"| 单人奖励池 | {n_solo} | {n_reward} − 3 张联机限定（传功/挡灾/共同狩猎） |")
    out.append("")
    out.append("两个易错点：")
    out.append("")
    out.append("- **功能标签是多选口径**（一卡可带多标签），各分类占比之和约 167%，")
    out.append("  本来就不等于 100%，也不该要求它等于 100%。要看「加起来正好 100%」的分布，")
    out.append("  用第二节的互斥主类划分和第三节的费用分布。")
    out.append("- 原版 wiki 的「正式卡池全表」含 3–4 张初始牌；与伊林奖励池严格对齐时应扣掉，")
    out.append("  见 `docs/CARD_FUNCTION_ANALYSIS_VANILLA.md` 的同口径对比。")
    out.append("")
    out.append("之前版本用「可获得 83」当分母（含固定起手牌和联机卡），")
    out.append("会低估「抽到不能直接用的牌」的比例，已废弃。")
    out.append("")

    out.append("## 一、功能标签统计（多选口径）")
    out.append("")
    out.append(f"| 分类 | 奖励池({n_reward}) | 占比 | 单人({n_solo}) | 占比 | 可获得({n_avail}) | 全量({n_all}) |")
    out.append("|---|---|---|---|---|---|---|")
    for tag in TAG_ORDER:
        c = counts(tag)
        out.append(
            f"| {TAG_ZHS[tag]} | {c['奖励池']} | {pct(c['奖励池'], n_reward)} "
            f"| {c['单人奖励池']} | {pct(c['单人奖励池'], n_solo)} "
            f"| {c['可获得']} | {c['全量']} |")
    none_n = sum(1 for tags in FUNCS.values() if not tags)
    if none_n:
        c_none = sum(1 for tags in pools["可获得"].values() if not tags)
        out.append(f"| （无功能标签的状态牌） | 0 | — | 0 | — | {c_none} | {none_n} |")
    sum_pct = sum(counts(t)["奖励池"] for t in TAG_ORDER) / n_reward * 100
    out.append(f"| **各类占比之和** | — | **{sum_pct:.1f}%** | — | — | — | — |")
    out.append("")
    out.append("> 多选口径：一张卡可同时属于多类（如马步 = 出数值 + 产炁）；")
    out.append("> 运转三态按「打出后手牌数 − 打出前手牌数」计（本卡离手 −1，条件抽牌按条件成立时计）。")
    out.append("")

    out.append("## 二、互斥主类划分（每卡只归一类，合计 100%）")
    out.append("")
    out.append("> 优先级：直接出数值 → 增幅/辅助 → 费用/能量 → 炁/金光资源 → 运转·手牌 → 启动/成长 → 其他。")
    out.append("> 复合卡按最先命中的类归，例如「马步」= 出数值 + 产炁 → 归直接出数值；")
    out.append("> 因此「启动/成长」类为空（3 张启动卡、4 张成长卡都同时带前面几类标签）。")
    out.append("")
    out.append(f"| 主类 | 奖励池({n_reward}) | 占比 | 单人({n_solo}) | 占比 |")
    out.append("|---|---|---|---|---|")
    for name, _ in PRIMARY_GROUPS:
        a = sum(1 for t in pools["奖励池"].values() if primary_group(t) == name)
        b = sum(1 for t in pools["单人奖励池"].values() if primary_group(t) == name)
        out.append(f"| {name} | {a} | {pct(a, n_reward)} | {b} | {pct(b, n_solo)} |")
    out.append(f"| **合计** | **{n_reward}** | **100.0%** | **{n_solo}** | **100.0%** |")
    out.append("")

    out.append("## 三、费用分布（各口径内合计 100%）")
    out.append("")
    out.append("> 费用抓自 `Code/Cards/**/*.cs` 构造函数，X 费单列。")
    out.append("> 旧结论「0 费 11 / 1 费 47 / 2 费 18 / 3 费 6 / X 费 1」合计 83，")
    out.append(f"> 是**可获得 {n_avail}** 口径，不是全量分母，故只覆盖 {n_all} 张的 {n_avail / n_all * 100:.1f}%。")
    out.append("")
    out.append(f"| 费用 | 奖励池({n_reward}) | 占比 | 单人({n_solo}) | 占比 | 可获得({n_avail}) | 全量({n_all}) |")
    out.append("|---|---|---|---|---|---|---|")
    for cost in COST_ORDER:
        c = {k: sum(1 for name in p if costs.get(name) == cost) for k, p in pools.items()}
        out.append(
            f"| {cost} 费 | {c['奖励池']} | {pct(c['奖励池'], n_reward)} "
            f"| {c['单人奖励池']} | {pct(c['单人奖励池'], n_solo)} "
            f"| {c['可获得']} | {c['全量']} |")
    out.append(f"| **合计** | **{n_reward}** | **100.0%** | **{n_solo}** | **100.0%** "
               f"| **{n_avail}** | **{n_all}** |")
    out.append("")

    out.append("## 四、按分类的卡列表（全量 97 张，含非奖励池卡）")
    out.append("")
    for tag in TAG_ORDER:
        names = [name for name, tags in FUNCS.items() if tag in tags]
        out.append(f"### {TAG_ZHS[tag]}（{len(names)}）")
        out.append("")
        out.append("、".join(f"**{n}**" for n in names))
        out.append("")

    out.append("## 五、逐卡标签矩阵")
    out.append("")
    out.append("> 「池」列标出该卡属于哪个口径；奖励池 = 默认分母，联机 = 单人模式不出现。")
    out.append("")
    header = "| 卡名 | 池 | " + " | ".join(TAG_ORDER) + " |"
    sep = "|---|" + "---|" * (len(TAG_ORDER) + 1)
    out += [header, sep]
    for name, tags in FUNCS.items():
        row = [name, pool_label(name)] + ["✓" if t in tags else "" for t in TAG_ORDER]
        out.append("| " + " | ".join(row) + " |")
    out.append("")

    OUTPUT.write_text("\n".join(out), encoding="utf-8")
    print(f"已生成 {OUTPUT.name}（{n_all} 张全覆盖）")
    print(f"口径：全量 {n_all} / 可获得 {n_avail} / 奖励池 {n_reward} / 单人 {n_solo}")
    for tag in TAG_ORDER:
        c = counts(tag)
        print(f"  {TAG_ZHS[tag]}: 奖励池 {c['奖励池']} ({pct(c['奖励池'], n_reward)})")


if __name__ == "__main__":
    main()
