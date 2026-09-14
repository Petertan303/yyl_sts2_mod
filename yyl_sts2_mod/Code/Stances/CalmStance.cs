using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Vfx;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Stances;

public class CalmStance : yylStanceModel
{
    public override bool ShouldReceiveCombatHooks => true;

    protected override StanceVfxConfig VfxConfig => new(
        "res://yyl_sts2_mod/scenes/vfx/calm_aura.tscn",
        new Color(0.7f, 0.85f, 1.3f),
        yylAudio.Sfx("stance/calm_enter.ogg"),
        AmbienceLoopPath: yylAudio.Ambience("calm_loop.ogg"),
        ScreenFlashColor: new Color(0.4f, 0.7f, 1f)
    );

    public override async Task OnExitStance(PlayerChoiceContext ctx, Player player, CardModel? source)
    {
        var amount = yylHook.ModifyCalmEnergyGain(player.Creature.CombatState!, player, 2);
        await PlayerCmd.GainEnergy(amount, player);
        await base.OnExitStance(ctx, player, source);
    }
}
