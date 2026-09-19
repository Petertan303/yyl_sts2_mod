using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     马步: 1 费, 获得 3 → 5 格挡, 获得 1 点炁 (升级只加格挡不加炁)。
///     [balance 2026-09-19] 升级原为 "+1 炁", 因产炁浓度控制改为只加防御。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class HorseStance(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public HorseStance() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(3, 2);
        WithPower<Qi>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
    }
}
