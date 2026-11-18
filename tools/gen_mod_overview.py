# -*- coding: utf-8 -*-
"""
独立导出「伊林 mod 详细介绍」——输出两个版本：
  - docs/MOD_OVERVIEW.md     Markdown（GitHub/文档用）
  - docs/MOD_OVERVIEW_BBCODE.txt   BBCode（Steam 创意工坊简介用）

内容：
  1. 伊林角色介绍（脚本内固定文本，Markdown 版把 BBCode 转写）
  2. 卡牌列表（复用 gen_card_list 的解析，不含雪藏）
  3. Power 列表（Code/Powers 源码类 + 本地化）
  4. 遗物列表（初始遗物 / 初始升级遗物单列）
  5. 事件列表（Code/Events 的 ModEventTemplate + 本地化）

用法:
  "C:/Users/Peter_Tan/.workbuddy/binaries/python/versions/3.13.12/python.exe" tools/gen_mod_overview.py

设计: 本脚本**独立于** gen_card_list.py / analyze_*.py，不修改它们；
      只 import gen_card_list 的公开解析函数（记忆约定勿改名的那些），避免重复造轮子。
"""

import json
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent
sys.path.insert(0, str(SCRIPT_DIR))
import gen_card_list as G  # noqa: E402  复用卡牌/本地化解析

PROJ = G.PROJECT_ROOT
ASSET = G.ASSET_ROOT
POWERS_DIR = G.POWERS_DIR
RELICS_DIR = PROJ / "Code" / "Relics"
EVENTS_DIR = PROJ / "Code" / "Events"
CHARACTER_CS = G.CHARACTER_CS

OUT_MD = REPO_ROOT / "docs" / "MOD_OVERVIEW.md"
OUT_BB = REPO_ROOT / "docs" / "MOD_OVERVIEW_BBCODE.txt"

# ---------------------------------------------------------------- 角色介绍（用户给定原文）
INTRO_BB = """为yyl制作的mod。
添加了专属卡牌，有新增事件与遗物。
角色数值尽量贴合原版角色设计，手感介于原版战士与猎人之间，欢迎游玩。
[hr][/hr]
[h2]他的背景……[/h2]
伊林，来自尖塔外某个国度的流浪者，追寻着[b]黄色生物[/b]进入了尖塔。
他的身上充满着谜团……"既古老又年轻，既内敛又疯狂"，这个形容再合适不过了。
精神略微错乱的他，会将他遇见的所有人看成是他正在追寻着的[b]黄色生物[/b]。[spoiler]当然，也会连带着并肩作战的战友一起。请小心行事！[/spoiler]
[h2]特色机制[/h2]
伊林掌握了[b]炁[/b]和[b]金光[/b]的力量，分别对应攻防两端！
[b]炁[/b]能增加他攻击时造成的伤害。
[b]金光[/b]能抵消他受到的伤害，但是无法抵消超过持有金光最大层数的伤害，且用于抵消了伤害的金光会在受击后的敌方回合结束时消失。除此之外，炁与金光还能被部分卡牌作为资源使用……所以，[u]请慎重规划这两种[i]独特而强大[/i]的力量！[/u]"""


def bb_to_md(s: str) -> str:
    """把介绍文本的 BBCode 转成 Markdown（保留语义：标题/粗斜体/下划线/剧透折叠/分隔线）。"""
    s = re.sub(r"\[hr\]\[/hr\]", "\n\n---\n\n", s)
    s = re.sub(r"\[h2\](.*?)\[/h2\]", r"\n## \1\n", s)
    s = re.sub(r"\[spoiler\](.*?)\[/spoiler\]", r"\n\n<details><summary>剧透</summary>\n\n\1\n\n</details>\n", s)
    # 先处理嵌套 u+i，再处理单个标签
    s = re.sub(r"\[u\]\[i\](.*?)\[/i\]\[/u\]", r"<u>*\1*</u>", s)
    s = re.sub(r"\[b\](.*?)\[/b\]", r"**\1**", s)
    s = re.sub(r"\[i\](.*?)\[/i\]", r"*\1*", s)
    s = re.sub(r"\[u\](.*?)\[/u\]", r"<u>\1</u>", s)
    # 去掉本地化文案里常见的颜色标签（介绍文本本身没有，防御性处理）
    s = G.RE_BBCODE.sub("", s)
    return s.strip()


