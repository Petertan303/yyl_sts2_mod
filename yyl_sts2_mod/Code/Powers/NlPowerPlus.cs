using System.Threading.Tasks;
using BaseLib.Audio;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public sealed class NlPowerPlus : yylPowerModel
{
    public override PowerType Type => PowerType.Debuff; 
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Applier == null || wasRemovalPrevented || creature != Owner) return;
        if (Applier.IsAlive)
        {
            await CreatureCmd.Heal(Applier, 6m);
        }
    }
    // // 1. 战斗开始时（即这个Buff被初次施加时）
    // public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    // {
    //     ModSound sound = null;
    //     ModAudio.PlaySound(sound);
    //     return base.BeforeHandDraw(player, choiceContext, combatState);
    // }
    //
    // // 2. 攻击时
    // // 请在IDE中通过 override 检查API是 AfterAttack / OnAttack / OnDamageDelt
    // public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    // {
    //     ModSound sound = null;
    //     ModAudio.PlaySound(sound);
    //     return base.AfterAttack(choiceContext, command);
    // }
    //
    // // 3. 死亡时
    // public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    // {
    //     ModSound sound = null;
    //     ModAudio.PlaySound(sound);
    //     return base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    // }
}
