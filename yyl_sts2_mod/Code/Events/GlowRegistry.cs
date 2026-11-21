using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using yyl_sts2_mod.Code.Cards.Ancient;
using yyl_sts2_mod.Code.Cards.Basic;
using yyl_sts2_mod.Code.Cards.Common;
using yyl_sts2_mod.Code.Cards.Rare;
using yyl_sts2_mod.Code.Cards.Uncommon;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     "条件满足 → 手牌卡牌金光"的注册表与刷新器。
///     <para>
///         条件按卡牌类型注册 (满足 <c>Func&lt;CardModel, bool&gt;</c> 时该卡在手中发光);
///         刷新由 <see cref="CardGlowDriver" /> 的事件钩子驱动, 遍历场景里的
///         <c>NCard</c> 节点, 对手牌区 (<c>DisplayingPile == Hand</c>) 的卡
///         开/关引擎自带的高亮节点 <c>NCard.CardHighlight</c> (AnimShow/AnimHide)。
///         用 Godot Meta 记录上次状态, 避免每帧重复触发动画。
///     </para>
/// </summary>
public static class GlowRegistry
{
    private static readonly Dictionary<Type, Func<CardModel, bool>> Conditions = new()
    {
        // ---- 金光护体联动 (有金光 → 额外伤害/格挡) ----
        [typeof(PalmThunder)] = HasAegis,
        [typeof(WhiteWorm)] = HasAegis,
        [typeof(FiveThunderLaw)] = HasAegis,
        [typeof(RisingPalm)] = HasAegis,
        [typeof(CloseGate)] = HasAegis,
        // 返照: 后半段「消耗全部金光 → 2 倍回流」只在有金光护体时触发, 与上面同款条件金光。
        [typeof(BaoShan)] = HasAegis,
        // 站桩: 后半段格挡要消耗 2 层金光 (AegisCost=2), 门槛比上面几张的 1 层高。
        //   ★虽在 Basic 雪藏池不出, 仍按语义注册, 保持"有金光额外效果→发光"一致。
        [typeof(StandingPost)] = c => HasEnoughAegis(c, "AegisCost"),
        // ---- 本回合获得过炁 (伪 0 费) ----
        [typeof(BurstFist)] = c => QiGainTracker.GainedThisRound(
            c.Owner, c.Owner.Creature.CombatState),
        // ---- 耗炁卡 (炁量达到卡面 QiLoss 才有额外效果) ----
        [typeof(TaiChi)] = HasEnoughQi,
        [typeof(FreeFlow)] = HasEnoughQi,
        [typeof(QiDispersal)] = HasEnoughQi,
        // 移穴: 格挡无条件给, 炁够 2 点才额外从抽牌堆选 1 张 → "有炁有额外效果", 炁足发金光。
        //   ★2026-11-21 用户定调恢复: 之前误判为"不该亮"而移除; 按用户口径这类牌就该提示额外收益。
        [typeof(AcupointShift)] = HasEnoughQi,
        // 占卜: 0 费预视始终生效, 炁够 1 点才额外抽牌 → 同上, 炁足发金光。
        [typeof(Divination)] = HasEnoughQi,
        // ---- 炁量 > 0 (有炁就有额外效果) ----
        // ⚠ 炁冲**没有**声明 QiLoss 变量 (它只在有炁时补一段, 不是耗炁卡),
        //    之前把它注册成 HasEnoughQi 会在抽牌瞬间读 DynamicVars["QiLoss"] 抛
        //    KeyNotFoundException, 直接冻死游戏 (2026-09-22 修复)。
        [typeof(QiBurst)] = c => GetQi(c) > 0,
        [typeof(SkyFire)] = c => GetQi(c) > 0,
        // ---- 场上存在奶龙 (指认: 已带标记 → 产能量; 未带 → 补标记) ----
        // ★用 AnyNailong 而非「目标是否带标记」: 指认可以对任意目标打出,
        //   发光只提示"场上有没有奶龙"这个前提, 与最终选谁无关。
        [typeof(Identify)] = AnyNailongOnField,
    };

    /// <summary>
    ///     ★红色警示高亮 (2026-11-21): "能打出但炁不足 → 打出去完全没效果"的卡。
    ///     <para>
    ///         与 <see cref="Conditions" /> 的金光(条件满足才亮, 正面提示)语义相反:
    ///         这里<b>条件不满足(没炁)才亮</b>, 走引擎原生 <c>CardModel.ShouldGlowRed</c> →
    ///         <c>NCardHighlight.Modulate = red</c>。引擎已用 CanPlay() 守卫, 不能打出的牌不会亮。
    ///         红优先级高于金 (引擎先判红再判金), 所以这些卡有炁时不亮红、无炁时亮红, 互斥干净。
    ///     </para>
    ///     <para>
    ///         只放"无炁 = 整个效果全无"的卡。"有炁只是多段/额外效果"的牌 (炁冲/移穴/占卜等)
    ///         绝不能进这里 —— 那样会把本来有效的牌误标成警示。
    ///     </para>
    /// </summary>
    private static readonly Dictionary<Type, Func<CardModel, bool>> RedConditions = new()
    {
        [typeof(TaiChi)] = NotEnoughQi,        // 太极: 无炁 → 印记不施加
        [typeof(FreeFlow)] = NotEnoughQi,      // 通畅: 无炁 → 不抽牌
        [typeof(QiDispersal)] = NotEnoughQi,   // 散炁: 无炁 → 不给能量
        [typeof(SkyFire)] = c => GetQi(c) <= 0, // 天火: 无炁 → 完全无伤害 (它用 GetQi>0 而非 QiLoss)
        // 食炁用 QiCost 变量 (非 QiLoss), 单列判据
        [typeof(ShiQi)] = c => !HasVarEnough(c, "QiCost"),
        // 炁化金光用 QiLoss=1, 但效果含格挡/护体, 无炁则全无
        [typeof(QiTransmute)] = NotEnoughQi,
    };

