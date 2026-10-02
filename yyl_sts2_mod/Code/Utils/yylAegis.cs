using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     金光护体（<see cref="GoldenAegis" />）的<b>统一消耗入口</b>。
///     <para>
///         掌心雷 / 白长虫 / 五雷正法原本各自调用
///         <c>CommonActions.ApplySelf&lt;GoldenAegis&gt;</c>。集中到本方法后，
///         「金光化炁」这类"消耗金光时触发"的能力只需在这一处挂钩，
///         不必改动三张卡、也不会漏挂。
///     </para>
/// </summary>
public static class yylAegis
{
    /// <summary>
    ///     消耗 <paramref name="layers" /> 层金光护体。
    ///     若持有者拥有「金光化炁」，每消耗 1 层获得 <c>JinGuangHuaQi.Amount</c> 点炁。
    /// </summary>
    public static async Task Consume(PlayerChoiceContext ctx, CardModel card, int layers = 1)
    {
        if (layers <= 0) return;

        // 空安全: 预览/联机切换场景 Owner 或 Creature 可能为空，直接跳过（绝不抛异常）。
        var owner = card.Owner;
        var creature = owner?.Creature;
        if (owner == null || creature == null) return;
        if (!creature.HasPower<GoldenAegis>()) return;

        await PowerCmd.Apply<GoldenAegis>(ctx, new[] { creature }, -layers, creature, card);

        // 「金光化炁」: 消耗掉的金光按比例回炉成炁。
        var convert = creature.GetPower<JinGuangHuaQi>();
        if (convert == null || convert.Amount <= 0) return;

        var qi = layers * convert.Amount;
        await yylCmd.GainQi(ctx, owner, qi, convert, card);
    }
}
