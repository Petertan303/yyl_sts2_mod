using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Rewards;

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

        /*  照原版「禁忌魔典」(ForbiddenGrimoirePower -> CardRemovalReward) 的做法:
            把选牌本身做成一个战后奖励, 由战利品界面驱动, 而不是在这里内联 await
            (战斗结算序列等 Task、Task 又等结算, 会互相等待卡死游戏)。 */
        room.AddExtraReward(player, new QiMeridianEnchantReward(player, count));
        return Task.CompletedTask;
    }
}
