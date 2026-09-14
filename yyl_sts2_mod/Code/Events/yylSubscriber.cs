using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Core;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Events;

public class yylSubscriber
{
    public static void Subscribe()
    {
        ModHelper.SubscribeForCombatStateHooks(MainFile.ModId, CollectModels2);
    }

    private static IEnumerable<AbstractModel> CollectModels2(CombatState combatState)
    {
        return combatState.Players
            .Select(yylModel.GetStanceModel)
            .Where(s => s is not NoStance);
    }
}