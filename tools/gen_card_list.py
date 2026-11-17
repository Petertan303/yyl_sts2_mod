# -*- coding: utf-8 -*-
"""
自动生成伊林卡牌清单 CARD_LIST.md —— 含基础/升级实际数值 + Power 附录。

数据来源（全部权威，不做人工猜测）:
  - Code/Cards/**/*.cs          稀有度/费用/类型/目标/数值变量/升级增量/特性
  - Code/Powers/**/*.cs         power 常量（DefaultAmount / UpgradeAmount）与实现类
  - localization/{zhs,eng}/cards.json、powers.json   卡名与描述模板
  - Code/Character/yyl_sts2_mod.cs 的 StartingDeck   判定 Basic 是否为雪藏

用法:
  "C:/Users/Peter_Tan/.workbuddy/binaries/python/versions/3.13.12/python.exe" tools/gen_card_list.py
  （或任意 python3；路径以本脚本位置自动上溯到仓库根）

规则:
  - 卡牌 Id = 类名 PascalCase -> UPPER_SNAKE（yylDefend -> YYL_DEFEND），
    本地化键 = "YYL_STS2_MOD-<Id>.title/.description"。
  - {Var:diff()} 等占位符用源码里 With* 声明的变量替换为**实际数值**；
    描述渲染两遍（基础 / 升级后），升级列里变化的数字加粗。
  - Basic 组区分「初始牌组」与「雪藏」：只有出现在 StartingDeck 里的 Basic 才是真初始牌；
    用户约定 Basic 池不进卡牌奖励，塞进 Basic = 雪藏 —— 本清单**不收录雪藏牌**。
  - 卡牌关键词（消耗 / 保留 / 固有 / 虚无 / 不可打出）由 WithKeyword(s) 声明，
    引擎会自动把词条文字追加到游戏卡面，但本地化描述模板里不写，故清单单列一列，
    并区分基础 / 升级后两态；文末附关键词统计便于核对漏抓。
  - 双向核对: 类没有本地化键、或键没有类、或占位符无法解析，都打印告警。

注意: 本模块被 analyze_card_functions.py / analyze_number_baseline.py 复用，
      CARDS_DIR / ZHS_JSON / pascal_to_upper_snake / parse_card_file 等公开名字请勿改名。
"""

import json
import re
import sys
from collections import defaultdict
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent          # tools/
REPO_ROOT = SCRIPT_DIR.parent                         # 仓库根
PROJECT_ROOT = REPO_ROOT / "yyl_sts2_mod"             # 工程根（含 Code/）
ASSET_ROOT = PROJECT_ROOT / "yyl_sts2_mod"            # 资产根（含 localization/）
CARDS_DIR = PROJECT_ROOT / "Code" / "Cards"
POWERS_DIR = PROJECT_ROOT / "Code" / "Powers"
ZHS_JSON = ASSET_ROOT / "localization" / "zhs" / "cards.json"
ENG_JSON = ASSET_ROOT / "localization" / "eng" / "cards.json"
ZHS_POWERS_JSON = ASSET_ROOT / "localization" / "zhs" / "powers.json"
ENG_POWERS_JSON = ASSET_ROOT / "localization" / "eng" / "powers.json"
CHARACTER_CS = PROJECT_ROOT / "Code" / "Character" / "yyl_sts2_mod.cs"
OUTPUT = REPO_ROOT / "docs" / "CARD_LIST.md"

MOD_PREFIX = "YYL_STS2_MOD"

