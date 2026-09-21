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

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     炼化: 1 → 0 费, 消耗手中所有的牌, 每消耗一张获得 1 层金光护体。本身消耗。
///     <para>
///         [new 2026-09-19] 用户定调的"烧牌/牌堆控制"补位之一: 把整手牌炼成金光,
///         是金光体系的一张爆发型产出来源 (通常 3~5 层, 回合末打空手牌时最赚)。
///         与金光壁 (无代价厚产) 区分: 炼化烧掉的是手牌资源。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class LianHua(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public LianHua() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        // 迭代副本: 消耗会改变牌堆内容。
        var cards = hand.Cards.ToList();
        foreach (var card in cards)
            await CardCmd.Exhaust(choiceContext, card);

        if (cards.Count > 0)
            await PowerCmd.Apply<GoldenAegis>(choiceContext, new[] { Owner.Creature },
                cards.Count, Owner.Creature, cardPlay.Card);
    }
}
