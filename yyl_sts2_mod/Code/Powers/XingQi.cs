using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     行炁: 每当你<b>失去炁</b>, 抽 1 张牌 (一次失去事件抽 1 张, 与失去点数无关)。
///     <para>
///         「温养」是把失去炁换成格挡, 行炁是换成过牌 —— 两者是耗炁流的两个方向。
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
        await CardPileCmd.Draw(ctx, 1, player);
    }
}
