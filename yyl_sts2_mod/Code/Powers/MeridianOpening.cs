using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Enchantments;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     开脉 (能力): 本场战斗结束后, 选择牌组中的一张牌永久附魔「炁脉」。
///     <para>
///         机制参考原版死灵法师的「禁忌魔典」(战斗结束后移除一张牌) —— 同样是把
///         一次战斗的结算变成对牌组的永久改造, 只是这里改造成"附魔"而不是"移除"。
///         附魔一旦写上就跨战斗保留, 所以这张牌是「局外资源」的入口。
///     </para>
/// </summary>
public sealed class MeridianOpening : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var player = Owner.Player;
        if (player is null || !player.Creature.IsAlive) return Task.CompletedTask;

        // 层数 = 本次战斗结束时可以附魔几张牌。
        var count = Amount;
        if (count <= 0) return Task.CompletedTask;

        /*  不能在这里内联 await 选牌 —— 那样会死锁:
            战斗结算序列在等这个 Task 完成, 而选牌界面又要等结算推进才可用, 两边互相等待, 游戏卡死。
            原版死灵法师的「禁忌魔典」是用 CombatRoom.AddExtraReward 把选择做成战后奖励来绕开的;
            这里改成让出一帧 (CallDeferred), 等结算流程走完再弹选牌界面, 效果等价且不用自建奖励类。 */
        async void RunSelection()
        {
            try
            {
                var enchantment = ModelDb.Enchantment<QiMeridian>();
                var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, count);
                var selected = await CardSelectCmd.FromDeckForEnchantment(
                    player,
                    enchantment,
                    count,
                    prefs);

                foreach (var card in selected)
                {
                    // CardCmd.Enchant 是同步的: 施加附魔并返回附魔实例本身。
                    CardCmd.Enchant(enchantment, card, 1m);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"MeridianOpening: enchant selection failed.\n{ex}");
            }
        }

        Callable.From(RunSelection).CallDeferred();
        return Task.CompletedTask;
    }
}
