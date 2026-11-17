using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     统计「本场战斗累计获得的金光」，供「寸劲」读取（2026-10-07）。
///     <para>
///         挂在 <c>PowerCmd.Apply</c> 的<b>非泛型</b>重载上 —— 这是所有施加power 的公共出口：
///         泛型 <c>Apply&lt;T&gt;</c> 先 <c>FindExistingInstanceForStacking</c>，
///         不存在则 <c>ToMutable()</c> 后调本方法，存在则走 <c>ModifyAmount</c>叠层；
///         叠层那条路<b>不经过</b> Power 自己的 <c>BeforeApplied</c>/<c>AfterApplied</c>
///         （引擎文档明写 "Does not run if the owner already has this power, and its amount was
///         just changed"），所以统计必须挂在这里才能覆盖叠层。
///     </para>
///     <para/>
///     ★ 钩子绝不抛异常：任何未捕获异常都会冻结整场战斗。
/// </summary>
[HarmonyPatch]
internal static class AegisGainTrackerPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(PowerCmd), "Apply",
            new[]
            {
                typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature),
                typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)
            })
               ?? throw new MissingMethodException("PowerCmd.Apply (non-generic) not found");
    }

    [HarmonyPostfix]
    private static void Postfix(Creature? target, decimal amount)
    {
        try
        {
            // 只统计获得; amount <= 0 是消耗(负数走同一个出口), 不计入累计。
            if (amount <= 0 || target == null) return;

            var combatState = target.CombatState;
            if (combatState == null) return;

            foreach (var power in target.Powers)
            {
                // 判定"这次加的是金光": 直接比 power 类型最可靠。
                if (power is not GoldenAegis) continue;

                AegisGainTracker.Mark(target.Player, combatState, (int)amount);
                return;
            }
        }
        catch (Exception e)
        {
            // 静默失败优于冻结战斗; 需要排查时把 e.Message 写进日志。
            MainFile.Logger.Warn($"[yyl] AegisGainTracker failed: {e.GetType().Name}: {e.Message}");
        }
    }
}