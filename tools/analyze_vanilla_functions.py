# -*- coding: utf-8 -*-
"""
原版卡牌功能分类分析（一代/二代 × 铁甲战士/静默猎手）。

标签体系与 `analyze_card_functions.py`（伊林）完全一致，便于跨池对比：
  出数值 / 助攻 / 费用操控 / 产炁 / 耗炁 / 产金光 / 耗金光 /
  补牌 / 平牌 / 亏牌 / 启动 / 局外成长 / 战斗内成长

口径说明：
  - 多选：一卡多标签（如 Bash = 出数值 + 助攻）。
  - 运转三态按「打出后手牌数 − 打出前」（本卡离手 −1），且只标
    「以手牌/牌堆/消耗堆为资源」的卡——纯攻击/格挡卡不标（与伊林口径一致）。
  - 原版无炁/金光资源 → 「产炁/耗炁/产金光/耗金光」四维恒空；
    原版的能量获取归「费用操控」，力量/敏捷增幅归「助攻」。
  - 毒=延迟数值 → 上毒标「出数值」，放大毒（Catalyst 等）标「助攻」。
  - 伊林数据从 analyze_card_functions.py 动态读取，按奖励池口径（79 张；单人 76）。
    原版四池是 wiki 全表（含 3–4 张初始牌），故另给「同口径占比」小节。
  - 数据来源：slaythespire.wiki.gg（一代）、slaythespire2.wiki.fextralife.com（二代，v0.107.1）。
"""

from pathlib import Path

import analyze_card_functions as A   # 伊林标签/口径单一来源，避免硬编码漂移

REPO_ROOT = Path(__file__).resolve().parent.parent
OUTPUT = REPO_ROOT / "docs" / "CARD_FUNCTION_ANALYSIS_VANILLA.md"

# 伊林按奖励池口径（与 docs/CARD_FUNCTION_ANALYSIS.md 一致）
YILIN_REWARD = {k: v for k, v in A.FUNCS.items()
                if k not in A.EXCLUDED and k not in A.STARTING_DECK}
YILIN_SOLO = {k: v for k, v in YILIN_REWARD.items()
              if k not in A.MULTIPLAYER_ONLY}

# 原版各角色初始牌：wiki 全表含这些牌，但它们不进卡牌奖励。
# 若 wiki 口径有变，只需要改这里。
STARTERS = {
    "一代·铁甲战士（75）": {"Bash", "Defend", "Strike"},
    "一代·静默猎手（75）": {"Strike", "Defend", "Neutralize", "Survivor"},
    "二代·铁甲战士（87）": {"Bash", "Defend", "Strike"},
    "二代·静默猎手（88）": {"Defend", "Strike", "Neutralize", "Survivor"},
}

TAG_ORDER = ["出数值", "助攻", "费用操控", "产炁", "耗炁", "产金光", "耗金光",
             "补牌", "平牌", "亏牌", "启动", "局外成长", "战斗内成长"]

