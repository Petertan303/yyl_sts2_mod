using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     移穴: 1 费, 获得 10 → 15 格挡, 失去 1 炁, 从抽牌堆选 1 张加入手牌。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class AcupointShift(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public AcupointShift() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(10, 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);

        var card = await CommonActions.SelectSingleCard(this, SelectionScreenPrompt, choiceContext, PileType.Draw);
        if (card != null)
        {
            // 抽牌堆 -> 手牌。CardPileCmd.Draw(ctx, n, player) 是"随机抽 n 张", 不能用来抽指定牌,
            // 指定牌要用 Add。Add 之后必须 PreviewCardPileAdd, 否则牌进了手牌但手牌 UI 不刷新,
            // 看起来就像"选了牌却没生效"。
            var result = await CardPileCmd.Add(card, PileType.Hand);
            CardCmd.PreviewCardPileAdd(result, 0.6f, CardPreviewStyle.HorizontalLayout);
        }
    }
}
