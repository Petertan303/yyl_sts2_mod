using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     狂热: 1 费能力牌, 获得 1 → 2 层狂热(攻击奶龙时每层额外造成 2 点伤害)。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Zeal(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Zeal() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<Fervor>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["Fervor"].IntValue;
        await PowerCmd.Apply<Fervor>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
