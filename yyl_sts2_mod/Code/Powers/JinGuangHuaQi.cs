using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     金光化炁: 每<b>消耗</b> 1 层[gold]金光护体[/gold]，每层本能力获得 1 点[gold]炁[/gold]。
///     <para>
///         常态下金光护体是纯消耗品（掌心雷 / 白长虫 / 五雷正法 / 返照 / 站桩及回合开始衰减），
///         有了本能力后"付出去的金光"会回炉成炁，把防御资源与进攻资源真正接通。
///     </para>
///     <para>
///         ★ 2026-11-15 用户定调「每层能力对应一层效果」: 转换逻辑从
///         <see cref="Utils.yylAegis" /> 里对 GetPower 的直接读值搬进本 power,
///         改由 <see cref="IAegisConsumed" /> 广播驱动, 效果强度直接取 <c>Amount</c>。
///         卡牌层数 / 卡面 {JinGuangHuaQi} / 能力说明 {Amount} / 实际产炁量四者同源。
///     </para>
/// </summary>
public sealed class JinGuangHuaQi : yylPowerModel, IAegisConsumed
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>层数 = 每消耗 1 层金光护体获得的炁量。</summary>
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterAegisConsumed(
        PlayerChoiceContext ctx, Player player, int layers, CardModel? cardSource)
    {
        if (layers <= 0 || Amount <= 0m) return;
        // IAegisConsumed 会遍历全场监听者: 只处理本能力持有者的玩家, 队友消耗金光不给他产炁。
        if (player != Owner.Player) return;

        try
        {
            await yylCmd.GainQi(ctx, player, layers * (int)Amount, this, cardSource);
        }
        catch (Exception ex)
        {
            // 广播是串行的: 本钩子抛异常会打断后面的监听者 (铜镜等), 也会把 Task 弄成失败态。
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光化炁产炁失败: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
