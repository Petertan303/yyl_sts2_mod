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

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     纳炁: 1 费, 获得 1 → 2 点炁。
///     最朴素的产炁牌。炁是对数增伤 + 各种花费的资源, 所以产炁一律压低。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class IntakeQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    // [balance 2026-09-22] 雪藏: 稀有度 Common → Basic, 暂离卡池 (白卡主产炁件, 产炁改由马步/引炁/蓄势承担)。
    public IntakeQi() : this(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithPower<Qi>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
    }
}
