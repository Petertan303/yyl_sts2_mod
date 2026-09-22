using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     吐纳: 获得 1 → 2 炁, 抽 1, 消耗。（罕见, 用于稀释产炁浓度）
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QiBreath(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QiBreath() : this(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<Qi>(1, 1);
        WithCards(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await CommonActions.Draw(this, choiceContext);
    }
}
