using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     散炁: 1 费, 失去 1 炁, 获得 2 → 3 能量。能量引擎。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class SanQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public SanQi() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var energy = 2; // TODO upgrade: would be 3 if IsUpgraded
        await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
        await PlayerCmd.GainEnergy(energy, Owner);
    }
}

