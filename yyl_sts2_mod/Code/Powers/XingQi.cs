using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     行炁: 每当你<b>失去炁</b>, 抽 <c>Amount</c> 张牌 (一次失去事件触发一次, 与失去点数无关)。
///     ★ 2026-11-15 用户定调: 「每层能力对应一层效果」—— 卡牌给 2 层就抽 2 张,
///       层数不再是纯装饰。卡面用 {DrawCount}、能力说明用 {Amount}, 三者一致。
///     <para>
///         「丹噬」是把失去炁换成中毒, 行炁是换成过牌 —— 两者是耗炁流的两个方向
///         (2026-11-08: 温养改为产炁侧格挡)。
///         配合散炁/通畅/炁化金光这类"花炁"的牌, 可以把炁同时当伤害、能量和过牌用。
///     </para>
/// </summary>
public sealed class XingQi : yylPowerModel, ILoseQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyQiLoss(Player player, int amount) => amount;

    public async Task AfterModifyingQiLoss(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;
        if (player != Owner.Player) return;
        // 每层 1 张: 2 层能力就抽 2 张 (2026-11-15)。Amount 为 0 时该 power 应已散去,
        // 仍做一次守卫, 避免 Draw(0) 走空命令管线。
        var cards = (int)Amount;
        if (cards <= 0) return;
        await CardPileCmd.Draw(ctx, cards, player);
    }
}
