using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     运行时调整单张卡牌费用的工具。
///     <para>
///         <see cref="CardModel.AddTemporaryStarCost" /> 是私有方法 (公开面只有
///         <c>UpgradeStarCostBy</c>), 只能反射调用;
///         <see cref="TemporaryCardCost.ThisCombat(int)" /> 是公开工厂,
///         语义为"本场战斗内把该牌费用设为指定值"。
///     </para>
/// </summary>
public static class CardCostUtil
{
    private static readonly System.Reflection.MethodInfo? AddTemporaryCost = typeof(CardModel).GetMethod(
        "AddTemporaryStarCost",
        System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Public);

    /// <summary>本场战斗内把该牌费用设为 <paramref name="cost" /> (打出不清除, 战斗结束失效)。</summary>
    public static void SetCostThisCombat(CardModel card, int cost)
    {
        if (card == null || cost < 0) return;
        AddTemporaryCost?.Invoke(card, new object[] { TemporaryCardCost.ThisCombat(cost) });
    }

    /// <summary>把该牌费用设为 <paramref name="cost" />, 打出后清除 (配合消耗词条即"免费一次性")。</summary>
    public static void SetCostUntilPlayed(CardModel card, int cost)
    {
        if (card == null || cost < 0) return;
        AddTemporaryCost?.Invoke(card, new object[] { TemporaryCardCost.UntilPlayed(cost) });
    }
}
