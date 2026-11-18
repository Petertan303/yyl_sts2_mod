using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     金光护体源: 持有者每回合开始时获得 Amount 层金光护体。
///     为金光护体提供除「金光咒」之外的第二条稳定获取途径。
///     <para>
///         ★2026-11-18 时机修正: 从 <c>BeforeSideTurnStart</c> 挪到 <c>AfterPlayerTurnStart</c>。
///         叠金光本身不受清格挡影响, 但「鎏金镜」(获得金光即给等层格挡) 的联动格挡
///         若在清格挡前生成会被 ClearBlock 抹掉 —— 挪到清格挡之后一并解决。
///         守卫用 <c>player == Owner.Player</c> (联机时 side 判不出是谁的回合)。
///     </para>
/// </summary>
public sealed class GoldenWard : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext ctx, Player player)
    {
        if (Owner == null || player == null || player != Owner.Player) return;
        if (Amount <= 0) return;
        await PowerCmd.Apply<GoldenAegis>(ctx, new[] { Owner }, Amount, Owner, null);
    }
}
