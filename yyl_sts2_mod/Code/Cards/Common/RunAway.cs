using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
// using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Common;

// 占位实现：暂不进入正式卡池。
#pragma warning disable STS004
public class RunAway(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public RunAway() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(700, 300);
    }


    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }
}
