using MegaCrit.Sts2.Core.HoverTips;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Core;

public class yylHoverTipFactory
{
    public static IHoverTip FromStance<T>() where T : yylStanceModel
    {
        return FromStance(yylModelDb.yylStance<T>());
    }

    public static IHoverTip FromStance(yylStanceModel model)
    {
        return model.DumbHoverTip;
    }
}