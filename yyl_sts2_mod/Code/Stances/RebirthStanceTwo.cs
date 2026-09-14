using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>逆生二重：保留一重效果，并在玩家回合开始回复 6 点生命。</summary>
public class RebirthStanceTwo : RebirthStanceOne
{
    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(0.7f, 1f, 0.9f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life2.ogg"));

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        await base.BeforeSideTurnStart(ctx, side, participants, combatState);
        if (side == Owner.Creature.Side)
            await CreatureCmd.Heal(Owner.Creature, 6m);
    }
}
