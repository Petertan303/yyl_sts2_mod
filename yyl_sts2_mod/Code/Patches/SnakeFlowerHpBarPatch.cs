using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code.Monsters;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     蛇花小姐的血条与站位。
///     <para>
    ///         ★血条根因 (2026-09-29 反编译确认): 宠物血条与 <c>MonsterModel.IsHealthBarVisible</c> 无关 ——
    ///         <c>NCombatRoom.AddCreature</c> 对所有"非奥斯提"宠物统一关闭血条与鼠标交互，
    ///         只有 <c>Monster is Osty</c> 走特殊分支保留血条。官方就是把宠物设计成不显示血条的。
    ///         解法: AddCreature 后缀对蛇花小姐重新打开交互。
    ///     </para>
    ///     <para>
    ///         ★ v0.113.0 迁移 (2026-11-14): 引擎把这套 API 重命名为
    ///         <c>MonsterModel.IsInteractable</c> / <c>NCreature.SetIsInteractable(bool, bool)</c>，
    ///         旧 <c>ToggleIsInteractable(bool)</c> 已删除。旧 dll 在 113 上 JIT 这个 Postfix 时抛
    ///         MissingMethodException，直接打断 <c>NCombatRoom.CreateAllyNodes</c> ——
    ///         表现为角色站位错乱、敌人不显示、进战斗卡死。
    ///     </para>
///     <para>
///         ★站位 (2026-10-21 移交): 位置强制指定已移除, 召唤定位与体型成长完全交给
///         引擎的 <c>OstyScaleToSize</c> (遗物召唤流程调用, 原版奥斯提同款:
///         tween 到主人身边 + 按生命上限缩放)。本 patch 保留血条显示修复, 并复刻原版 Osty 的层级处理 (蛇花排到主人节点之前, 主人遮挡蛇花)。
///     </para>
/// </summary>
internal static class SnakeFlowerHpBarPatch
{
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
    internal static class AddCreaturePatch
    {
        [HarmonyPostfix]
        internal static void Postfix(Creature creature)
        {
            try
            {
                if (creature?.Monster is not SnakeFlowerMissPet) return;

                var node = NCombatRoom.Instance?.GetCreatureNode(creature);
                if (node == null) return;

                // 第二个参数 = changeVisibilityImmediately: 让血条立刻出现，不等动画。
                node.SetIsInteractable(true, true);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花小姐血条显示失败: {ex.Message}");
            }
        }
    }

}
