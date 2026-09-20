using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Stances;
using yyl_sts2_mod.Code.UI;
using yyl_sts2_mod.Code.Utils;
using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace yyl_sts2_mod.Code.Core;

public class yylModel() : CustomSingletonModel(HookType.Combat)
{
    private static readonly SpireField<Player, yylStanceModel> ActiveStance =
        new(yylModelDb.yylStance<NoStance>);

    public override bool ShouldReceiveCombatHooks => true;


    public static yylStanceModel GetStanceModel(Player player)
    {
        return ActiveStance[player] ?? yylModelDb.yylStance<NoStance>();
    }

    public static bool IsInStance<T>(Player player) where T : yylStanceModel
    {
        return ActiveStance[player] is T;
    }


    public static async Task SetStance<T>(PlayerChoiceContext ctx, Player player, CardModel? source)
        where T : yylStanceModel
    {
        await SetStance(ctx, player, yylModelDb.yylStance<T>(), source);
    }

    private static async Task SetStance(PlayerChoiceContext ctx, Player player, yylStanceModel newCanonical,
        CardModel? source)
    {
        var current = ActiveStance[player];
        if (current?.GetType() == newCanonical.GetType()) return;

        if (current != null)
            await current.OnExitStance(ctx, player, source);

        var mutable = newCanonical.ToMutable(player);
        ActiveStance[player] = mutable;
        await mutable.OnEnterStance(ctx, player, source);

        // 「逆生一重」的首格挡翻倍由隐藏 Power 承载 (原版坚定不移 UnmovablePower 同款管线):
        // 块值分发(ModifyBlockMultiplicative)不吃 ModHelper 订阅的模型, 姿态上的重写从不触发。
        // 进一重挂上, 切到其它姿态/退出时移除。
        var ward = player.Creature.GetPower<RebirthStanceOnePower>();
        if (newCanonical is RebirthStanceOne)
        {
            if (ward == null)
                await PowerCmd.Apply<RebirthStanceOnePower>(ctx, new[] { player.Creature }, 1m, player.Creature, source);
        }
        else if (ward != null)
        {
            await PowerCmd.Remove(ward);
        }

        yylAnim.ResetStanceTint(player.Creature);
        await yylAnim.PlayStanceTransition(player.Creature, mutable.BodyTint);
        await yylHook.OnStanceChange(ctx, player, current ?? yylModelDb.yylStance<NoStance>(), mutable);

        // 把「当前姿态」同步成一个独立于能力栏的常驻标签 (UI/StanceTagIndicator)：进入/退出/切换姿态时
        // 图标与文字自动跟随，悬浮角色头顶的标签即可查看真实效果。姿态 ≠ 能力，不进能力栏。
        StanceTagUI.Sync(player, mutable);
    }

    public override Task BeforeCombatStart()
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null) return Task.CompletedTask;
        StanceTagUI.Reset();
        foreach (var player in state.Players)
        {
            ActiveStance[player] = yylModelDb.yylStance<NoStance>();
            yylAnim.ResetStanceTint(player.Creature);
        }
        return Task.CompletedTask;
    }

    /// <summary>战斗结束时清理姿态 VFX 并把姿态重置为 NoStance，避免 _vfx / StanceVfxContainer 跨战斗残留。</summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null) return;
        foreach (var player in state.Players)
        {
            try
            {
                var current = ActiveStance[player];
                if (current != null && current is not NoStance)
                    await current.OnExitStance(null, player, null);
            }
            catch (Exception e)
            {
                GD.PushError($"[yyl] AfterCombatEnd stance cleanup failed: {e}");
            }
            ActiveStance[player] = yylModelDb.yylStance<NoStance>();
            yylAnim.ResetStanceTint(player.Creature);
        }
        StanceTagUI.Reset();
    }
}
