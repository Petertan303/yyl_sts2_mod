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
        // ---- 本回合获得过炁 (伪 0 费) ----
        [typeof(BurstFist)] = c => QiGainTracker.GainedThisRound(
            c.Owner, c.Owner.Creature.CombatState),
        // ---- 耗炁卡 (炁量达到卡面 QiLoss 才有效果) ----
        [typeof(TaiChi)] = HasEnoughQi,
        [typeof(AcupointShift)] = HasEnoughQi,
        [typeof(FreeFlow)] = HasEnoughQi,
        [typeof(QiDispersal)] = HasEnoughQi,
        // ---- 炁量 > 0 ----
        // ⚠ 炁冲**没有**声明 QiLoss 变量 (它只在有炁时补一段, 不是耗炁卡),
        //    之前把它注册成 HasEnoughQi 会在抽牌瞬间读 DynamicVars["QiLoss"] 抛
        //    KeyNotFoundException, 直接冻死游戏 (2026-09-22 修复)。
        [typeof(QiBurst)] = c => GetQi(c) > 0,
        [typeof(SkyFire)] = c => GetQi(c) > 0,
    };

    public static bool ShouldGlow(CardModel? card)
    {
        if (card == null || card.Owner == null) return false;
        return Conditions.TryGetValue(card.GetType(), out var fn) && fn(card);
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

    private static int GetQi(CardModel c) => c.Owner.Creature.GetPower<Qi>()?.Amount ?? 0;

    /// <summary>炁量达到卡面声明的 QiLoss (随升级变化) 才有额外效果。</summary>
    private static bool HasEnoughQi(CardModel c)
    {
        // ★防御: 卡没声明 QiLoss 时绝不能抛异常 —— 本方法跑在抽牌/出牌的事件钩子里,
        //   任何未捕获异常都会让战斗流程卡死 (炁冲曾因此冻结整个游戏)。
        if (!c.DynamicVars.TryGetValue("QiLoss", out var qiLoss)) return false;
        return GetQi(c) >= qiLoss.IntValue;
    }
}
