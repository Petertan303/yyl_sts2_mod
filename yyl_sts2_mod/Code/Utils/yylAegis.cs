using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     金光护体（<see cref="GoldenAegis" />）的<b>统一消耗入口</b>。
///     <para>
///         掌心雷 / 白长虫 / 五雷正法 / 返照的主动消耗，以及
///         <see cref="GoldenAegis.AfterPlayerTurnStart" /> 的玩家回合开始按「单次命中的最大用层数」衰减，全部走这里。
///         ★ 2026-11-08 用户定调：衰减也走本入口，让它能被「金光化炁」「铜镜」等
///         卡牌/遗物捕获利用 —— 这是玩家决策的一种选项，不是需要绕开的漏洞。
///     </para>
/// </summary>
public static class yylAegis
{
    /// <summary>卡牌主动消耗（卡面声明的代价由各卡自己传给 <paramref name="layers" />）。</summary>
    public static Task Consume(PlayerChoiceContext ctx, CardModel card, int layers = 1)
        => ConsumeCore(ctx, card.Owner, card, layers);

    /// <summary>
    ///     无卡牌来源的消耗（玩家回合开始时的自然衰减）。<paramref name="cardSource" /> 可空，
    ///     <c>PowerCmd.Apply</c> 允许传 null（<c>GoldenWard.cs</c> 同款写法）。
    /// </summary>
    public static Task Consume(
        PlayerChoiceContext ctx, Player player, int layers, CardModel? cardSource = null)
        => ConsumeCore(ctx, player, cardSource, layers);

    private static async Task ConsumeCore(
        PlayerChoiceContext ctx, Player? player, CardModel? card, int layers)
    {
        try
        {
            if (layers <= 0 || player == null) return;
            var creature = player.Creature;
            if (creature == null) return;

            // 实际扣层数不得高于当前持有量，避免负层数 / 0 层残留。
            var aegis = creature.GetPower<GoldenAegis>();
            if (aegis == null || aegis.Amount <= 0m) return;
            var actual = (int)Math.Min((decimal)layers, aegis.Amount);
            if (actual <= 0) return;

            await PowerCmd.Apply<GoldenAegis>(ctx, new[] { creature }, -actual, creature, card);

            // 「金光化炁」「铜镜」等 IAegisConsumed 监听者：主动消耗与自然衰减一视同仁。
            // ★ 2026-11-15: 金光化炁的转换逻辑已搬进 Code/Powers/JinGuangHuaQi.cs
            //   (每层 1 点炁, 直接读自己的 Amount)，这里不再直接 GetPower + GainQi,
            //   避免"同一个效果写在两处"导致层数与产炁量对不上。
            await yylHook.AfterAegisConsumed(ctx, player, actual, card);
        }
        catch (Exception ex)
        {
            // 衰减挂在回合钩子里，抛异常会冻结整场战斗。
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光消耗失败: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
