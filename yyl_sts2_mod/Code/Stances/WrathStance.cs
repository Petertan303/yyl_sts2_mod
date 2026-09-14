using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public sealed class WrathStance : yylStanceModel, IModifyDamageMultiplicative
{
    public override bool ShouldReceiveCombatHooks => true;

    protected override StanceVfxConfig VfxConfig => new(
        "res://yyl_sts2_mod/scenes/vfx/wrath_aura.tscn",
        EnterSfxPath: yylAudio.Sfx("stance/wrath_enter.ogg"),
        AmbienceLoopPath: yylAudio.Ambience("wrath_loop.ogg"),
        ScreenFlashColor: new Color(1f, 0.15f, 0.1f),
        ScreenShakeStrength: ShakeStrength.Medium
    );

    
    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (props.HasFlag(ValueProp.Unpowered) || Owner.Creature.CombatState == null) return 1m;
        var varA = yylHook.ModifyWrathDamage(Owner.Creature.CombatState, Owner, 0);
        if (dealer == Owner.Creature) return 2m + varA;
        return target == Owner.Creature ? 2m : 1m;
    }
}
