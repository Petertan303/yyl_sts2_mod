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
///     移穴: 1 费, 获得 10 → 15 格挡, 失去 1 炁, 从抽牌堆选 1 张加入手牌。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YiXue(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YiXue() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var block = 10; // TODO upgrade: would be 15 if IsUpgraded
        // TODO: PlayerCmd.GainBlock not found in this BaseLib - hook via yylCmd.GainBlock helper or PowerCmd equivalent
        // await PlayerCmd.GainBlock(block, Owner);
        await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
        // TODO: 从抽牌堆选 1 张加入手牌。BaseLib 应该有现成的 CardSelectCmd / PileScry 类原语
        //       (例如 CardCmd.SearchDrawPileAddToHand / ScryCmd.SearchPickN);
        //       若确认不到,改为 Scry 1 张(老 Mechanics)。
    }
}

