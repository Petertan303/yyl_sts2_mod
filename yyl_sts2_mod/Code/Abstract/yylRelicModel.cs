using BaseLib.Abstracts;
using BaseLib.Extensions;
using yyl_sts2_mod.Code.Extensions;

namespace yyl_sts2_mod.Code.Abstract;

public abstract class yylRelicModel : CustomRelicModel
{
    protected override string BigIconPath => 
        $"relics.png".BigRelicImagePath();
        // $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    // // public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".TresRelicImagePath();
    
    // protected override string PackedIconOutlinePath =>
    //     $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres".TresRelicImagePath();
}