using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     吐纳: 1 费, 获得 1 → 2 炁, 抽 1, 消耗。简易发动机。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TuNa(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TuNa() : this(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得 1→2 炁
        var qiAmount = 1; // TODO upgrade: would be 2 if IsUpgraded
        await yylCmd.GainQi(choiceContext, Owner, qiAmount, this, cardPlay.Card);
        // 抽 1
        // TODO: PlayerCmd.Draw not found - use CardPileCmd.Add from draw pile to hand
        // await PlayerCmd.Draw(Owner, 1);
    }
}