POOLS = {
    "一代·铁甲战士（75）": {
        # Basic
        "Bash": ["出数值", "助攻"], "Defend": ["出数值"], "Strike": ["出数值"],
        # Common
        "Anger": ["出数值"], "Armaments": ["出数值", "助攻"], "Body Slam": ["出数值"],
        "Clash": ["出数值"], "Cleave": ["出数值"], "Clothesline": ["出数值", "助攻"],
        "Flex": ["助攻"], "Havoc": ["费用操控"], "Headbutt": ["出数值", "亏牌"],
        "Heavy Blade": ["出数值"], "Iron Wave": ["出数值"], "Perfected Strike": ["出数值"],
        "Pommel Strike": ["出数值", "平牌"], "Shrug It Off": ["出数值", "平牌"],
        "Sword Boomerang": ["出数值"], "Thunderclap": ["出数值", "助攻"],
        "True Grit": ["出数值", "亏牌"], "Twin Strike": ["出数值"], "Warcry": ["亏牌"],
        "Wild Strike": ["出数值"],
        # Uncommon
        "Battle Trance": ["补牌"], "Blood for Blood": ["出数值"], "Bloodletting": ["费用操控"],
        "Burning Pact": ["平牌"], "Carnage": ["出数值"], "Combust": ["出数值"],
        "Dark Embrace": ["补牌"], "Disarm": ["助攻"], "Dropkick": ["出数值", "费用操控", "平牌"],
        "Dual Wield": ["补牌"], "Entrench": ["出数值"], "Evolve": ["补牌"],
        "Feel No Pain": ["出数值"], "Fire Breathing": ["出数值"], "Flame Barrier": ["出数值"],
        "Ghostly Armor": ["出数值"], "Hemokinesis": ["出数值"], "Infernal Blade": ["平牌", "费用操控"],
        "Inflame": ["助攻"], "Intimidate": ["助攻"], "Metallicize": ["出数值"],
        "Power Through": ["出数值", "补牌"], "Pummel": ["出数值"], "Rage": ["出数值"],
        "Rampage": ["出数值", "战斗内成长"], "Reckless Charge": ["出数值"], "Rupture": ["助攻"],
        "Searing Blow": ["出数值", "战斗内成长"], "Second Wind": ["出数值", "亏牌"],
        "Seeing Red": ["费用操控"], "Sentinel": ["出数值", "费用操控"], "Sever Soul": ["出数值", "亏牌"],
        "Shockwave": ["助攻"], "Spot Weakness": ["助攻"], "Uppercut": ["出数值", "助攻"],
        "Whirlwind": ["出数值"],
        # Rare
        "Barricade": ["助攻"], "Berserk": ["费用操控"], "Bludgeon": ["出数值"],
        "Brutality": ["补牌"], "Corruption": ["费用操控"], "Demon Form": ["助攻"],
        "Double Tap": ["助攻"], "Exhume": ["平牌"], "Feed": ["出数值", "局外成长"],
        "Fiend Fire": ["出数值", "亏牌"], "Immolate": ["出数值"], "Impervious": ["出数值"],
        "Juggernaut": ["出数值"], "Limit Break": ["助攻"],
        "Offering": ["费用操控", "补牌", "启动"], "Reaper": ["出数值"],
    },
    "一代·静默猎手（75）": {
        # Basic
        "Strike": ["出数值"], "Defend": ["出数值"], "Neutralize": ["出数值", "助攻"],
        "Survivor": ["出数值", "亏牌"],
        # Common
        "Acrobatics": ["补牌"], "Backflip": ["出数值", "补牌"], "Bane": ["出数值"],
        "Blade Dance": ["补牌"], "Cloak and Dagger": ["出数值", "补牌"],
        "Dagger Spray": ["出数值"], "Dagger Throw": ["出数值", "亏牌"],
        "Deadly Poison": ["出数值"], "Deflect": ["出数值"], "Dodge and Roll": ["出数值"],
        "Flying Knee": ["出数值", "费用操控"], "Outmaneuver": ["费用操控"],
        "Piercing Wail": ["助攻"], "Poisoned Stab": ["出数值"], "Prepared": ["亏牌"],
        "Quick Slash": ["出数值", "平牌"], "Slice": ["出数值"],
        "Sneaky Strike": ["出数值", "费用操控"], "Sucker Punch": ["出数值", "助攻"],
        # Uncommon
        "Accuracy": ["助攻"], "All-Out Attack": ["出数值", "亏牌"], "Backstab": ["出数值"],
        "Blur": ["出数值"], "Bouncing Flask": ["出数值"], "Calculated Gamble": ["亏牌"],
        "Caltrops": ["出数值"], "Catalyst": ["助攻"], "Choke": ["出数值"],
        "Concentrate": ["费用操控", "亏牌"], "Crippling Cloud": ["助攻"], "Dash": ["出数值"],
        "Distraction": ["平牌", "费用操控"], "Endless Agony": ["出数值"],
        "Escape Plan": ["平牌"], "Eviscerate": ["出数值"], "Expertise": ["补牌"],
        "Finisher": ["出数值"], "Flechettes": ["出数值"], "Footwork": ["助攻"],
        "Heel Hook": ["出数值", "费用操控", "平牌"], "Infinite Blades": ["出数值"],
        "Leg Sweep": ["出数值", "助攻"], "Masterful Stab": ["出数值"],
        "Noxious Fumes": ["出数值"], "Predator": ["出数值", "补牌"], "Reflex": ["补牌"],
        "Riddle With Holes": ["出数值"], "Setup": ["费用操控"], "Skewer": ["出数值"],
        "Tactician": ["费用操控"], "Terror": ["助攻"], "Well-Laid Plans": [],
        # Rare
        "A Thousand Cuts": ["出数值"], "Adrenaline": ["费用操控", "补牌", "启动"],
        "After Image": ["出数值"], "Alchemize": [], "Bullet Time": ["费用操控"],
        "Burst": ["助攻"], "Corpse Explosion": ["出数值", "助攻"], "Die Die Die": ["出数值"],
        "Doppelganger": ["费用操控", "补牌", "启动"], "Envenom": ["助攻"],
        "Glass Knife": ["出数值", "战斗内成长"], "Grand Finale": ["出数值"],
        "Malaise": ["助攻"], "Nightmare": ["补牌"], "Phantasmal Killer": ["助攻"],
        "Storm of Steel": ["亏牌"], "Tools of the Trade": ["平牌"], "Unload": ["出数值", "亏牌"],
        "Wraith Form": ["助攻"],
    },
    "二代·铁甲战士（87）": {
        # Basic
        "Bash": ["出数值", "助攻"], "Defend": ["出数值"], "Strike": ["出数值"],
        # Common
        "Anger": ["出数值"], "Armaments": ["出数值", "助攻"], "Blood Wall": ["出数值"],
        "Bloodletting": ["费用操控"], "Body Slam": ["出数值"], "Breakthrough": ["出数值"],
        "Cinder": ["出数值", "亏牌"], "Havoc": ["费用操控"], "Headbutt": ["出数值", "亏牌"],
        "Iron Wave": ["出数值"], "Molten Fist": ["出数值", "助攻"], "Perfected Strike": ["出数值"],
        "Pommel Strike": ["出数值", "平牌"], "Setup Strike": ["出数值", "助攻"],
        "Shrug It Off": ["出数值", "平牌"], "Sword Boomerang": ["出数值"],
        "Thunderclap": ["出数值", "助攻"], "Tremble": ["助攻"], "True Grit": ["出数值", "亏牌"],
        "Twin Strike": ["出数值"],
        # Uncommon
        "Ashen Strike": ["出数值"], "Battle Trance": ["补牌"], "Bludgeon": ["出数值"],
        "Bully": ["出数值", "助攻"], "Burning Pact": ["平牌"], "Colossus": ["助攻"],
        "Demonic Shield": ["助攻"], "Dismantle": ["出数值"], "Dominate": ["助攻"],
        "Drum of Battle": ["费用操控", "补牌"], "Evil Eye": ["出数值"],
        "Expect a Fight": ["费用操控"], "Feel No Pain": ["出数值"], "Fight Me!": ["出数值", "助攻"],
        "Flame Barrier": ["出数值"], "Forgotten Ritual": ["费用操控"], "Hemokinesis": ["出数值"],
        "Howl from Beyond": ["出数值", "费用操控"], "Infernal Blade": ["平牌", "费用操控"],
        "Inferno": ["出数值"], "Inflame": ["助攻"], "Juggling": ["补牌"],
        "Pillage": ["出数值", "补牌"], "Rage": ["出数值"], "Rampage": ["出数值", "战斗内成长"],
        "Rupture": ["助攻"], "Second Wind": ["出数值", "亏牌"], "Spite": ["出数值"],
        "Stampede": ["费用操控"], "Stomp": ["出数值", "费用操控"], "Stone Armor": ["出数值"],
        "Taunt": ["出数值", "助攻"], "Unrelenting": ["出数值", "费用操控"],
        "Uppercut": ["出数值", "助攻"], "Vicious": ["助攻"], "Whirlwind": ["出数值"],
        # Rare
        "Aggression": ["补牌", "助攻"], "Barricade": ["助攻"], "Brand": ["费用操控", "助攻"],
        "Cascade": ["费用操控", "补牌"], "Conflagration": ["出数值"], "Crimson Mantle": ["出数值"],
        "Cruelty": ["助攻"], "Dark Embrace": ["补牌"], "Demon Form": ["助攻"],
        "Feed": ["出数值", "局外成长"], "Fiend Fire": ["出数值", "亏牌"],
        "Hellraiser": ["出数值", "费用操控"], "Impervious": ["出数值"], "Juggernaut": ["出数值"],
        "Mangle": ["出数值", "助攻"], "Not Yet": ["出数值"],
        "Offering": ["费用操控", "补牌", "启动"], "One-Two Punch": ["助攻"],
        "Pact's End": ["出数值"], "Primal Force": ["费用操控"], "Pyre": ["费用操控"],
        "Stoke": ["亏牌"], "Tank": ["助攻"], "Tear Asunder": ["出数值", "战斗内成长"],
        "Thrash": ["出数值", "亏牌"], "Unmovable": ["助攻"],
        # Ancient
        "Break": ["出数值", "助攻"], "Corruption": ["费用操控"],
    },
    "二代·静默猎手（88）": {
        # Basic
        "Defend": ["出数值"], "Strike": ["出数值"], "Neutralize": ["出数值", "助攻"],
        "Survivor": ["出数值", "亏牌"],
        # Common
        "Anticipate": ["助攻"], "Backflip": ["出数值", "补牌"], "Blade Dance": ["补牌"],
        "Cloak and Dagger": ["出数值", "补牌"], "Dagger Spray": ["出数值"],
        "Dagger Throw": ["出数值", "亏牌"], "Deadly Poison": ["出数值"], "Deflect": ["出数值"],
        "Dodge and Roll": ["出数值"], "Flick-Flack": ["出数值"],
        "Leading Strike": ["出数值", "补牌"], "Piercing Wail": ["助攻"],
        "Poisoned Stab": ["出数值"], "Predator": ["出数值", "补牌"], "Prepared": ["亏牌"],
        "Ricochet": ["出数值"], "Slice": ["出数值"], "Snakebite": ["出数值"],
        "Sucker Punch": ["出数值", "助攻"], "Untouchable": ["出数值"],
        # Uncommon
        "Accuracy": ["助攻"], "Acrobatics": ["补牌"], "Backstab": ["出数值"], "Blur": ["出数值"],
        "Bouncing Flask": ["出数值"], "Bubble Bubble": ["出数值"], "Calculated Gamble": ["亏牌"],
        "Dash": ["出数值"], "Escape Plan": ["平牌"], "Expertise": ["补牌"],
        "Expose": ["助攻"], "Finisher": ["出数值"], "Flanking": ["助攻"],
        "Flechettes": ["出数值"], "Footwork": ["助攻"], "Hand Trick": ["出数值", "费用操控"],
        "Haze": ["出数值", "助攻"], "Hidden Daggers": ["亏牌"], "Infinite Blades": ["出数值"],
        "Leg Sweep": ["出数值", "助攻"], "Memento Mori": ["出数值", "助攻"],
        "Mirage": ["出数值"], "Noxious Fumes": ["出数值"], "Outbreak": ["出数值", "助攻"],
        "Phantom Blades": ["出数值", "助攻"], "Pinpoint": ["出数值", "费用操控"],
        "Pounce": ["出数值", "费用操控"], "Precise Cut": ["出数值"], "Reflex": ["补牌"],
        "Scare": ["助攻"], "Skewer": ["出数值"], "Speedster": ["出数值", "助攻"],
        "Strangle": ["出数值"], "Tactician": ["费用操控"], "Up My Sleeve": ["补牌"],
        "Well-Laid Plans": [],
        # Rare
        "Abrasive": ["助攻"], "Accelerant": ["助攻"],
        "Adrenaline": ["费用操控", "补牌", "启动"], "Afterimage": ["出数值"],
        "Assassinate": ["出数值", "助攻"], "Blade of Ink": ["补牌"], "Bullet Time": ["费用操控"],
        "Burst": ["助攻"], "Corrosive Wave": ["出数值", "助攻"], "Echoing Slash": ["出数值"],
        "Envenom": ["助攻"], "Fan of Knives": ["出数值", "补牌", "助攻"],
        "Grand Finale": ["出数值"], "Knife Trap": ["出数值"], "Malaise": ["助攻"],
        "Master Planner": ["助攻"], "Murder": ["出数值", "战斗内成长"], "Nightmare": ["补牌"],
        "Serpent Form": ["出数值"], "Shadow Step": ["助攻", "亏牌"], "Shadowmeld": ["助攻"],
        "Sneaky": ["助攻"], "Storm of Steel": ["亏牌"], "The Hunt": ["出数值", "局外成长"],
        "Tools of the Trade": ["平牌"], "Tracking": ["助攻"],
        # Ancient
        "Suppress": ["出数值", "助攻"], "Wraith Form": ["助攻"],
    },
}

