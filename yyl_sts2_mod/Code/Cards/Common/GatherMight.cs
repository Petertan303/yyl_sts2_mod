using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     蓄势: 1 费, 下回合开始时获得 2 → 3 炁。延迟型产炁。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GatherMight(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GatherMight() : this(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<Powers.StoredMight>(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var qi = DynamicVars["StoredMight"].IntValue;
        await PowerCmd.Apply<Powers.StoredMight>(choiceContext, new[] { Owner.Creature }, qi, Owner.Creature, cardPlay.Card);
    }
}
