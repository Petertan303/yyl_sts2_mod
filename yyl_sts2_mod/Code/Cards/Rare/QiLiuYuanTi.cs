using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     炁流源体: 1 费, 将当前炁翻倍, 消耗。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QiLiuYuanTi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QiLiuYuanTi() : this(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var current = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (current <= 0) return;
        var gain = current; // double means +current
        await yylCmd.GainQi(choiceContext, Owner, gain, this, cardPlay.Card);
    }
}
