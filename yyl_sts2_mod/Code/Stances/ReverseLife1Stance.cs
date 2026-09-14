using Godot;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>逆生一重：每回合第一次获得格挡时，该次格挡翻倍。</summary>
public class ReverseLife1Stance : yylStanceModel
{
    private static readonly SpireField<Creature, bool> FirstBlockUsed = new(() => false);

    public override bool ShouldReceiveCombatHooks => true;

    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(0.85f, 1f, 0.82f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life1.ogg"));

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Creature.Side)
            FirstBlockUsed[Owner.Creature] = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner.Creature || props.HasFlag(ValueProp.Unpowered) || FirstBlockUsed[target])
            return 1m;

        FirstBlockUsed[target] = true;
        return 2m;
    }
}
