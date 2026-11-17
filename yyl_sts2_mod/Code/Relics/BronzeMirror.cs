using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     铜镜（罕见）：每回合第一次消耗[gold]金光护体[/gold]时，下回合抽 1 张牌。
///     <para>
///         ★ 为什么是「下回合抽」而不是立刻抽：金光的衰减结算在**敌方回合结束**
///         （<c>AfterSideTurnEnd</c>，<c>side != Owner.Side</c>，2026-11-09 改），
///         那一刻抽牌会被回合切换冲掉。用原版 <see cref="DrawCardsNextTurnPower" />
///         （百年积木 / 镜止水同款）把抽牌推迟到下个玩家回合开始。
///         ★ 衰减触发时读的是刚过去那个玩家回合的「本回合已触发」标志：若该回合玩家
///         已主动消耗过金光，衰减不再重复给抽牌；标志在下个玩家回合开始才重置。
///     </para>
///     <para>
///         ★ 触发源是 <see cref="IAegisConsumed" />，广播点在 <c>yylAegis.Consume</c>：
///         主动消耗与回合末自然衰减都会计入，但每个玩家回合只给一次。
///         重置挂在 <c>AfterPlayerTurnStart</c>（判 <c>player == Owner</c>，不用 side ——
///         联机时全员同属一个 CombatSide，side 判不出是谁的回合）。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class BronzeMirror : yylRelicModel, IAegisConsumed
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    private bool _triggeredThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public async Task AfterAegisConsumed(
        PlayerChoiceContext ctx, Player player, int layers, CardModel? cardSource)
    {
        try
        {
            if (player != Owner || layers <= 0 || _triggeredThisTurn) return;
            _triggeredThisTurn = true;

            await PowerCmd.Apply<DrawCardsNextTurnPower>(
                ctx, new[] { Owner.Creature }, 1m, Owner.Creature, cardSource);
            Flash(); // 纯视觉，放最后
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 铜镜触发失败: {ex.Message}");
        }
    }
}
