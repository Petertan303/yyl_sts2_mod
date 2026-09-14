using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Core;

public class yylModelDb
{
    public static T yylStance<T>() where T : yylStanceModel
    {
        return ModelDb.GetById<T>(ModelDb.GetId<T>());
    }
}