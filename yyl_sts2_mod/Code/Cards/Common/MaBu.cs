using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     马步: 1 费, 获得 3 → 5 格挡, 获得 1 → 2 炁。防御 + 产炁。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class MaBu(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public MaBu() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var block = 3; // TODO upgrade: would be 5 if IsUpgraded
        var qi = 1; // TODO upgrade: would be 2 if IsUpgraded
        // TODO: PlayerCmd.GainBlock not found in this BaseLib - hook via yylCmd.GainBlock helper or PowerCmd equivalent
        // await PlayerCmd.GainBlock(block, Owner);
        await yylCmd.GainQi(choiceContext, Owner, qi, this, cardPlay.Card);
    }
}

