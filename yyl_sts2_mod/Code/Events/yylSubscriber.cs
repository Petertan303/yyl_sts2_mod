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
        // ★注意: 挂在 Creature 身上的 Power 由引擎 IterateHookListeners 自动枚举,
        //   不需要进本收集器; 这里收集的是"不在任何 Creature 身上"的模型(姿态)。
        //   切勿把 CardSfxDriver 加进来 —— 会与引擎自动派发重复, 导致出牌音效响两遍。
        IEnumerable<AbstractModel> drivers = combatState.Players
            .SelectMany(p => p.Creature.Powers)
            .OfType<CardGlowDriver>();
        return stances.Concat(drivers);
    }
}