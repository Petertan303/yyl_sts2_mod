# -*- coding: utf-8 -*-
"""
伊林数值基准分析（只读，不生成/修改任何文件）。

数据来源：
  - 原版两角色 175 张：tools/data/vanilla_cards_v01071.tsv（wiki v0.107.1 基础值）
  - 伊林 97 张：yyl_sts2_mod/Code/Cards/**/*.cs 的 WithDamage / WithBlock / RepeatVar
  - 炁 / 金光公式：直接从 Code/Powers/Qi.cs、GoldenAegis.cs、Relics/PeachCan.cs 读取常量
  - 卡池口径：复用 tools/analyze_card_functions.py 的 EXCLUDED / STARTING_DECK / MULTIPLAYER_ONLY

口径约定：
  - 即时数值 = 基础伤害 × 段数 + 基础格挡；AoE 按「对每名敌人」计，不乘敌人数量。
  - 只取基础值，升级增量（WithDamage(6,3) 的第 2 参数）不参与统计。
  - 动态伤害卡（寸劲 / 天火 / 思维膨大 / 生生不息等）只取能确认的基础段值，见 MANUAL_DMG。
  - 原版引擎/条件牌（描述含「每当 / Whenever / 回合开始时 / 每」等）不计入分位数。

用法："C:/Users/Peter_Tan/.workbuddy/binaries/python/versions/3.13.12/python.exe" tools/analyze_number_baseline.py
"""
import json
import math
import re
import statistics as st
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
import gen_card_list as g  # noqa: E402
from analyze_card_functions import EXCLUDED, STARTING_DECK, MULTIPLAYER_ONLY  # noqa: E402

PROJECT = ROOT / "yyl_sts2_mod"
CARDS = PROJECT / "Code" / "Cards"
VANILLA = ROOT / "tools" / "data" / "vanilla_cards_v01071.tsv"
ZHS = json.loads(g.ZHS_JSON.read_text(encoding="utf-8"))

# 动态伤害卡：源码里能确认的第一段 / 基础伤害（其余留给人工判断）
MANUAL_DMG = {
    "寸劲": 3, "思维膨大": 4, "瞪视": 3, "诛邪": 8, "生生不息": 5,
    "天火": 0, "大啖食粮": 4, "奶龙无影脚": 12, "食炁": 26,
}
RE_DMG = re.compile(r"\bWithDamage\(\s*(\d+)\s*,")
RE_BLK = re.compile(r"\bWithBlock\(\s*(\d+)\s*,")
RE_REPEAT = re.compile(r"new\s+RepeatVar\(\s*(\d+)\s*\)")
RE_V_DMG = re.compile(r"(?:造成|Deal)\s*(\d+)\s*(?:点)?(?:伤害|damage)", re.I)
RE_V_BLK = re.compile(r"(?:获得|Gain)\s*(\d+)\s*(?:点)?(?:Block|格挡)", re.I)
RE_V_HITS = re.compile(r"[×xX]\s*(\d+)\s*次|(\d+)\s+times", re.I)
RE_ENGINE = re.compile(r"每当|Whenever|Every |回合开始时|At the start|若|If |本场|this combat|for each|每", re.I)
V_STARTERS = {"Strike", "Defend", "Neutralize", "Survivor"}
V_OUTLIERS = {"Break", "Hemokinesis", "Outbreak", "Impervious"}  # 先古 / 自伤代价 / 消耗型上限


def read_source_consts():
    qi = (PROJECT / "Code" / "Powers" / "Qi.cs").read_text(encoding="utf-8-sig")
    peach = (PROJECT / "Code" / "Relics" / "PeachCan.cs").read_text(encoding="utf-8-sig")
    aegis = (PROJECT / "Code" / "Powers" / "GoldenAegis.cs").read_text(encoding="utf-8-sig")
    coeff = float(re.search(r"LogCoeff\s*=\s*([\d.]+)m", qi).group(1))
    initial_qi = int(re.search(r"InitialQi\s*=\s*(\d+)", peach).group(1))
    per_stack = float(re.search(r"var perStack\s*=\s*([\d.]+)m", aegis).group(1))
    return coeff, initial_qi, per_stack


def parse_vanilla():
    rows = []
    for line in VANILLA.read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("#"):
            continue
        actor, name, cost, desc = line.split("\t", 3)
        dmg = [int(x) for x in RE_V_DMG.findall(desc)]
        blk = [int(x) for x in RE_V_BLK.findall(desc)]
        hits = [int(a or b) for a, b in RE_V_HITS.findall(desc)]
        if "twice" in desc.lower():
            hits.append(2)
        d = (dmg[0] * (hits[0] if hits else 1)) if dmg else 0
        b = blk[0] if blk else 0
        rows.append(dict(actor=actor, name=name, cost=cost, desc=desc, dmg=d, blk=b,
                         total=d + b, aoe=bool(re.search(r"对所有敌人|to ALL enemies|ALL enemies", desc, re.I)),
                         engine=bool(RE_ENGINE.search(desc))))
    return rows


def parse_yylin():
    rows = []
    for p in sorted(CARDS.rglob("*.cs")):
        info = g.parse_card_file(p)
        if not info:
            continue
        text = p.read_text(encoding="utf-8-sig")
        cls = info["class"]
        name = ZHS.get("YYL_STS2_MOD-" + g.pascal_to_upper_snake(cls) + ".title", "")
        d = [int(x) for x in RE_DMG.findall(text)]
        b = [int(x) for x in RE_BLK.findall(text)]
        h = [int(x) for x in RE_REPEAT.findall(text)]
        dmg = max(d) if d else 0
        blk = max(b) if b else 0
        hits = max(h) if h else 1
        if dmg == 0 and name in MANUAL_DMG:
            dmg = MANUAL_DMG[name]
        rows.append(dict(name=name, cls=cls, cost=info["cost"], rarity=info["rarity"],
                         dmg=dmg, blk=blk, hits=hits, total=dmg * hits + blk,
                         aoe=info["target"] == "AllEnemies",
                         reward=name not in EXCLUDED and name not in STARTING_DECK,
                         solo=name not in MULTIPLAYER_ONLY))
    return rows


