using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Extensions;

namespace yyl_sts2_mod.Code.Abstract;

public abstract class yylPowerModel : CustomPowerModel
{
    public sealed override string CustomPackedIconPath =>
        $"power.png".PowerImagePath();
        // $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    public sealed override string CustomBigIconPath => CustomPackedIconPath;
    
    // public abstract override PowerType Type { get; }
    // public abstract override PowerStackType StackType { get; }
}