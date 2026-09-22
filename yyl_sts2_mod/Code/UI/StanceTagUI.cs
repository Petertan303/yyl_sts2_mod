using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.UI;

/// <summary>
/// 姿态指示器的生命周期管理：在切换/进入姿态时创建或更新常驻标签，退出姿态（<see cref="NoStance" />）时销毁。
/// 与 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreature" /> 解耦——自身持有每个玩家的指示器节点引用。
/// </summary>
public static class StanceTagUI
{
    private static readonly Dictionary<Player, StanceTagIndicator> Indicators = new();

    /// <summary>让指定玩家的姿态指示器与当前姿态保持一致。</summary>
    public static void Sync(Player player, yylStanceModel stance)
    {
        if (Indicators.TryGetValue(player, out var old))
        {
            old.QueueFree();
            Indicators.Remove(player);
        }

        if (stance is NoStance) return;

        var room = NCombatRoom.Instance;
        if (room == null) return; // 战斗尚未就绪；姿态进入通常发生在战斗中，届时房间已存在

        var indicator = new StanceTagIndicator();
        room.AddChild(indicator);
        indicator.TopLevel = true;
        indicator.Bind(player, stance);
        Indicators[player] = indicator;
    }

    /// <summary>战斗开始时清空所有残留指示器（玩家对象每场战斗重建，避免跨场泄漏）。</summary>
    public static void Reset()
    {
        foreach (var indicator in Indicators.Values)
            indicator.QueueFree();
        Indicators.Clear();
    }
}
