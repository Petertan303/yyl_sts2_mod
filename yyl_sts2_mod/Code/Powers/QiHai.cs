using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     炁海: 每回合开始时获得 Amount 点炁 (1 → 2 点)。产炁流的稳定来源。
///     <para>
///         与「金光护体源」(每回合 +1 层金光) 对称: 一个是攻的长期投资, 一个是防的长期投资。
///         炁是对数增伤, 所以这里的"每回合 +1"更像是给天火/丹噬/耗炁牌供血的引擎, 而非直接伤害。
///     </para>
///     <para>
///         ★ [2026-10-19 修] 时机从 <c>BeforeSideTurnStart</c> 挪到
///         <c>AfterPlayerTurnStart</c>。原版引擎在 <c>Creature.AfterTurnStart</c> 里
///         调 <c>ClearBlock</c>, 而 <c>StartTurn</c> 的 IL 顺序是
///         <c>BeforeSideTurnStart</c> → <c>AfterTurnStart</c>(清格挡) → <c>AfterSideTurnStart</c>
///         —— 挂在 <c>BeforeSideTurnStart</c> 时, 本次发的炁经「温养」换算出的格挡
///         **紧跟着就被 ClearBlock 抹掉** (观感: 回合开始冒出一个格挡数字然后立刻消失)。
///         挪到 <c>AfterPlayerTurnStart</c> 后, 发炁发生在清格挡之后, 格挡能正常留下来;
///         它也带 <c>PlayerChoiceContext</c> (而 <c>AfterSideTurnStart</c> 不带, 引擎的
///         PowerCmd 全系都要 ctx), 所以这是唯一可行的安全位置。
///         ⚠ 守卫用 <c>player == Owner.Player</c> 而非 side: 联机时所有玩家同属一个
///         CombatSide, side 判不出是谁的回合 (同 HeartGuard/LongNu 的做法)。
///     </para>
/// </summary>
public sealed class QiHai : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player) return;
        var amount = (int)Amount;
        if (amount <= 0) return;
        if (Owner.Player == null) return;
        await yylCmd.GainQi(choiceContext, Owner.Player, amount, this, null);
    }
}