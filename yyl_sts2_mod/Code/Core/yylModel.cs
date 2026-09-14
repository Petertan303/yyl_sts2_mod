using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Nodes;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Core;

public class yylModel() : CustomSingletonModel(HookType.Combat)
{
    private static readonly SpireField<Player, yylStanceModel> ActiveStance =
        new(yylModelDb.yylStance<CalmStance>);

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

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        var visuals = creatureNode?.Visuals as yylNCreatureVisuals;
        visuals?.SetEyeStance(mutable switch
        {
            WrathStance => "wrath",
            CalmStance => "calm",
            DivinityStance => "divinity",
            _ => "RESET"
        });
        await yylHook.OnStanceChange(ctx, player, current!, ActiveStance[player]!);
    }

    public override Task BeforeCombatStart()
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null) return Task.CompletedTask;
        foreach (var player in state.Players)
            ActiveStance[player] = yylModelDb.yylStance<NoStance>();
        return Task.CompletedTask;
    }
}