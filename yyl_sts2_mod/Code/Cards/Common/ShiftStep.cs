using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     套步: 1 费, 抽 1 → 2 张, 然后随机消耗 1 张手牌。
///     廉价的滤牌: 抽得少, 但能把废牌(尤其状态牌)随机烧掉, 越到残局越有用。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShiftStep(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShiftStep() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1);
        // 仅用于卡面显示: 随机消耗几张
        WithCalculatedDamage("ExhaustCount", 1, (_, _) => 0m, 0, 0, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Draw(this, choiceContext);
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0) return;
        var pick = Owner.RunState.Rng.CombatCardSelection.NextItem(hand);
        if (pick != null)
            await CardCmd.Exhaust(choiceContext, pick);
    }
}
