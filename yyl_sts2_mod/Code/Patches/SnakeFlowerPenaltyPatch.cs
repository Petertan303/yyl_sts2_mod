using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Cards.Quests;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     任务卡「蛇花」的负面: 此牌在手牌中时, 你每次攻击敌人并<b>实际造成伤害</b>后,
///     该敌人获得 4 点格挡 (被完全格挡掉的段落不算)。
///     <para>
///         ★v2 (2026-09-29): 第一版挂在 <c>CreatureCmd.Damage</c> 上, 实测不触发 ——
///         攻击并不走那个入口。改为挂 <see cref="AttackCommand.Execute" /> 的后缀:
///         等攻击结算完, 逐段检查 <see cref="DamageResult.UnblockedDamage" />,
///         每个真正造成伤害的段落触发一次。Harmony 支持"原方法为 async 时后缀返回 Task 会被等待"。
///     </para>
///     <para>整个过程包 try/catch —— 钩子绝不抛异常。</para>
/// </summary>
internal static class SnakeFlowerPenaltyPatch
{
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    internal static class AttackExecutePatch
    {
        // ★Harmony 直传后缀的返回类型必须与原方法完全一致 (Task<AttackCommand>),
        //   返回 Task 会报 "Return type of pass through postfix ... does not match" (实测 2026-09-29)。
        [HarmonyPostfix]
        internal static async Task<AttackCommand> Postfix(Task<AttackCommand> __result)
        {
            var attack = await __result;
            try
            {
                var dealer = attack.Attacker;
                var player = dealer?.Player;
                if (player == null) return attack;

                // 手上是否有「蛇花」
                var inHand = PileType.Hand.GetPile(player).Cards.OfType<SnakeFlower>().Any();
                if (!inHand) return attack;

                foreach (var group in attack.Results)
                {
                    foreach (var result in group)
                    {
                        var receiver = result.Receiver;
                        if (receiver == null || dealer == null) continue;
                        if (receiver.Side == dealer.Side) continue; // 只针对敌人
                        if (!receiver.IsAlive) continue;
                        if (result.UnblockedDamage <= 0m) continue; // 被格挡掉的不算

                        // 卡面写明"获得 4 点格挡": Unpowered, 与玩家自己的格挡互不干扰。
                        await CreatureCmd.GainBlock(
                            receiver, SnakeFlower.BlockGrantedToEnemy, ValueProp.Unpowered, null, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花惩罚补丁异常: {ex.Message}");
            }

            return attack;
        }
    }
}
