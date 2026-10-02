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
///         <c>NCombatRoom.AddCreature</c> 对所有"非奥斯提"宠物统一调用
///         <c>ToggleIsInteractable(on: false)</c>（同时关闭血条与鼠标交互），
///         只有 <c>Monster is Osty</c> 走特殊分支保留血条。官方就是把宠物设计成不显示血条的。
///         解法: AddCreature 后缀对蛇花小姐重新 <c>ToggleIsInteractable(true)</c>。
///     </para>
///     <para>
///         ★站位: 让蛇花稍微离开主角脚边（右移 1/3 主角身位、上移 1/20 身位）。
///         必须在 <b>两个</b>地方修正位置 —— AddCreature（战斗中途召唤）和
///         PositionPlayersAndPets（战斗开始统一摆位, 会用绝对坐标覆盖之前的值）；
///         两处都按"相对主角的绝对偏移"设置, 天然幂等不会叠加。
///         血条是 NCreature 的子节点, 会跟随本体移动。
///     </para>
/// </summary>
internal static class SnakeFlowerHpBarPatch
{
    private const float OffsetXRatio = 36f / 30f;
    private const float OffsetYRatio = 1f / 30f;

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

                node.ToggleIsInteractable(on: true);
                ApplyOffset(node);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花小姐血条显示失败: {ex.Message}");
            }
        }
    }

    /// <summary>战斗开始统一摆位会把宠物拉回脚边, 这里再修正一次。</summary>
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
    internal static class PositionPetsPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(List<NCreature> creatureNodes)
        {
            try
            {
                foreach (var node in creatureNodes)
                {
                    if (node?.Entity?.Monster is not SnakeFlowerMissPet) continue;
                    ApplyOffset(node);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花小姐站位修正失败: {ex.Message}");
            }
        }
    }

    private static void ApplyOffset(NCreature node)
    {
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(node.Entity.PetOwner?.Creature);
        if (ownerNode?.Visuals == null) return;

        var w = ownerNode.Visuals.Bounds.Size.X;
        var h = ownerNode.Visuals.Bounds.Size.Y;

        // 相对主角的绝对偏移 (幂等): 右移 1/3 身位, 上移 1/20 身位。
        node.Position = new Vector2(
            ownerNode.Position.X + w * OffsetXRatio,
            ownerNode.Position.Y - h * OffsetYRatio);
    }
}
