using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public class NoStance : yylStanceModel
{
    public override bool ShouldReceiveCombatHooks => false;
    protected override StanceVfxConfig VfxConfig => new();
}