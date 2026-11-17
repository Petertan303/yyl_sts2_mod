using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.UI;

/// <summary>
///     姿态指示器的生命周期管理：在切换/进入姿态时创建或更新常驻标签，退出姿态（<see cref="NoStance" />）时销毁。
///     与 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreature" /> 解耦——自身持有每个玩家的指示器节点引用。
///     <para>
///         ★★ <b>2026-10-05 修「SL 后进战斗整场卡死」</b>:
///         日志 <c>Combat #36 turn loop died while its combat is in progress; the combat is stuck
///         until the room is restarted: System.ObjectDisposedException: Cannot access a disposed object.
///         Object name: 'yyl_sts2_mod.Code.UI.StanceTagIndicator'. at Godot.Node.QueueFree()
///         at StanceTagUI.Reset() line 40 at yylModel.BeforeCombatStart() line 85</c>。
///         <para/>
///         根因: 本类是<b>静态字典</b>, 跨战斗持有 <see cref="StanceTagIndicator" /> 节点引用。
///         读档(SL)/换战斗时旧节点已被引擎释放(disposed), 但字典没被清掉;下次
///         <see cref="Reset" /> 遍历时对 disposed 对象调 <c>QueueFree()</c> → Godot 抛
///         <c>ObjectDisposedException</c>。而这发生在
///         <c>Hook.BeforeCombatStart → CombatManager.StartCombatInternal</c> 里,
///         异常直接<b>杀死整个战斗回合循环</b> ⇒ 表现为"进战斗后整场卡死, 只能重开房间"。
///         <para/>
///         修法: ①每次释放前用 <see cref="GodotObject.IsInstanceValid" /> 判活(项目既有范式,
///         见 <c>Utils/yylAudio.cs</c>); ② 整个方法包 try/catch —— UI 代码永不影响战斗流程
///         (与项目铁律「钩子绝不抛异常」一致)。
///         </para>
///     </para>
/// </summary>
public static class StanceTagUI
{
    private static readonly Dictionary<Player, StanceTagIndicator> Indicators = new();

    /// <summary>
    ///     安全释放一个指示器：跳过已被引擎释放的节点（否则 <c>QueueFree()</c> 抛 ObjectDisposedException），
    ///     并单点 try/catch 保证一个坏节点不会中断整段清理。
    /// </summary>
    private static void SafeFree(StanceTagIndicator? indicator)
    {
        if (indicator == null) return;
        try
        {
            // ★ 关键: 静态字典里的节点可能已被引擎 dispose（SL / 换战斗 / 房间卸载）。
            //   不判活就 QueueFree 会抛 ObjectDisposedException。
            if (!GodotObject.IsInstanceValid(indicator)) return;
            indicator.QueueFree();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"StanceTagUI.SafeFree: {ex.Message}");
        }
    }

    /// <summary>让指定玩家的姿态指示器与当前姿态保持一致。</summary>
    public static void Sync(Player player, yylStanceModel stance)
    {
        try
        {
            if (player == null) return;

            if (Indicators.TryGetValue(player, out var old))
            {
                SafeFree(old);
                Indicators.Remove(player);
            }

            if (stance is NoStance) return;

            var room = NCombatRoom.Instance;
            if (room == null) return; // 战斗尚未就绪；姿态进入通常发生在战斗中，届时房间已存在
            if (!GodotObject.IsInstanceValid(room)) return;

            var indicator = new StanceTagIndicator();
            room.AddChild(indicator);
            indicator.TopLevel = true;
            indicator.Bind(player, stance);
            Indicators[player] = indicator;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"StanceTagUI.Sync: {ex.Message}");
        }
    }

    /// <summary>战斗开始时清空所有残留指示器（玩家对象每场战斗重建，避免跨场泄漏）。</summary>
    public static void Reset()
    {
        try
        {
            foreach (var indicator in Indicators.Values)
                SafeFree(indicator);
            Indicators.Clear();
        }
        catch (Exception ex)
        {
            // 连字典遍历本身出错(理论上不会)也不能影响战斗
            MainFile.Logger.Warn($"StanceTagUI.Reset: {ex.Message}");
        }
    }
}
