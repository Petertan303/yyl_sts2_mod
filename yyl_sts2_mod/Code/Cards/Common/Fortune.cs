using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     运势: 0 费技能, 抽 2 → 3 张牌, 然后随机弃 1 张手牌。
///     与静默猎手(基准角色)的低费过滤思路一致 —— 用抽弃循环把不需要的牌淘掉,
///     "随机"对应"运势"的赌性。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public class Fortune(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Fortune() : this(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Draw(this, choiceContext);

        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0)
        {
            return;
        }

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var discarded = rng.NextItem(hand);
        if (discarded == null)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, new[] { discarded });
    }
}