def cls_to_id(name: str) -> str:
    return G.pascal_to_upper_snake(name)


def src_classes(directory: Path, base_exclude: set[str] | None = None) -> list[str]:
    """从源码目录抓 public (sealed) class 名，排除基类/接口。"""
    names = set()
    for p in sorted(directory.glob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in re.finditer(r"^public\s+(?:sealed\s+)?(?:abstract\s+)?class\s+([A-Za-z_]\w*)", text, re.M):
            names.add(m.group(1))
    if base_exclude:
        names -= base_exclude
    return sorted(names)


def relic_rarity(text: str) -> str:
    m = re.search(r"Rarity\s*=>\s*RelicRarity\.(\w+)", text)
    return m.group(1) if m else "Unknown"


RARITY_ZH = {
    "Starter": "初始", "Common": "普通", "Uncommon": "罕见", "Rare": "稀有",
    "Ancient": "先古", "Shop": "商店", "Event": "事件", "Unknown": "未知",
}


def clean_text(s: str) -> str:
    """去掉颜色/样式 BBCode，压平换行为空格，供表格单元格使用。"""
    s = G.RE_BBCODE.sub("", s or "")
    return re.sub(r"\s+", " ", s.replace("\\n", " ")).strip()


# ---------------------------------------------------------------- 数据收集

def collect_cards(zhs, zhs_p, eng_p):
    starting = G.parse_starting_deck(CHARACTER_CS)
    cards = []
    for path in sorted(G.CARDS_DIR.rglob("*.cs")):
        info = G.parse_card_file(path)
        if info:
            cards.append(info)
    for c in cards:
        cid = cls_to_id(c["class"])
        c["title"] = zhs.get(f"{G.MOD_PREFIX}-{cid}.title", "")
        c["desc_tpl"] = zhs.get(f"{G.MOD_PREFIX}-{cid}.description", "")
        c["desc_base"], _ = G.render_desc(c["desc_tpl"], c["vars"], False, [], c["class"])
    hidden = [c for c in cards if c["rarity"] == "Basic" and c["class"] not in starting]
    visible = [c for c in cards if c not in hidden]
    visible.sort(key=lambda c: (
        G.RARITY_ORDER.index(c["rarity"]) if c["rarity"] in G.RARITY_ORDER else 99,
        c["title"] or c["class"],
    ))
    return visible, hidden


def collect_powers(zhs_p):
    rows = []
    for cls in src_classes(POWERS_DIR, base_exclude={"yylPowerModel"}):
        cid = cls_to_id(cls)
        title = zhs_p.get(f"{G.MOD_PREFIX}-{cid}.title", "")
        desc = zhs_p.get(f"{G.MOD_PREFIX}-{cid}.description", "")
        smart = zhs_p.get(f"{G.MOD_PREFIX}-{cid}.smartDescription", "")
        if not title and not desc:
            continue
        rows.append({
            "class": cls, "title": title or cls,
            "desc": clean_text(desc or smart),
        })
    rows.sort(key=lambda r: r["title"])
    return rows


def collect_relics(zhs):
    rows = []
    base = {"yyl_sts2_modRelic"}
    for cls in src_classes(RELICS_DIR, base_exclude=base):
        path = RELICS_DIR / f"{cls}.cs"
        rarity = relic_rarity(path.read_text(encoding="utf-8", errors="replace"))
        cid = cls_to_id(cls)
        title = zhs.get(f"{G.MOD_PREFIX}-{cid}.title", "")
        desc = zhs.get(f"{G.MOD_PREFIX}-{cid}.description", "")
        flavor = zhs.get(f"{G.MOD_PREFIX}-{cid}.flavor", "")
        rows.append({
            "class": cls, "rarity": rarity,
            "title": title or cls, "desc": clean_text(desc),
            "flavor": clean_text(flavor),
        })
    order = {k: i for i, k in enumerate(["Starter", "Ancient", "Rare", "Uncommon", "Common"])}
    rows.sort(key=lambda r: order.get(r["rarity"], 99))
    return rows


def collect_events(zhs):
    rows = []
    for p in sorted(EVENTS_DIR.glob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        if "ModEventTemplate" not in text:
            continue
        m = re.search(r'KeyPrefix\s*=\s*"([^"]+)"', text)
        if not m:
            continue
        prefix = m.group(1)
        title = zhs.get(f"{prefix}.title", "")
        initial = zhs.get(f"{prefix}.pages.INITIAL.description", "")
        rows.append({
            "class": p.stem, "prefix": prefix,
            "title": title or p.stem,
            "initial": clean_text(initial) or "—",
        })
    rows.sort(key=lambda r: r["title"])
    return rows


# ---------------------------------------------------------------- 渲染 Markdown

def md_table(headers, rows):
    out = ["| " + " | ".join(headers) + " |",
           "|" + "|".join(["---"] * len(headers)) + "|"]
    for r in rows:
        out.append("| " + " | ".join(str(x) for x in r) + " |")
    return out


def render_md(intro, cards, powers, relics, events, hidden):
    L = ["# 伊林 Mod 详细介绍", "", "> 由 tools/gen_mod_overview.py 自动生成，勿手工编辑。", ""]
    L += ["## 一、角色介绍", "", intro, ""]

    L += ["## 二、卡牌列表", "", f"> 共 {len(cards)} 张（不含雪藏 {len(hidden)} 张）。数值为源码实际值（基础态），升级变化见原卡面。", ""]
    cur = None
    rows = []
    grouped = []
    for c in cards:
        if c["rarity"] != cur:
            if rows:
                grouped.append((cur, rows))
            cur, rows = c["rarity"], []
        rows.append((f"**{c['title'] or c['class']}**", c["cost"],
                     G.TYPE_ZHS.get(c["type"], c["type"]), c["desc_base"] or "—"))
    if rows:
        grouped.append((cur, rows))
    for rarity, group in grouped:
        L += [f"### {G.RARITY_ZHS.get(rarity, rarity)} {rarity}（{len(group)}）", ""]
        L += md_table(["卡名", "费用", "类型", "基础效果"], group)
        L.append("")

    L += ["## 三、Power 列表", "",
          f"> 共 {len(powers)} 个（取自 Code/Powers 源码类）。",
          "> 说明中的 `{Amount}` 为该 power 的当前层数，由游戏运行时替换。", ""]
    L += md_table(["名称", "类名", "效果"], [(f"**{p['title']}**", p["class"], p["desc"] or "—") for p in powers])
    L.append("")

    L += ["## 四、遗物列表", ""]
    starters = [r for r in relics if r["rarity"] == "Starter"]
    upgraded = [r for r in relics if r["class"] == "NlCan2"]
    others = [r for r in relics if r not in starters and r not in upgraded]

    def relic_rows(rs):
        return [(f"**{r['title']}**", RARITY_ZH.get(r["rarity"], r["rarity"]), r["desc"] or "—") for r in rs]

    if starters:
        L += ["### 初始遗物", ""] + md_table(["名称", "稀有度", "效果"], relic_rows(starters)) + [""]
    if upgraded:
        L += ["### 初始升级遗物", ""] + md_table(["名称", "稀有度", "效果"], relic_rows(upgraded)) + [""]
    L += [f"### 其它遗物（{len(others)}）", ""] + md_table(["名称", "稀有度", "效果"], relic_rows(others)) + [""]

    L += ["## 五、事件列表", "", f"> 共 {len(events)} 个。描述为事件的初始页面文案。",
          "> `{CurseCard}` / `{QuestCard}` 等占位符由游戏运行时替换为实际卡牌。", ""]
    L += md_table(["名称", "事件 ID", "初始描述"], [(f"**{e['title']}**", e["prefix"], e["initial"]) for e in events])
    L.append("")
    return "\n".join(L)


# ---------------------------------------------------------------- 渲染 BBCode（Steam）

def bb_escape(s: str) -> str:
    """表格里 | 会破坏 [table] 结构，转义之。"""
    return (s or "").replace("|", "¦").replace("\n", " ")


def bb_table(headers, rows):
    out = ["[table]", "[tr]" + "".join(f"[th]{bb_escape(h)}[/th]" for h in headers) + "[/tr]"]
    for r in rows:
        out.append("[tr]" + "".join(f"[td]{bb_escape(x)}[/td]" for x in r) + "[/tr]")
    out.append("[/table]")
    return out


def render_bb(intro, cards, powers, relics, events, hidden):
    L = ["[h1]伊林 Mod 详细介绍[/h1]", "", intro, ""]
    L += ["[h2]二、卡牌列表[/h2]", f"共 {len(cards)} 张（不含雪藏 {len(hidden)} 张）。", ""]
    cur = None
    rows = []
    grouped = []
    for c in cards:
        if c["rarity"] != cur:
            if rows:
                grouped.append((cur, rows))
            cur, rows = c["rarity"], []
        rows.append((f"[b]{c['title'] or c['class']}[/b]", c["cost"],
                     G.TYPE_ZHS.get(c["type"], c["type"]), c["desc_base"] or "—"))
    if rows:
        grouped.append((cur, rows))
    for rarity, group in grouped:
        L += [f"[h3]{G.RARITY_ZHS.get(rarity, rarity)} {rarity}（{len(group)}）[/h3]"]
        L += bb_table(["卡名", "费用", "类型", "基础效果"], group)
        L.append("")

    L += [f"[h2]三、Power 列表[/h2]（共 {len(powers)} 个）", "说明中的 {Amount} 为该 power 的当前层数，由游戏运行时替换。", ""]
    L += bb_table(["名称", "效果"], [(f"[b]{p['title']}[/b]", p["desc"] or "—") for p in powers])
    L.append("")

    L += ["[h2]四、遗物列表[/h2]", ""]
    starters = [r for r in relics if r["rarity"] == "Starter"]
    upgraded = [r for r in relics if r["class"] == "NlCan2"]
    others = [r for r in relics if r not in starters and r not in upgraded]
    rr = lambda rs: [(f"[b]{r['title']}[/b]", RARITY_ZH.get(r["rarity"], r["rarity"]), r["desc"] or "—") for r in rs]
    if starters:
        L += ["[h3]初始遗物[/h3]"] + bb_table(["名称", "稀有度", "效果"], rr(starters)) + [""]
    if upgraded:
        L += ["[h3]初始升级遗物[/h3]"] + bb_table(["名称", "稀有度", "效果"], rr(upgraded)) + [""]
    L += [f"[h3]其它遗物（{len(others)}）[/h3]"] + bb_table(["名称", "稀有度", "效果"], rr(others)) + [""]

    L += [f"[h2]五、事件列表[/h2]（共 {len(events)} 个，描述为初始页面文案）", "{CurseCard} / {QuestCard} 等占位符由游戏运行时替换为实际卡牌。", ""]
    L += bb_table(["名称", "初始描述"], [(f"[b]{e['title']}[/b]", e["initial"]) for e in events])
    L.append("")
    return "\n".join(L)


def main():
    zhs_cards = G.load_json(G.ZHS_JSON)
    zhs_p = G.load_json(G.ZHS_POWERS_JSON)
    zhs_relics = G.load_json(ASSET / "localization" / "zhs" / "relics.json")
    zhs_events = G.load_json(ASSET / "localization" / "zhs" / "events.json")
    cards, hidden = collect_cards(zhs_cards, zhs_p, {})
    powers = collect_powers(zhs_p)
    relics = collect_relics(zhs_relics)
    events = collect_events(zhs_events)
    intro_md = bb_to_md(INTRO_BB)
    intro_bb = INTRO_BB

    OUT_MD.write_text(render_md(intro_md, cards, powers, relics, events, hidden), encoding="utf-8")
    OUT_BB.write_text(render_bb(intro_bb, cards, powers, relics, events, hidden), encoding="utf-8")

    print(f"已生成：{OUT_MD.name} / {OUT_BB.name}")
    print(f"  卡牌 {len(cards)}（雪藏 {len(hidden)}）/ Power {len(powers)} / 遗物 {len(relics)} / 事件 {len(events)}")


if __name__ == "__main__":
    main()
