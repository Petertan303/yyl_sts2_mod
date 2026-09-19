using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     拾遗: 1 → 0 费, 从弃牌堆选择 1 张牌加入手牌, 其费用变为 0, 并附加[消耗]。
///     <para>
///         [new 2026-09-19] 用户定调的"烧牌/牌堆控制"补位之一: 精准回收 +
///         免费打出 (费用置 0 只在打出前有效, 打出后清除; 配合附加的消耗,
///         等效于这张被回收的牌"免费且不占牌库")。
///         与移穴 (选抽牌堆, 不变费) 区分: 拾遗回收的是已经打出过的牌。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShiYi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShiYi() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 从弃牌堆选 1 张 (必须用 CardSelectCmd.FromCombatPile, 见移穴的实现笔记)。
        var discard = PileType.Discard.GetPile(Owner);
        if (discard == null || discard.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, discard, Owner, prefs)).ToList();
        if (selected.Count == 0) return;

        var card = selected[0];
        // 费用置 0 (打出后清除), 并附加消耗词条 —— 二者都作用于被回收的那张牌。
        CardCostUtil.SetCostUntilPlayed(card, 0);
        card.AddKeyword(CardKeyword.Exhaust);

        // Add 之后必须 PreviewCardPileAdd, 否则牌进了手牌但手牌 UI 不刷新。
        var result = await CardPileCmd.Add(card, PileType.Hand);
        CardCmd.PreviewCardPileAdd(result, 0.6f, CardPreviewStyle.HorizontalLayout);
    }
}
