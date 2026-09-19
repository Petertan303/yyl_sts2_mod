using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     移穴: 1 费, 获得 10 → 15 格挡, 失去 2 炁, 从抽牌堆选 1 张加入手牌。
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
        if(Owner.Creature.GetPower<Qi>()?.Amount >= 2)
        {
            await yylCmd.LoseQi(choiceContext, Owner, 2, this, cardPlay.Card);

            /*  从战斗牌堆(抽牌堆)选牌必须用 CardSelectCmd.FromCombatPile:
                之前用的 CommonActions.SelectSingleCard(..., PileType.Draw) 会让牌卡在待打出区、
                选牌界面根本不弹出来(OnPlay 的 Task 一直挂在那儿)。
                CardPileCmd.Draw(ctx, n, player) 是"随机抽 n 张", 也不能用来抽指定牌。 */
            var pile = PileType.Draw.GetPile(Owner);
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
            var selected = (await CardSelectCmd.FromCombatPile(choiceContext, pile, Owner, prefs)).ToList();

            if (selected.Count > 0)
            {
                // Add 之后必须 PreviewCardPileAdd, 否则牌进了手牌但手牌 UI 不刷新。
                var result = await CardPileCmd.Add(selected[0], PileType.Hand);
                CardCmd.PreviewCardPileAdd(result, 0.6f, CardPreviewStyle.HorizontalLayout);
            }
        }
    }
}
