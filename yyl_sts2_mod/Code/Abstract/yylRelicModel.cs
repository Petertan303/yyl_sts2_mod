using BaseLib.Abstracts;
using BaseLib.Extensions;
using yyl_sts2_mod.Code.Extensions;

namespace yyl_sts2_mod.Code.Abstract;

public abstract class yylRelicModel : CustomRelicModel
{
    protected override string BigIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
}
