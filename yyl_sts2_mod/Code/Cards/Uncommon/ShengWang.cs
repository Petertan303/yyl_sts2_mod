using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     生旺死衰: 0 费, 选择手牌中的 1 张牌, 其在本场战斗中费用 -1;
///     此牌自身在本场战斗中费用 +1。升级附加保留 (Retain)。
///     <para>
///         [new 2026-09-19] 用户定调的"伪加费"位: 每次发动都会让下一次更贵,
///         自带边际递减, 不会像降费引擎那样无限滚雪球。
///         费用增减走 <see cref="CardCostUtil.SetCostThisCombat" />
///         (反射 CardModel.AddTemporaryStarCost + TemporaryCardCost.ThisCombat)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShengWang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShengWang() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 从手牌选 1 张 (必须用 CardSelectCmd.FromCombatPile, 见移穴的实现笔记)。
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || hand.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, hand, Owner, prefs)).ToList();
        if (selected.Count == 0) return;

        // 被选的牌: 本场战斗费用 -1 (不低于 0)。
        var target = selected[0];
        CardCostUtil.SetCostThisCombat(target, Math.Max(0, target.CurrentStarCost - 1));

        // 此牌自身: 本场战斗费用 +1 (边际递减)。
        CardCostUtil.SetCostThisCombat(cardPlay.Card, cardPlay.Card.CurrentStarCost + 1);
    }
}
