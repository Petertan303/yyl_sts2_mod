using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using System.Linq;

namespace yyl_sts2_mod.Code.Cards.Common;

[Pool(typeof(yyl_sts2_modCardPool))]
public class NlFoot(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public NlFoot(): this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(12, 6);
        WithBlock(12, 6);
        WithPower<PoisonPower>(4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        
        await CommonActions.CardBlock(this, cardPlay);
        // var allCreatures = combatState.Enemies
        //     .Concat(combatState.GetTeammatesOf(Owner.Creature))
        //     .Append(Owner.Creature)
        //     .Where(c => c != null && c.IsAlive)
        //     .ToList(); 
        //
        // foreach (var creature in allCreatures)
        // {
        //     // await CommonActions.CardAttack(this, cardPlay)
        //     //     .Targeting(creature)
        //     //     .WithHitFx("vfx/vfx_attack_slash")
        //     //     .Execute(choiceContext);
        //     // await CommonActions.CardAttack(this, cardPlay)
        //     //     .Targeting(creature)
        //     //     .Execute(choiceContext);
        //     // await PowerCmd.Apply<PoisonPower>(choiceContext, creature, 4, null, null);
        //     // await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        // }
        
        await CommonActions.CardAttack(this, cardPlay)
            .Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, combatState.Creatures, this);
    }
}