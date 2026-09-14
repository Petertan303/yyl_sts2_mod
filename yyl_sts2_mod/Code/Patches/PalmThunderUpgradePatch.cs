using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using yyl_sts2_mod.Code.Cards.Ancient;
using yyl_sts2_mod.Code.Cards.Basic;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     掌心雷升级后「入阴」化为白长虫。
///     <para>
///         原作里阳五雷与阴五雷互斥、不能同时使用, 所以这两张牌不做成两张独立的牌,
///         而是同一张牌的先后形态: 掌心雷升级时直接变形为白长虫。
///         StS2 没有"升级成另一张牌"的声明式接口 (CardModel.MaxUpgradeLevel 只能控制层数),
///         因此这里挂 <see cref="CardCmd.Upgrade(CardModel, CardPreviewStyle)" /> 的后置补丁,
///         在升级完成后调用 <see cref="CardCmd.TransformTo{T}" />。
///     </para>
///     <para>
///         变形失败只记警告, 不会吞掉"这张牌已经升级"的结果 —— 最坏情况就是退化成
///         掌心雷自身的数值强化 (伤害 1 → 2)。
///     </para>
/// </summary>
[HarmonyPatch]
internal static class PalmThunderUpgradePatch
{
    private static MethodBase? TargetMethod() =>
        AccessTools.Method(
            typeof(CardCmd),
            nameof(CardCmd.Upgrade),
            new[] { typeof(CardModel), typeof(CardPreviewStyle) });

    private static void Postfix(CardModel card, ref Task __result)
    {
        if (card is not PalmThunder) return;
        __result = TransformAsync(__result, card);
    }

    private static async Task TransformAsync(Task upgrade, CardModel card)
    {
        await upgrade;

        try
        {
            await CardCmd.TransformTo<WhiteWorm>(card, CardPreviewStyle.None);
        }
        catch (Exception e)
        {
            Godot.GD.PushWarning($"[yyl_sts2_mod] 掌心雷入阴变形失败, 已保留普通升级结果: {e.Message}");
        }
    }
}

/// <summary>批量升级 (事件 / 遗物) 走的是另一个重载, 单独挂一份。</summary>
[HarmonyPatch]
internal static class PalmThunderMassUpgradePatch
{
    private static MethodBase? TargetMethod() =>
        AccessTools.Method(
            typeof(CardCmd),
            nameof(CardCmd.Upgrade),
            new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) });

    private static void Postfix(IEnumerable<CardModel> cards, ref Task __result)
    {
        var targets = cards.OfType<PalmThunder>().ToList();
        if (targets.Count == 0) return;
        __result = TransformAsync(__result, targets);
    }

    private static async Task TransformAsync(Task upgrade, IReadOnlyList<CardModel> cards)
    {
        await upgrade;

        foreach (var card in cards)
        {
            try
            {
                await CardCmd.TransformTo<WhiteWorm>(card, CardPreviewStyle.None);
            }
            catch (Exception e)
            {
                Godot.GD.PushWarning($"[yyl_sts2_mod] 掌心雷入阴变形失败, 已保留普通升级结果: {e.Message}");
            }
        }
    }
}
