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
        IEnumerable<AbstractModel> stances = combatState.Players
            .Select(yylModel.GetStanceModel)
            .Where(s => s is not NoStance);
        // 手牌金光驱动 (隐藏 Power, 由黄桃罐头战斗开始挂上)。
        IEnumerable<AbstractModel> drivers = combatState.Players
            .SelectMany(p => p.Creature.Powers)
            .OfType<CardGlowDriver>();
        return stances.Concat(drivers);
    }
}