def dist(vals):
    if not vals:
        return "n=0"
    s = sorted(vals)
    return (f"n={len(s):<3} min={s[0]:<3} 中位={st.median(s):<5g} "
            f"均值={st.mean(s):<5.1f} max={s[-1]}")


def layer(label, rows, role):
    out = []
    for cost in ["0", "1", "2", "3"]:
        if role == "v":
            base = [r for r in rows if r["cost"] == cost and r["total"] > 0 and not r["engine"]]
            atk = [r["total"] for r in base if r["dmg"] and not r["blk"] and not r["aoe"]]
            blk = [r["total"] for r in base if r["blk"] and not r["dmg"]]
            mix = [r["total"] for r in base if r["dmg"] and r["blk"]]
            aoe = [r["total"] for r in base if r["aoe"]]
        else:
            base = [r for r in rows if r["cost"] == cost and r["total"] > 0 and r["reward"]]
            atk = [r["total"] for r in base if r["dmg"] and not r["blk"] and not r["aoe"]]
            blk = [r["total"] for r in base if r["blk"] and not r["dmg"]]
            mix = [r["total"] for r in base if r["dmg"] and r["blk"]]
            aoe = [r["total"] for r in base if r["aoe"]]
        out.append((cost, atk, blk, mix, aoe))
    print(f"\n=== {label} ===")
    print(f"{'费':<3} {'单体攻击':<40} {'纯格挡':<40} {'伤+挡':<22} {'AoE/每敌':<22}")
    for cost, atk, blk, mix, aoe in out:
        print(f"{cost:<3} {dist(atk):<40} {dist(blk):<40} {dist(mix):<22} {dist(aoe):<22}")
    # 1 费奖励池基准组：排除初始牌、先古/代价 outlier、AoE、引擎、明显超模卡
    if role == "v":
        a = [r["total"] for r in rows if r["cost"] == "1" and r["dmg"] and not r["blk"]
             and not r["aoe"] and not r["engine"]
             and r["name"] not in V_STARTERS and r["name"] not in V_OUTLIERS]
        b = [r["total"] for r in rows if r["cost"] == "1" and r["blk"] and not r["dmg"]
             and not r["engine"] and r["name"] not in V_STARTERS]
    else:
        a = [r["total"] for r in rows if r["cost"] == "1" and r["dmg"] and not r["blk"]
             and not r["aoe"] and r["reward"] and r["rarity"] != "Ancient"
             and r["name"] not in {"血雷"}]
        b = [r["total"] for r in rows if r["cost"] == "1" and r["blk"] and not r["dmg"]
             and r["reward"]]
    print(f"  1 费奖励池基准 · 攻击: {dist(a)}")
    print(f"  1 费奖励池基准 · 格挡: {dist(b)}")


def main():
    coeff, initial_qi, aegis_per_stack = read_source_consts()
    v, y = parse_vanilla(), parse_yylin()
    print(f"源码常量: 炁系数={coeff}  战斗开始炁={initial_qi}  金光每层基础减伤={aegis_per_stack}")
    print(f"原版卡表 {len(v)} 张 / 伊林 {len(y)} 张；奖励池 {sum(1 for r in y if r['reward'])} 张")
    layer("原版 Ironclad + Silent（wiki v0.107.1 基础值）", v, "v")
    layer("伊林奖励池（源码 WithDamage/WithBlock/RepeatVar）", y, "y")

    print("\n=== 炁：倍率与边际（Qi.cs 对数公式） ===")
    print(f"{'Q':>3} {'倍率':>7} {'下一点增伤':>10} {'1费9伤牌增量':>12} {'5段3伤牌增量':>12}")
    for q in [0, 1, 2, 3, 5, 8, 10, 15, 20, 30, 40]:
        mult = 1 + coeff * math.log(1 + q)
        nxt = coeff * math.log((2 + q) / (1 + q))
        print(f"{q:>3} {mult:>7.3f} {nxt*100:>9.2f}% {9*nxt:>12.2f} {15*nxt:>12.2f}")
    base_mult = 1 + coeff * math.log(1 + initial_qi)
    print(f"  战斗开始 {initial_qi} 炁 → 常驻倍率 ×{base_mult:.3f}")
    for target in [8, 8.5, 9]:
        print(f"  对齐原版 {target} 伤需要的伊林牌面 = {target/base_mult:.2f} 伤")

    print("\n=== 金光：每层减伤的格挡当量（层数不消耗） ===")
    print("  剩余回合 × 每回合受击次数 → 1 层当量（点格挡）")
    for turns in [3, 4, 5, 8]:
        print(f"    {turns} 回合: " + "  ".join(
            f"{hits}次/回合={turns*hits*aegis_per_stack:g}点" for hits in [1, 2]))
    print("  对照：二代 Stone Armor 1 费 4 层镀甲 = 4+3+2+1 = 10 点（分 4 回合，层数递减）")
    print("  守势：每层额外减伤 +Amount（基础 +1 / 升级 +2）→ 当量 ×2 / ×3，同时炁增伤归 1")


if __name__ == "__main__":
    main()
