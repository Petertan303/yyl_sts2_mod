using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public sealed class NlPower : yylPowerModel
{
    public override PowerType Type => PowerType.None; 
    public override PowerStackType StackType => PowerStackType.None;
    
    private bool _applierIsAttacking;
    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker == Applier)
            _applierIsAttacking = true;
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Applier == null || wasRemovalPrevented || !_applierIsAttacking || creature != Owner) return;
        if (Applier.IsAlive)
        {
            await CreatureCmd.Heal(Applier, 3m);
        }
    }
}