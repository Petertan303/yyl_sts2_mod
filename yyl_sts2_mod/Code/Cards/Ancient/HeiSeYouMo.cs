using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace yyl_sts2_mod.Code.Cards.Ancient;

/// <summary>
///     黑色幽默: 1 费, 为奶龙回复 20 HP, 获得 3 炁, 获得 1 层无实体。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class HeiSeYouMo(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public HeiSeYouMo() : this(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        // 为奶龙回复 20 HP
        foreach (var c in combatState.Creatures)
        {
            if (c.HasPower<NlPower>() || c.HasPower<NlPowerPlus>())
                await CreatureCmd.Heal(c, 20);
        }
        // 获得 3 炁
        await yylCmd.GainQi(choiceContext, Owner, 3, this, cardPlay.Card);
        // 1 层无实体
        await PowerCmd.Apply<IntangiblePower>(choiceContext, new[] { Owner.Creature }, 1, Owner.Creature, cardPlay.Card);
    }
}
