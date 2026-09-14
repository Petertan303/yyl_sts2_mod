using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     炁婴: 「老农功」的具现 —— 危急时刻接管宿主身体的战斗人格。
///     <para>
///         机制照搬 LexNinja2 的「罗汉分身」(TwoMonksPower): 该能力让你多获得一个回合,
///         这个回合由系统自动打出所有可打出的牌, 打完即结束回合并消耗 1 层。
///     </para>
/// </summary>
public sealed class QiInfant : yylPowerModel
{
    /// <summary>本层是否已经兑换成额外回合; 只有兑换过才在侧回合结束时递减层数。</summary>
    private bool _hasTakenExtraTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner.Player;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        _hasTakenExtraTurn = true;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || !_hasTakenExtraTurn)
        {
            return;
        }

        _hasTakenExtraTurn = false;
        await PowerCmd.Decrement(this);
    }

    /// <summary>
    ///     炁婴接管回合: 自动把手牌里所有可打出的牌依次打出, 然后结束该回合。
    /// </summary>
    public override async Task AfterAutoPrePlayPhaseEnteredLate(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player || Owner.Player == null)
        {
            return;
        }

        var combatState = player.Creature.CombatState;
        Flash();

        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
        {
            var cardsPlayed = 0;
            for (; cardsPlayed < 30; cardsPlayed++)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    break;
                }

                if (CombatManager.Instance.IsPlayerReadyToEndTurn(player))
                {
                    break;
                }

                var hand = PileType.Hand.GetPile(Owner.Player).Cards;
                var card = hand.FirstOrDefault(c => c.CanPlay());
                if (card == null)
                {
                    break;
                }

                var target = GetTarget(card, combatState!);
                await card.SpendResources();
                await CardCmd.AutoPlay(choiceContext, card, target, skipXCapture: true);
            }

            if (cardsPlayed == 0)
            {
                return;
            }

            PlayerCmd.EndTurn(Owner.Player, false);
        }
    }

    private Creature? GetTarget(CardModel card, ICombatState combatState)
    {
        var combatTargets = Owner.Player!.RunState.Rng.CombatTargets;
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => combatTargets.NextItem(
                combatState.Allies.Where(c => c is { IsAlive: true, IsPlayer: true } && c != Owner)),
            TargetType.AnyPlayer => Owner,
            _ => null,
        };
    }
}
