using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>逆生三重：保留二重效果，并在玩家回合开始获得 1 层无实体。</summary>
public sealed class RebirthStanceThree : RebirthStanceTwo
{
    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(1.1f, 0.95f, 0.65f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life3.ogg"));

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        await base.BeforeSideTurnStart(ctx, side, participants, combatState);
        if (side == Owner.Creature.Side)
            await PowerCmd.Apply<IntangiblePower>(ctx, Owner.Creature, 1m, Owner.Creature, null);
    }
}
