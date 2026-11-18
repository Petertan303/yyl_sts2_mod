using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     [gold]金光护体[/gold]被<b>消耗</b>时广播的事件接口。
///     <para>
///         唯一广播点是 <see cref="Utils.yylAegis.Consume" />——主动消耗（掌心雷 / 白长虫 /
///         五雷正法 / 返照）与**玩家回合开始**的自然衰减都走这个入口，
///         所以监听者不需要区分「玩家主动花掉的」和「回合开始掉掉的」。
///         ★ 2026-11-18：衰减改挂玩家回合开始（AfterPlayerTurnStart，清格挡之后），
///         且多段攻击只按单次命中的最大用层数扣一次。
///         ★ 2026-11-08 用户定调：衰减可被卡牌与遗物捕获利用，这是玩家决策的一种选项。
///     </para>
///     <para>
///         监听者必须是 <c>AbstractModel</c>（power / relic / stance / card），
///         因为 <see cref="yylHook" /> 通过 <c>combatState.IterateHookListeners()</c> 遍历并
///         <c>PushModel</c>。层数参数是<b>实际扣掉</b>的层数（已 clamp 到当前持有量）。
///     </para>
/// </summary>
public interface IAegisConsumed
{
    Task AfterAegisConsumed(PlayerChoiceContext ctx, Player player, int layers, CardModel? cardSource);
}
