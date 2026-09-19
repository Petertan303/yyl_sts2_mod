using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     手牌条件发光的**驱动 Power**(对玩家隐藏, <c>IsVisibleInternal => false</c>)。
///     由起始遗物(黄桃罐头)战斗开始时挂到玩家身上, 并经由
///     <see cref="yylSubscriber" /> 的订阅回调接收 CombatState hooks;
///     每当回合开始 / 抽牌 / 打出牌 / 任意 Power 数量变化(含炁增减)时,
///     触发 <see cref="GlowRegistry.Refresh" /> 重算手牌金光。
///     <para>
///         视觉本体是引擎的 <c>NCard.CardHighlight</c> (NCardHighlight,
///         公开 AnimShow/AnimHide), 参考原版华丽收场的"条件满足即高亮"。
///     </para>
/// </summary>
public sealed class CardGlowDriver : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家完全隐藏 (不占 buff 槽位、无图标)。</summary>
    protected override bool IsVisibleInternal => false;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        GlowRegistry.Refresh();
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        GlowRegistry.Refresh();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        GlowRegistry.Refresh();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        // 炁/金光等数量变化都可能翻转条件 (如耗炁后炁冲不再满足)。
        GlowRegistry.Refresh();
        return Task.CompletedTask;
    }
}
