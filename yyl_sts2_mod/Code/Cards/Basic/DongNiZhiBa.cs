using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Relics;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     东尼之霸：2 费能力（罕见），升级后 1 费。获得战斗内遗物"东尼之霸" ——
///     你获得的格挡恒定为 15（任意来源：卡牌 / 遗物 / buff），战斗结束遗物自毁。
///     <para>
///         结算由遗物 <see cref="yyl_sts2_mod.Code.Relics.DongNiZhiBa" /> 纯引擎原生接管
///         （覆写 ModifyBlockMultiplicative，无 Harmony）。升级只降费。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DongNiZhiBa(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DongNiZhiBa() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await RelicCmd.Obtain(ModelDb.Relic<Relics.DongNiZhiBa>(), Owner, -1);
    }
}
