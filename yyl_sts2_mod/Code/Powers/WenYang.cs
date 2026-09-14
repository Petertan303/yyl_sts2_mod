using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Compatibility;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     温养: 获得炁时, 对所有敌人造成 4 → 6 点伤害 / 层。
///     Hook-based: implements <see cref="IGainQi" />, reacts as a follow-up side effect
///     whenever the player gains Qi.
/// </summary>
public sealed class WenYang : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Base damage dealt to each enemy per stack of WenYang on each Qi gain event.</summary>
    public const int BaseDamage = 4;

    public int ModifyQiGain(Player player, int amount)
    {
        // Opt out of the modify pass — we only care about the after-modifying follow-up.
        return amount;
    }

    public async Task AfterModifyingQiGain(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        var damage = BaseDamage * Amount;
        if (damage <= 0) return;

        foreach (var enemy in combatState.HittableEnemies)
        {
            await CompatibilityCreatureCmd.Damage(
                ctx, enemy, damage, default(ValueProp), cardSource: null!, cardPlay: null);
        }
    }
}
