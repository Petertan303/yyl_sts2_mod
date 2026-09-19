using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     运行时调整单张卡牌费用的工具。
///     <para>
///         ⚠ 卡牌的运行时费用走 <see cref="CardModel.EnergyCost" /> (<see cref="CardEnergyCost"/>)。
///         不要碰 <c>StarCost</c>/<c>AddTemporaryStarCost</c> —— 那是另一套(星费)系统,
///         对能量卡完全无效 (v0.111.0 实测踩坑)。
///         <c>CardEnergyCost</c> 的公开 API 与原版文案一一对应:
///         <c>SetUntilPlayed(0, true)</c> 即"弃牌堆中卡的费用变为 0 直到打出"。
///         第二个 bool 是 IsReduceOnly: 该修正只在会**降低**当前费用时才计入。
///     </para>
/// </summary>
public static class CardCostUtil
{
    /// <summary>本场战斗内把该牌费用设为 <paramref name="cost" /> (只降不升)。</summary>
    public static void SetCostThisCombat(CardModel card, int cost)
    {
        card?.EnergyCost.SetThisCombat(cost, true);
    }

    /// <summary>把该牌费用设为 <paramref name="cost" />, 打出后清除 (配合消耗词条即"免费一次性")。</summary>
    public static void SetCostUntilPlayed(CardModel card, int cost)
    {
        card?.EnergyCost.SetUntilPlayed(cost, true);
    }

    /// <summary>本场战斗内该牌费用增减 <paramref name="delta" /> (负数=降费, 只降不升; 正数=升费)。</summary>
    public static void AddCostThisCombat(CardModel card, int delta)
    {
        card?.EnergyCost.AddThisCombat(delta, delta < 0);
    }
}