EXPECTED_SIZES = {"一代·铁甲战士（75）": 75, "一代·静默猎手（75）": 75,
                  "二代·铁甲战士（87）": 87, "二代·静默猎手（88）": 88}


def main() -> None:
    out = []
    out.append("# 原版卡牌功能分类分析（一代/二代 × 铁甲战士/静默猎手）")
    out.append("")
    out.append("> 由 `tools/analyze_vanilla_functions.py` 生成，标签体系与伊林")
    out.append("> （`docs/CARD_FUNCTION_ANALYSIS.md`）完全一致，可直接跨池对比。")
    out.append("> **多选口径**：一卡多标签，各类之和大于池大小。运转只标「以手牌/牌堆/消耗堆")
    out.append("> 为资源」的卡；纯攻/防卡不标。原版无炁/金光 → 该四维恒空（伊林独有资源体系），")
    out.append("> 原版能量获取归「费用操控」；毒=延迟数值（上毒→出数值，放大毒→助攻）。")
    out.append("> 二代数值基于 wiki v0.107.1。")
    out.append("> 伊林列从 `analyze_card_functions.py` 动态读取（不再硬编码），口径为奖励池。")
    out.append("")

    # 校验池大小
    for pool, cards in POOLS.items():
        want = EXPECTED_SIZES[pool]
        got = len(cards)
        status = "✓" if got == want else f"✗ 预期 {want}"
        print(f"{pool}: {got} 张 {status}")

    out.append("## 一、五池对比总表（张数）")
    out.append("")
    out.append(f"> 伊林列为**奖励池口径（{len(YILIN_REWARD)} 张）**：97 − 衍生3 − 任务1 − 状态1 − 雪藏9 − 初始4；")
    out.append(f"> 单人模式再扣 3 张联机限定 = {len(YILIN_SOLO)} 张。")
    out.append("> **口径差**：原版四池是 wiki「正式卡池全表」，含 3–4 张初始牌；")
    out.append("> 严格与伊林奖励池对齐时看下一节的同口径占比。")
    out.append("")
    out.append(f"| 分类 | 伊林·奖励池({len(YILIN_REWARD)}) | " + " | ".join(POOLS.keys()) + " |")
    out.append("|---|---|" + "---|" * len(POOLS))
    for tag in TAG_ORDER:
        n_y = sum(1 for t in YILIN_REWARD.values() if tag in t)
        row = f"| {tag} | {n_y} |"
        for pool, cards in POOLS.items():
            row += f" {sum(1 for t in cards.values() if tag in t)} |"
        out.append(row)
    out.append("")

    # ---- 同口径占比：五池都扣初始牌 ----
    reward_pools = {pool: {k: v for k, v in cards.items() if k not in STARTERS[pool]}
                    for pool, cards in POOLS.items()}
    heads = [f"伊林·单人({len(YILIN_SOLO)})"]
    for pool, rew in reward_pools.items():
        heads.append(f"{pool.split('（')[0]}·奖励({len(rew)})")
    out.append("## 一·B、同口径占比（五池都按奖励池，扣初始牌）")
    out.append("")
    out.append("> 原版初始牌按各角色公认起手牌扣除（脚本 `STARTERS`，wiki 口径变了改这里）；")
    out.append(f"> 伊林再扣 3 张联机限定 = 单人奖励池 {len(YILIN_SOLO)} 张。")
    out.append("> **多选口径**：各分类占比之和大于 100%，只做同一分类的横向比较。")
    out.append("")
    out.append("| 分类 | " + " | ".join(heads) + " |")
    out.append("|---|" + "---|" * len(heads))
    for tag in TAG_ORDER:
        cells = [f"{sum(1 for t in YILIN_SOLO.values() if tag in t) / len(YILIN_SOLO) * 100:.1f}%"]
        for rew in reward_pools.values():
            cells.append(f"{sum(1 for t in rew.values() if tag in t) / len(rew) * 100:.1f}%")
        out.append(f"| {tag} | " + " | ".join(cells) + " |")
    out.append("")

    # 每池明细
    for pool, cards in POOLS.items():
        out.append(f"## {pool}")
        out.append("")
        out.append("| 分类 | 张数 | 卡 |")
        out.append("|---|---|---|")
        for tag in TAG_ORDER:
            names = [n for n, t in cards.items() if tag in t]
            if not names:
                continue
            out.append(f"| {tag} | {len(names)} | " + "、".join(f"**{n}**" for n in names) + " |")
        no_tag = [n for n, t in cards.items() if not t]
        if no_tag:
            out.append(f"| （无标签） | {len(no_tag)} | " + "、".join(no_tag) + " |")
        out.append("")

    OUTPUT.write_text("\n".join(out), encoding="utf-8")
    print(f"已生成 {OUTPUT.name}")
    print(f"伊林：奖励池 {len(YILIN_REWARD)} / 单人 {len(YILIN_SOLO)}；各池统计见文档。")


if __name__ == "__main__":
    main()