RARITY_ORDER = ["Basic", "Common", "Uncommon", "Rare", "Ancient", "Token", "Quest", "Status", "Curse"]
RARITY_ZHS = {
    "Basic": "基础", "Common": "普通", "Uncommon": "罕见", "Rare": "稀有",
    "Ancient": "先古", "Token": "衍生", "Quest": "任务", "Status": "状态", "Curse": "诅咒",
}
TYPE_ZHS = {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Status": "状态", "Curse": "诅咒", "Quest": "任务"}
TARGET_ZHS = {
    "AnyEnemy": "任一敌人", "AllEnemies": "全体敌人", "Self": "自身",
    "AnyAlly": "任一队友", "AllAllies": "全体队友", "RandomEnemy": "随机敌人", "None": "无",
}

# 卡牌关键词：引擎会自动把词条文字追加到游戏卡面（sts2.xml 的 CardKeyword 摘要），
# 但我们的 zhs/cards.json 描述模板不写它们，所以清单必须单独从 WithKeyword(s) 抓。
# 中文用词沿用工程内既有说法；未列出的新关键词保留英文原名，不猜译名。
KEYWORD_ZHS = {
    "Exhaust": "消耗", "Retain": "保留", "Innate": "固有",
    "Ethereal": "虚无", "Unplayable": "不可打出",
}
KEYWORD_ORDER = ["Exhaust", "Retain", "Innate", "Ethereal", "Unplayable"]

# With* 方法里与卡面数值有关的那些（WithKeywords / WithKeyword 由 extract_keywords 单独处理）
NUMERIC_CALLS = {
    "WithDamage", "WithBlock", "WithCards", "WithEnergy", "WithHeal",
    "WithVar", "WithVars", "WithPower", "WithCalculatedDamage",
    "WithCalculatedVar", "WithCostUpgradeBy",
}

RE_BBCODE = re.compile(r"\[/?(?:gold|blue|red|green|gray|yellow|purple|pink|i|b|u|s|sup|sub)\]")
RE_INCOMBAT = re.compile(r"\{InCombat:.*?\|\}", re.S)
RE_IFUPGRADED = re.compile(r"\{IfUpgraded:show:([^}]*)\}")
RE_ENERGYPREFIX = re.compile(r"\{energyPrefix:energyIcons\([^)]*\)\}")
RE_DIFF = re.compile(r"\{(\w+):diff\(\)\}")
RE_ENERGY = re.compile(r"\{(\w+):energyIcons\(\)\}")
RE_LEFTOVER = re.compile(r"\{[^}]*\}")

# ---------------------------------------------------------------- helpers


def pascal_to_upper_snake(name: str) -> str:
    """SideKick -> SIDE_KICK; yylDefend -> YYL_DEFEND; TouNaoFengBao -> TOU_NAO_FENG_BAO"""
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", name).upper()


def clean_desc(desc: str) -> str:
    """去掉 BBCode 颜色标签，保留 {动态变量} 原样；压平换行。"""
    s = RE_BBCODE.sub("", desc)
    # 本地化里既有真换行，也有字面量 \n（两个字符），都要压平；
    # 上一行没有句读时补一个分号，避免「逆生」那类逐行词条连成一串。
    s = s.replace("\\n", "\n").replace("\r", "")
    s = re.sub(
        r"\s*\n\s*",
        lambda m: "" if s[:m.start()].rstrip()[-1:] in "。！？；，、：.!?;," else "；",
        s,
    )
    return re.sub(r"\s{2,}", " ", s).strip()


def parse_cost(raw: str) -> str:
    raw = raw.strip()
    if raw in ("int.MinValue", "Int32.MinValue"):
        return "X"
    return raw


def strip_comments(text: str) -> str:
    """去掉 // 与 /* */ 注释（保留字符串里的 //，如 res:// 路径）。"""
    out = []
    i, n = 0, len(text)
    state = "code"
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""
        if state == "code":
            if c == "/" and nxt == "/":
                state = "line"
                out.append("  ")
                i += 2
                continue
            if c == "/" and nxt == "*":
                state = "block"
                out.append("  ")
                i += 2
                continue
            if c == '"':
                state = "str"
            elif c == "'":
                state = "char"
            out.append(c)
            i += 1
        elif state == "line":
            if c == "\n":
                state = "code"
                out.append("\n")
            else:
                out.append(" ")
            i += 1
        elif state == "block":
            if c == "*" and nxt == "/":
                state = "code"
                out.append("  ")
                i += 2
            else:
                out.append("\n" if c == "\n" else " ")
                i += 1
        elif state == "str":
            out.append(c)
            if c == "\\" and nxt:
                out.append(nxt)
                i += 2
                continue
            if c == '"':
                state = "code"
            i += 1
        else:  # char
            out.append(c)
            if c == "\\" and nxt:
                out.append(nxt)
                i += 2
                continue
            if c == "'":
                state = "code"
            i += 1
    return "".join(out)


def mask_strings(text: str) -> str:
    """把字符串/字符字面量的内容替换为空格（长度不变），避免误把注释外的字符串 Whence 当作调用。"""
    chars = list(text)
    i, n = 0, len(chars)
    while i < n:
        c = chars[i]
        if c in ('"', "'"):
            q = c
            chars[i] = " "
            i += 1
            while i < n:
                if chars[i] == "\\":
                    chars[i] = " "
                    if i + 1 < n:
                        chars[i + 1] = " "
                    i += 2
                    continue
                if chars[i] == q:
                    chars[i] = " "
                    i += 1
                    break
                chars[i] = " "
                i += 1
        else:
            i += 1
    return "".join(chars)


def scan_calls(text: str, names: set[str] | None = None) -> list[tuple[str, str | None, str]]:
    """扫描源码里的 With* 调用，返回 [(方法名, 泛型参数|None, 实参串)]，支持跨行与嵌套括号。

    names 默认只取数值相关的 With*；extract_keywords 会传入 {"WithKeyword", "WithKeywords"}。
    """
    wanted = NUMERIC_CALLS if names is None else names
    masked = mask_strings(text)
    out = []
    for m in re.finditer(r"\b(With[A-Za-z]+)\b", masked):
        name = m.group(1)
        if name not in wanted:
            continue
        i = m.end()
        while i < len(text) and text[i].isspace():
            i += 1
        generic = None
        if i < len(text) and text[i] == "<":
            depth, j = 0, i
            while j < len(text):
                if text[j] == "<":
                    depth += 1
                elif text[j] == ">":
                    depth -= 1
                    if depth == 0:
                        break
                j += 1
            generic = text[i + 1:j].strip()
            i = j + 1
            while i < len(text) and text[i].isspace():
                i += 1
        if i >= len(text) or text[i] != "(":
            continue
        start, depth = i + 1, 1
        j, in_str, quote = start, False, ""
        while j < len(text) and depth:
            ch = text[j]
            if in_str:
                if ch == "\\":
                    j += 2
                    continue
                if ch == quote:
                    in_str = False
            elif ch in ('"', "'"):
                in_str, quote = True, ch
            elif ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        out.append((name, generic, text[start:j].strip()))
    return out


def split_args(args: str) -> tuple[list[str], dict[str, str]]:
    """按顶层逗号切分实参，同时收集 `name: value` 具名实参。"""
    parts, buf, depth, in_str, quote = [], [], 0, False, ""
    i = 0
    while i < len(args):
        ch = args[i]
        if in_str:
            buf.append(ch)
            if ch == "\\" and i + 1 < len(args):
                buf.append(args[i + 1])
                i += 2
                continue
            if ch == quote:
                in_str = False
        elif ch in ('"', "'"):
            in_str, quote = True, ch
            buf.append(ch)
        elif ch in "([{":
            depth += 1
            buf.append(ch)
        elif ch in ")]}":
            depth -= 1
            buf.append(ch)
        elif ch == "," and depth == 0:
            parts.append("".join(buf).strip())
            buf = []
        else:
            buf.append(ch)
        i += 1
    if "".join(buf).strip():
        parts.append("".join(buf).strip())
    named = {}
    for p in parts:
        m = re.match(r"^(\w+)\s*:\s*(.*)$", p, re.S)
        if m:
            named[m.group(1)] = m.group(2).strip()
    return parts, named


def collect_consts(text: str) -> dict[str, str]:
    """抓 const / static readonly 常量（值保留原文，交给 resolve_number 解析）。"""
    out = {}
    for m in re.finditer(
        r"\b(?:const|static\s+readonly)\s+[\w.<>]+\s+(\w+)\s*=\s*([^;]+);", text
    ):
        out[m.group(1)] = m.group(2).strip()
    return out


_POWER_CONSTS: dict[tuple[str, str], str] | None = None


def power_consts() -> dict[tuple[str, str], str]:
    """抓 Power 类里的常量（如 CinnabarBite.DefaultAmount=2 / UpgradeAmount=1）。"""
    global _POWER_CONSTS
    if _POWER_CONSTS is None:
        result = {}
        for p in sorted(POWERS_DIR.rglob("*.cs")):
            text = strip_comments(p.read_text(encoding="utf-8-sig"))
            m_cls = re.search(r"\bclass\s+(\w+)", text)
            if not m_cls:
                continue
            cls = m_cls.group(1)
            for k, v in collect_consts(text).items():
                result[(cls, k)] = v
        _POWER_CONSTS = result
    return _POWER_CONSTS


def resolve(expr: str, consts: dict[str, str], depth: int = 0):
    """把数值表达式解析成 int/float/str；解析不了返回 None。"""
    if expr is None or depth > 4:
        return None
    e = expr.strip()
    # 剥离 C# 强制类型转换前缀，如 (int)BasePercent。
    # 祖炁形态改用 WithPower((int)常量, 0) + OnUpgrade 小数升级后需要。
    m = re.fullmatch(r"\((?:int|decimal|float|double|long|short|byte)\)\s*(.+)", e, re.S)
    if m:
        return resolve(m.group(1).strip(), consts, depth + 1)
    # 常量之间的简单加减，如 UpgradeValueBy(UpgradedPercent - BasePercent)。
    m = re.fullmatch(r"(\w+)\s*([+-])\s*(\w+)", e)
    if m:
        a = resolve(m.group(1), consts, depth + 1)
        b = resolve(m.group(3), consts, depth + 1)
        if isinstance(a, (int, float)) and isinstance(b, (int, float)):
            return a + b if m.group(2) == "+" else a - b
    m = re.fullmatch(r"(-?\d+(?:\.\d+)?)[mMfFdDuUlL]?", e)
    if m:
        f = float(m.group(1))
        return int(f) if f.is_integer() else f
    m = re.fullmatch(r'"([^"]*)"', e)
    if m:
        return m.group(1)
    m = re.fullmatch(r"(?:Powers\.)?(\w+)\.(\w+)", e)
    if m:
        v = power_consts().get((m.group(1), m.group(2)))
        if v is not None:
            return resolve(v, consts, depth + 1)
    if e in consts:
        return resolve(consts[e], consts, depth + 1)
    return None


def as_number(v, default=0) -> int | float:
    return v if isinstance(v, (int, float)) else default


def set_var(vars_: dict, name: str, base, delta=0) -> None:
    """登记变量；同名后来者覆盖（与 C# 里后注册覆盖前注册一致）。"""
    b = as_number(base, None)
    if b is None:
        return
    d = as_number(delta, 0)
    vars_[name] = (b, b + d)


def sorted_keywords(kws: set[str]) -> list[str]:
    """按清单固定顺序排列；未收录的新关键词排后面、按字母序。"""
    return sorted(kws, key=lambda k: (KEYWORD_ORDER.index(k) if k in KEYWORD_ORDER else 99, k))


def keyword_text(kws: list[str]) -> str:
    """把关键词列表渲染成中文短句；空集合记为「—」。"""
    return "、".join(KEYWORD_ZHS.get(k, k) for k in kws) or "—"


def extract_keywords(text: str) -> tuple[list[str], list[str], list[str]]:
    """抓卡牌关键词在「基础 / 升级后」两态的集合，以及运行时给别的牌加词条的备注。

    BaseLib 语义由源码注释实证：
      WithKeywords(X) / WithKeyword(X)    → 基础与升级都带（QuickStep 的「消耗」）
      WithKeyword(X, UpgradeType.Add)     → 仅升级带（QuickStep 的「升级附加保留」）
      WithKeyword(X, UpgradeType.Remove)  → 仅基础带（DarkHumor「基础消耗，升级后去除消耗」、
                                            Purge「1 费，消耗」）
    被注释掉的声明（如 RebirthOne 里的 Innate）先由 strip_comments 去掉，不会误抓。
    """
    code = strip_comments(text)
    base: set[str] = set()
    up: set[str] = set()
    for _, _, argstr in scan_calls(code, {"WithKeyword", "WithKeywords"}):
        kws = re.findall(r"CardKeyword\.(\w+)", argstr)
        if not kws:
            continue
        um = re.search(r"UpgradeType\.(\w+)", argstr)
        mode = um.group(1) if um else "Both"
        for kw in kws:
            if mode in ("Both", "Remove"):
                base.add(kw)
            if mode in ("Both", "Add"):
                up.add(kw)

    hints: list[str] = []
    # AddKeyword / RemoveKeyword 作用于「其它牌」（拾遗给回收的牌加消耗），
    # 不是本卡固有词条，不能算进关键词列，只在备注里提示。
    for m in re.finditer(r"\b\w+\.AddKeyword\(\s*CardKeyword\.(\w+)\s*\)", code):
        hints.append(f"运行时给目标牌附加「{KEYWORD_ZHS.get(m.group(1), m.group(1))}」")
    for m in re.finditer(r"\b\w+\.RemoveKeyword\(\s*CardKeyword\.(\w+)\s*\)", code):
        hints.append(f"运行时移除目标牌的「{KEYWORD_ZHS.get(m.group(1), m.group(1))}」")
    return sorted_keywords(base), sorted_keywords(up), hints


def extract_vars(text: str) -> tuple[dict[str, tuple], int, list[str], list[str]]:
    """从卡牌源码解析 {变量: (基础值, 升级后值)}、费用升级增量、施加的 power 类、备注提示。"""
    code = strip_comments(text)
    consts = collect_consts(code)
    vars_: dict[str, tuple] = {}
    cost_delta = 0
    power_classes: list[str] = []
    hints: list[str] = []

    for name, generic, argstr in scan_calls(code):
        args, named = split_args(argstr)

        if name == "WithCostUpgradeBy":
            v = resolve(args[0], consts) if args else None
            if isinstance(v, (int, float)):
                cost_delta += int(v)
            continue

        if name in ("WithDamage", "WithBlock", "WithCards", "WithEnergy", "WithHeal"):
            var = {"WithDamage": "Damage", "WithBlock": "Block", "WithCards": "Cards",
                   "WithEnergy": "Energy", "WithHeal": "Heal"}[name]
            base = resolve(args[0], consts) if len(args) > 0 else None
            delta = resolve(args[1], consts) if len(args) > 1 else 0
            set_var(vars_, var, base, delta)
            continue

        if name in ("WithVar", "WithVars"):
            for a in args:
                m = re.search(r"new\s+(\w+)\s*\(([^)]*)\)", a)
                if m:
                    vtype = m.group(1)
                    var = vtype[:-3] if vtype.endswith("Var") else vtype
                    inner, _ = split_args(m.group(2))
                    base = resolve(inner[0], consts) if inner else None
                    up = re.search(r"\.WithUpgrade\(([^)]*)\)", a)
                    delta = resolve(up.group(1), consts) if up else 0
                    set_var(vars_, var, base, delta)
                else:
                    v = resolve(a, consts)
                    if isinstance(v, str) and len(args) >= 2:
                        base = resolve(args[1], consts)
                        delta = resolve(args[2], consts) if len(args) > 2 else 0
                        set_var(vars_, v, base, delta)
            continue

        if name == "WithPower":
            pcls = generic.split(".")[-1] if generic else None
            if args and isinstance(resolve(args[0], consts), str):
                # 带别名的重载：WithPower<T>("QiLoss", base, delta)。
                # 别名只是卡面显示用的变量，不代表真的施加这个 power，故不计入 power_classes。
                alias = resolve(args[0], consts)
                base = resolve(args[1], consts) if len(args) > 1 else None
                delta = resolve(args[2], consts) if len(args) > 2 else 0
                set_var(vars_, alias, base, delta)
            elif pcls:
                base = resolve(args[0], consts) if len(args) > 0 else None
                delta = resolve(args[1], consts) if len(args) > 1 else 0
                set_var(vars_, pcls, base, delta)
                # 负数层（如掌心雷的 WithPower<GoldenAegis>(-1)）是消耗用的显示变量，不算施加
                if isinstance(base, (int, float)) and base > 0 and pcls not in power_classes:
                    power_classes.append(pcls)
            continue

        if name == "WithCalculatedDamage":
            if args and isinstance(resolve(args[0], consts), str):
                # 具名重载：WithCalculatedDamage("Bonus", base, calc, prop, upgrade, bonusUpgrade)
                var = resolve(args[0], consts)
                base = resolve(args[1], consts) if len(args) > 1 else None
                delta = resolve(named.get("upgrade", args[4] if len(args) > 4 else "0"), consts)
                set_var(vars_, var, base, delta)
            else:
                # 双变量重载：CalculatedDamage（基础伤害）+ ExtraDamage（每档加成）
                base = resolve(args[0], consts) if len(args) > 0 else None
                per = resolve(args[1], consts) if len(args) > 1 else None
                up = resolve(named.get("upgrade", args[3] if len(args) > 3 else "0"), consts)
                bup = resolve(named.get("bonusUpgrade", args[4] if len(args) > 4 else "0"), consts)
                set_var(vars_, "CalculatedDamage", base, up)
                set_var(vars_, "ExtraDamage", per, bup)
            continue

        if name == "WithCalculatedVar":
            var = resolve(args[0], consts) if args else None
            if isinstance(var, str):
                base = resolve(args[1], consts) if len(args) > 1 else None
                set_var(vars_, var, base, 0)
            continue

    # 少数卡在 OnUpgrade 里用 UpgradeValueBy 改数值（如蛇花咬的 PetDamage）
    for m in re.finditer(r'DynamicVars\["(\w+)"\]\.UpgradeValueBy\(([^)]*)\)', code):
        var, expr = m.group(1), m.group(2)
        v = resolve(expr, consts)
        if var in vars_ and isinstance(v, (int, float)):
            b, u = vars_[var]
            vars_[var] = (b, u + v)

    # OnPlay 里直接 PowerCmd.Apply / CommonActions.Apply 施加的 power（没有 WithPower 变量）
    for m in re.finditer(
        r"(?:PowerCmd\.Apply|CommonActions\.Apply(?:Self)?)\s*<([\w.]+)>", code
    ):
        pcls = m.group(1).split(".")[-1]
        if pcls not in power_classes:
            power_classes.append(pcls)

    # 关键词（含基础态）改由 extract_keywords 统一处理，这里不再生成重复的备注提示。
    return vars_, cost_delta, power_classes, hints


def parse_card_file(path: Path) -> dict | None:
    """解析单个卡牌源文件；非卡牌文件返回 None。"""
    text = path.read_text(encoding="utf-8-sig")

    # 抽象类/基类跳过（yylCardModel、yyl_sts2_modCard 等）
    if re.search(r"\babstract\s+class\b", text):
        return None

    m_class = re.search(r"\b(?:sealed\s+|public\s+)*class\s+(\w+)", text)
    if not m_class:
        return None
    cls = m_class.group(1)

    # 无参构造: public Xxx() : this(N, CardType.T, CardRarity.R, TargetType.G)
    m_ctor = re.search(
        r"public\s+" + re.escape(cls) + r"\(\)\s*:\s*this\(([^)]*)\)", text)
    if not m_ctor:
        return None
    args = [a.strip() for a in m_ctor.group(1).split(",")]
    if len(args) < 4:
        return None

    cost = parse_cost(args[0])
    m_type = re.search(r"CardType\.(\w+)", args[1])
    m_rarity = re.search(r"CardRarity\.(\w+)", args[2])
    m_target = re.search(r"TargetType\.(\w+)", args[3])
    if not (m_type and m_rarity and m_target):
        return None

    # X 费确认（防只传 MinValue 没点亮的情况）
    is_x = cost == "X" and bool(
        re.search(r"override\s+bool\s+HasEnergyCostX\s*=>\s*true", text))

    # 多人限定
    mp_only = bool(re.search(
        r"MultiplayerConstraint\s*=>\s*CardMultiplayerConstraint\.MultiplayerOnly", text))

    # 不可升级（状态 / 任务牌常见：MaxUpgradeLevel => 0），升级后效果列不应写「同基础」
    no_upgrade = bool(re.search(r"override\s+int\s+MaxUpgradeLevel\s*=>\s*0\b", text))

    # 额外可打出条件（override IsPlayable）：本地化描述里不写，清单在备注给出条件表达式
    m_play = re.search(r"override\s+bool\s+IsPlayable\s*=>\s*([^;]+);", text, re.S)
    playable_cond = re.sub(r"\s+", " ", m_play.group(1)).strip() if m_play else None

    # 先古转化: ITranscendenceCard 实现（vanilla 接口，由"古老牙齿"遗物消费）
    trans_to = None
    m_ret = re.search(
        r"GetTranscendenceTransformedCard\s*\(\)\s*{[^}]*?return\s+ModelDb\.Card<(\w+)>\(\)",
        text, re.S)
    if m_ret:
        trans_to = m_ret.group(1)

    vars_, cost_delta, power_classes, hints = extract_vars(text)
    kw_base, kw_up, kw_hints = extract_keywords(text)
    hints = hints + kw_hints

    return {
        "class": cls,
        "cost": "X" if is_x else cost,
        "type": m_type.group(1),
        "rarity": m_rarity.group(1),
        "target": m_target.group(1),
        "mp_only": mp_only,
        "no_upgrade": no_upgrade,
        "playable_cond": playable_cond,
        "transcends_to": trans_to,
        "trans_iface": "ITranscendenceCard" in text,
        "vars": vars_,
        "cost_delta": cost_delta,
        "power_classes": power_classes,
        "kw_base": kw_base,
        "kw_up": kw_up,
        "hints": hints,
        "file": str(path.relative_to(PROJECT_ROOT)).replace("\\", "/"),
    }


def parse_starting_deck(path: Path) -> set:
    """从角色类抓取初始牌组（StartingDeck 列表里的 Card<类名>）。"""
    if not path.exists():
        return set()
    text = path.read_text(encoding="utf-8-sig")
    m = re.search(r"StartingDeck\s*=>\s*\[(.*?)\];", text, re.S)
    if not m:
        return set()
    return set(re.findall(r"ModelDb\.Card<(\w+)>\(\)", m.group(1)))


def fmt(v) -> str:
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return str(v)


def render_desc(template: str, vars_: dict, upgraded: bool, warns: list[str], cls: str) -> str:
    """把卡牌描述模板渲染成带实际数值的纯文本；返回 (文本, 是否含动态数值段)。
    upgraded=True 时变化的数字加粗。"""
    s = RE_BBCODE.sub("", template)

    # 引擎卡面里只在战斗中显示的动态数值段：静态清单里保留说明，不写死会误导的 0
    dynamic = bool(RE_INCOMBAT.search(s))
    s = RE_INCOMBAT.sub("（战斗中显示实时数值）", s)

    s = RE_IFUPGRADED.sub(
        lambda m: ("**" + m.group(1) + "**") if upgraded else "", s)
    s = RE_ENERGYPREFIX.sub("能量", s)

    def diff(m):
        name = m.group(1)
        if name not in vars_:
            warns.append(f"[未解析变量] {cls}: {{{name}:diff()}}")
            return "?"
        b, u = vars_[name]
        val = u if upgraded else b
        txt = fmt(val)
        if upgraded and u != b:
            txt = "**" + txt + "**"
        return txt

    s = RE_DIFF.sub(diff, s)

    def energy(m):
        name = m.group(1)
        if name not in vars_:
            warns.append(f"[未解析变量] {cls}: {{{name}:energyIcons()}}")
            return "?点能量"
        b, u = vars_[name]
        val = u if upgraded else b
        num = fmt(val)
        if upgraded and u != b:
            num = "**" + num + "**"
        return f"{num}点能量"

    s = RE_ENERGY.sub(energy, s)

    # eng 的 {Var:plural:a|b} 等；zhs 用不到，统一保留提示
    def leftover(m):
        warns.append(f"[未处理占位符] {cls}: {m.group(0)[:40]}")
        return "?"

    s = RE_LEFTOVER.sub(leftover, s)

    s = s.replace("//n", "").replace("\\n", " ")
    s = s.replace("\r", "").replace("\n", " ")
    s = re.sub(r"\s{2,}", " ", s).strip()
    return s, dynamic


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def build_power_section(cards: list[dict], zhs_p: dict, eng_p: dict) -> list[str]:
    """Power 附录：中文名 / 英文 / 说明 / 施加它的卡 / 实现类。"""
    appliers: dict[str, list[str]] = defaultdict(list)
    for c in cards:
        for pcls in c["power_classes"]:
            appliers[pascal_to_upper_snake(pcls)].append(c["title_zhs"] or c["class"])

    # 逆生三态不是 PowerCmd.Apply 施加的，而是 StanceCmd.EnterReverseLifeN；
    # 单独补上它们与「逆生」总词条的关联。
    stance_ids = {
        "1": ["REBIRTH_STANCE_ONE", "REBIRTH_STANCE_ONE_POWER", "INVERSE_LIFE"],
        "2": ["REBIRTH_STANCE_TWO", "INVERSE_LIFE"],
        "3": ["REBIRTH_STANCE_THREE", "INVERSE_LIFE"],
    }
    for c in cards:
        src = PROJECT_ROOT / c["file"]
        if not src.exists():
            continue
        text = strip_comments(src.read_text(encoding="utf-8-sig"))
        for m in re.finditer(r"StanceCmd\.EnterReverseLife(\d)", text):
            for pid in stance_ids.get(m.group(1), []):
                appliers[pid].append(c["title_zhs"] or c["class"])

    class_by_id = {}
    for p in sorted(POWERS_DIR.rglob("*.cs")):
        text = p.read_text(encoding="utf-8-sig")
        m = re.search(r"\bclass\s+(\w+)", text)
        if m:
            class_by_id[pascal_to_upper_snake(m.group(1))] = f"Code/Powers/{p.name}"

    lines = [
        "## 附：Power（能力 / 状态）说明",
        "",
        "> 数据来自 `localization/{zhs,eng}/powers.json` 的 title / description / smartDescription，",
        "> 以及 `Code/Powers/**/*.cs`（实现类）。",
        "> 说明里的 `{Amount}` 是该 power 当前的层数，由游戏运行时替换；",
        "> `{Block:diff()}` 一类变量由施加它的卡在运行时设定（如「积砖」由磨镜成砖设为 8 → 12）。",
        "> 「关联卡牌」只列本清单收录的卡（雪藏卡不计入）；「简版提示」为 smartDescription，与说明仅标点不同时留空。",
        "",
        "| Power | 英文 | 说明 | 简版提示 | 关联卡牌 | 实现类 |",
        "|---|---|---|---|---|---|",
    ]
    ids = sorted(k[len(MOD_PREFIX) + 1:-len(".title")]
                 for k in zhs_p if k.endswith(".title"))
    for pid in ids:
        prefix = f"{MOD_PREFIX}-{pid}"
        name = zhs_p.get(prefix + ".title", "")
        eng = eng_p.get(prefix + ".title", "")
        desc = clean_desc(zhs_p.get(prefix + ".description", "")) or "—"
        smart = clean_desc(zhs_p.get(prefix + ".smartDescription", ""))
        # 忽略标点差异，只有真正多出信息时才单列
        norm = lambda s: re.sub(r"[\s，。：、；！？,.:;!?]", "", s)
        a, b = norm(desc), norm(smart)
        smart_disp = smart if b and a != b and b not in a and a not in b else "—"
        linked = "、".join(dict.fromkeys(appliers.get(pid, []))) or "—"
        impl = class_by_id.get(pid, "—（无同名实现类）")
        impl_disp = f"`{impl}`" if impl.startswith("Code/") else impl
        english = eng or "⚠ eng 缺"
        lines.append(
            f"| **{name}** | {english} | {desc} | {smart_disp} | {linked} | {impl_disp} |"
        )
    lines.append("")
    lines.append(f"**共 {len(ids)} 条 Power 词条。**")
    lines.append("")
    return lines


def main() -> None:
    zhs = load_json(ZHS_JSON)
    eng = load_json(ENG_JSON)
    zhs_p = load_json(ZHS_POWERS_JSON)
    eng_p = load_json(ENG_POWERS_JSON)
    starting_deck = parse_starting_deck(CHARACTER_CS)

    cards: list[dict] = []
    for path in sorted(CARDS_DIR.rglob("*.cs")):
        info = parse_card_file(path)
        if info:
            cards.append(info)

    # 本地化匹配（类名 -> UPPER_SNAKE -> json 键）
    warns: list[str] = []
    for c in cards:
        key_id = pascal_to_upper_snake(c["class"])
        k_title = f"{MOD_PREFIX}-{key_id}.title"
        k_desc = f"{MOD_PREFIX}-{key_id}.description"
        c["title_zhs"] = zhs.get(k_title, "")
        c["desc_template"] = zhs.get(k_desc, "")
        c["title_eng"] = eng.get(k_title, "")
        if not c["title_zhs"]:
            warns.append(f"[缺本地化] {c['class']} -> {k_title}")

    # 反向核对: json 有 title 但没有对应类
    known_ids = {pascal_to_upper_snake(c["class"]) for c in cards}
    for k in zhs:
        if k.endswith(".title"):
            key_id = k[: -len(".title")].removeprefix(f"{MOD_PREFIX}-")
            if key_id not in known_ids:
                warns.append(f"[键无类] {k}")

    # 雪藏 = Basic 且不在初始牌组；本清单不收录
    hidden = [c for c in cards if c["rarity"] == "Basic" and c["class"] not in starting_deck]
    visible = [c for c in cards if c not in hidden]

    # 渲染两套描述
    for c in visible:
        tpl = c["desc_template"]
        c["desc_base"], c["dynamic"] = render_desc(tpl, c["vars"], False, warns, c["class"])
        # 升级渲染复用同一份 warns，去重在输出前做
        c["desc_up"], _ = render_desc(tpl, c["vars"], True, [], c["class"])

    # 排序: 稀有度 -> 中文卡名
    visible.sort(key=lambda c: (
        RARITY_ORDER.index(c["rarity"]) if c["rarity"] in RARITY_ORDER else 99,
        c["title_zhs"] or c["class"],
    ))

    # ------------------------------------------------ 生成 Markdown
    lines = []
    lines.append("# 伊林卡牌清单（自动生成）")
    lines.append("")
    lines.append("> 由 `tools/gen_card_list.py` 自动抓取生成，**勿手工编辑**——卡池或数值变动后重新运行：")
    lines.append("> `\"C:/Users/Peter_Tan/.workbuddy/binaries/python/versions/3.13.12/python.exe\" tools/gen_card_list.py`")
    lines.append(">")
    lines.append("> 数据来源：`Code/Cards/**/*.cs`（稀有度 / 费用 / 类型 / 目标 / 数值变量 / 升级增量）、")
    lines.append("> `Code/Powers/**/*.cs`（power 常量）、`localization/zhs/cards.json`（卡名 / 描述模板）、")
    lines.append("> `localization/eng/cards.json`（英文卡名）、`localization/{zhs,eng}/powers.json`（Power 附录）。")
    lines.append(">")
    lines.append("> **读法**：「基础效果」「升级后效果」两列都已把 `{X:diff()}` 等占位符换成**源码里的实际数值**；")
    lines.append("> 升级列中用**加粗**标出发生变化的部分；两列文字相同记为「同基础」，")
    lines.append("> `MaxUpgradeLevel = 0` 的牌记为「不可升级」。")
    lines.append("> 费用列 `1 → 0` 表示升级后费用变化。")
    lines.append("> 「关键词」列抓自源码 `WithKeyword(s)`（消耗 / 保留 / 固有 / 虚无 / 不可打出）——")
    lines.append("> 引擎会自动把这些词条追加到游戏卡面，本地化描述模板里不写，所以清单单列；")
    lines.append("> `消耗 → —` 表示升级移除该词条，`— → 保留` 表示升级附加，`消耗` 表示两态都有。")
    lines.append("> 描述中的「消耗 1 层金光护体」等是资源消耗，不是这里的「消耗」词条。")
    lines.append("> 「（战斗中显示实时数值）」对应卡面里只在战斗内出现的动态数值段，静态清单不写死它。")
    lines.append(f"> **本清单不含雪藏牌**（Basic 且不在 StartingDeck，共 {len(hidden)} 张）。")
    lines.append("")

    total = 0
    for rarity in RARITY_ORDER:
        group = [c for c in visible if c["rarity"] == rarity]
        if not group:
            continue
        total += len(group)
        lines.append(f"## {RARITY_ZHS.get(rarity, rarity)} {rarity}（{len(group)}）")
        lines.append("")
        lines.append("| 卡名 | 费用 | 类型 | 目标 | 关键词 | 基础效果 | 升级后效果 | 备注 |")
        lines.append("|---|---|---|---|---|---|---|---|")
        for c in group:
            notes = []
            if c["rarity"] == "Basic" and c["class"] in starting_deck:
                notes.append("初始牌组")
            if c["mp_only"]:
                notes.append("联机限定")
            if c["no_upgrade"]:
                notes.append("不可升级")
            if c["playable_cond"]:
                notes.append(f"可打出条件 `{c['playable_cond']}`")
            if c["transcends_to"]:
                notes.append(f"经古老牙齿先古转化 → {c['transcends_to']}")
            if c["dynamic"]:
                notes.append("含动态数值")
            notes.extend(c["hints"])
            if c["cost_delta"] and c["cost"] == "0":
                notes.append("升级费用 −1（已为 0）")

            cost_disp = ("—" if (c["rarity"] in ("Quest", "Status", "Curse")
                                 and not c["cost"].isdigit()) else c["cost"])
            if c["cost"].isdigit() and c["cost_delta"]:
                up = max(0, int(c["cost"]) + c["cost_delta"])
                if up != int(c["cost"]):
                    cost_disp = f"{c['cost']} → {up}"

            if c["kw_base"] == c["kw_up"]:
                kw_disp = keyword_text(c["kw_base"])
            else:
                kw_disp = f"{keyword_text(c['kw_base'])} → {keyword_text(c['kw_up'])}"

            up_desc = c["desc_up"]
            if c["no_upgrade"]:
                up_desc = "不可升级"
            elif up_desc == c["desc_base"]:
                up_desc = "同基础"
            lines.append(
                f"| **{c['title_zhs'] or '⚠' + c['class']}** | {cost_disp} "
                f"| {TYPE_ZHS.get(c['type'], c['type'])} "
                f"| {TARGET_ZHS.get(c['target'], c['target'])} "
                f"| {kw_disp} "
                f"| {c['desc_base']} | {up_desc} "
                f"| {'、'.join(notes)} |")
        lines.append("")

    stats = ", ".join(
        f"{RARITY_ZHS.get(r, r)} {len([c for c in visible if c['rarity'] == r])}"
        for r in RARITY_ORDER if any(c["rarity"] == r for c in visible))
    lines.append(f"**本清单共 {total} 张**（{stats}）；另有雪藏牌 {len(hidden)} 张未收录。")
    lines.append("")

    kw_all = sorted(
        {k for c in visible for k in c["kw_base"] + c["kw_up"]},
        key=lambda k: (KEYWORD_ORDER.index(k) if k in KEYWORD_ORDER else 99, k),
    )
    if kw_all:
        lines.append("## 附：关键词统计")
        lines.append("")
        lines.append("> 基础 / 升级后两态分别计数；「变化」列是升级导致的携带张数增减。")
        lines.append("> 无关键词的卡不计入本表；雪藏牌不参与统计。")
        lines.append("")
        lines.append("| 关键词 | 基础携带 | 升级后携带 | 变化 |")
        lines.append("|---|---|---|---|")
        for k in kw_all:
            nb = sum(k in c["kw_base"] for c in visible)
            nu = sum(k in c["kw_up"] for c in visible)
            if nb == nu:
                change = "—"
            elif nu > nb:
                change = f"升级附加 {nu - nb} 张"
            else:
                change = f"升级移除 {nb - nu} 张"
            lines.append(f"| {KEYWORD_ZHS.get(k, k)} | {nb} | {nu} | {change} |")
        lines.append("")

    # 类名 ↔ 英文卡名对照（同样排除雪藏）
    lines.append("## 附：类名 ↔ 英文卡名对照")
    lines.append("")
    lines.append("| 类名 | 中文 | 英文卡名 | 稀有度 |")
    lines.append("|---|---|---|---|")
    for c in visible:
        lines.append(f"| {c['class']} | {c['title_zhs']} | {c['title_eng'] or '⚠缺失'} | {c['rarity']} |")
    lines.append("")

    lines += build_power_section(visible, zhs_p, eng_p)

    OUTPUT.write_text("\n".join(lines), encoding="utf-8")

    # ------------------------------------------------ 控制台报告
    unique_warns = list(dict.fromkeys(warns))
    print(f"已生成 {OUTPUT.name}：收录 {total} 张，排除雪藏 {len(hidden)} 张（全量 {len(cards)} 张）")
    print("统计:", stats)
    if kw_all:
        print("关键词: " + ", ".join(
            f"{KEYWORD_ZHS.get(k, k)} 基础{sum(k in c['kw_base'] for c in visible)}"
            f"/升级{sum(k in c['kw_up'] for c in visible)}" for k in kw_all))
    print("排除的雪藏牌:", "、".join(c["title_zhs"] or c["class"] for c in hidden))
    if unique_warns:
        print(f"\n⚠ 告警 {len(unique_warns)} 条:")
        for w in unique_warns:
            print("  " + w)
    else:
        print("双向核对通过：类 ↔ 本地化键一一对应，全部数值占位符已解析，无告警。")


if __name__ == "__main__":
    sys.exit(main())
