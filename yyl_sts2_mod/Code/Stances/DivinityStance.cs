using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
// using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public class DivinityStance : yylStanceModel, IModifyDamageMultiplicative
{
    public override bool ShouldReceiveCombatHooks => true;

    protected override StanceVfxConfig VfxConfig => new(
        "res://yyl_sts2_mod/scenes/vfx/divinity_aura.tscn",
        new Color(1.1f, 0.7f, 1.4f),
        yylAudio.Sfx("stance/divinity_enter.ogg"),
        AmbienceLoopPath: yylAudio.Ambience("divinity_loop.ogg"),
        ScreenFlashColor: new Color(0.8f, 0.3f, 1f),
        ScreenShakeStrength: ShakeStrength.Strong
    );

    public override Task OnEnterStance(PlayerChoiceContext ctx, Player player, CardModel? source)
    {
        player.PlayerCombatState!.GainEnergy(3);
        return base.OnEnterStance(ctx, player, source);
    }
    
    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer == Owner.Creature && !props.HasFlag(ValueProp.Unpowered))
            return 3m;
        return 1m;
    }
}