    public static bool ShouldGlow(CardModel? card)
    {
        if (card == null || card.Owner == null) return false;
        return Conditions.TryGetValue(card.GetType(), out var fn) && fn(card);
    }

    public static bool ShouldGlowRed(CardModel? card)
    {
        if (card == null || card.Owner == null) return false;
        return RedConditions.TryGetValue(card.GetType(), out var fn) && fn(card);
    }

    /// <summary>重算当前场景中所有手牌 NCard 的金光状态 (事件驱动, 非每帧)。</summary>
    public static void Refresh()
    {
        var root = (Godot.Engine.GetMainLoop() as Godot.SceneTree)?.Root;
        if (root == null) return;

        foreach (var nc in EnumerateNCards(root))
        {
            // 只处理手牌区的卡 (其它区域的卡不参与条件发光)。
            if (nc.DisplayingPile != PileType.Hand) continue;
            var model = nc.Model as CardModel;
            var want = ShouldGlow(model);
            var last = nc.GetMeta("yyl_glow", false).AsBool();
            if (want == last) continue;

            nc.SetMeta("yyl_glow", want);
            var highlight = nc.CardHighlight;
            if (highlight == null) continue;
            if (want) highlight.AnimShow();
            else highlight.AnimHide();
        }
    }

    private static IEnumerable<NCard> EnumerateNCards(Godot.Node node)
    {
        if (node is NCard card) yield return card;
        foreach (var child in node.GetChildren())
        foreach (var found in EnumerateNCards(child))
            yield return found;
    }

    // ---- 条件实现 ----

    private static bool HasAegis(CardModel c) => c.Owner.Creature.HasPower<GoldenAegis>();

    /// <summary>金光层数达到卡面声明的花费 (如站桩 AegisCost=2) 才有额外效果。防御式。</summary>
    private static bool HasEnoughAegis(CardModel c, string varName)
    {
        if (c.Owner?.Creature == null) return false;
        if (!c.DynamicVars.TryGetValue(varName, out var cost)) return false;
        return (c.Owner.Creature.GetPower<GoldenAegis>()?.Amount ?? 0m) >= cost.IntValue;
    }

    private static int GetQi(CardModel c) => c.Owner.Creature.GetPower<Qi>()?.Amount ?? 0;

    /// <summary>
    ///     场上是否存在奶龙(敌人中存在即可 —— 含奶龙标记与心魔两种)。
    ///     <para>
    ///         ⚠ 防御式实现: 本方法跑在抽牌/出牌的事件钩子里, **绝不能抛异常**
    ///         (未捕获异常会冻结整场战斗)。因此全程 TryGetValue + 逐项筛选,
    ///         战斗状态缺失/creature 被移除时一律当作"无奶龙"(不发光), 不抛。
    ///     </para>
    /// </summary>
    private static bool AnyNailongOnField(CardModel c)
    {
        try
        {
            var combatState = c.Owner.Creature.CombatState;
            if (combatState == null) return false;
            foreach (var enemy in combatState.HittableEnemies)
                if (yylNailong.IsNailongMarked(enemy))
                    return true;
            return false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"GlowRegistry.AnyNailongOnField: {ex.Message}");
            return false;
        }
    }

    /// <summary>炁量达到卡面声明的 QiLoss (随升级变化) 才有额外效果。</summary>
    private static bool HasEnoughQi(CardModel c)
    {
        // ★防御: 卡没声明 QiLoss 时绝不能抛异常 —— 本方法跑在抽牌/出牌的事件钩子里,
        //   任何未捕获异常都会让战斗流程卡死 (炁冲曾因此冻结整个游戏)。
        if (!c.DynamicVars.TryGetValue("QiLoss", out var qiLoss)) return false;
        return GetQi(c) >= qiLoss.IntValue;
    }

    /// <summary>红色警示条件: 炁不足以支付卡面 QiLoss → 这张牌打出去完全无效。</summary>
    private static bool NotEnoughQi(CardModel c)
    {
        // ★防御: 缺变量/ owner 为空一律当作"不亮"(false), 绝不抛异常冻战斗。
        if (c.Owner?.Creature == null) return false;
        if (!c.DynamicVars.TryGetValue("QiLoss", out var qiLoss)) return false;
        return GetQi(c) < qiLoss.IntValue;
    }

    /// <summary>红色警示条件: 自定义变量名(如食炁的 QiCost)的炁花费不足。</summary>
    private static bool HasVarEnough(CardModel c, string varName)
    {
        if (c.Owner?.Creature == null) return false;
        if (!c.DynamicVars.TryGetValue(varName, out var cost)) return false;
        return GetQi(c) >= cost.IntValue;
    }
}
