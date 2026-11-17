using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     炁婴: 「老农功」的具现 —— 危急时刻接管宿主身体的战斗人格。
///     <para>
///         机制照搬 LexNinja2 的「罗汉分身」(TwoMonksPower): 该能力让你多获得一个回合,
///         这个回合由系统自动打出所有可打出的牌, 打完即结束回合并消耗 1 层。
///     </para>
///     <para>
///         [2026-11-08] 配套原版「疾影」(BlurPower): 打出「老农功」时额外施加 1 层,
///         让格挡从当前回合保留到炁婴接管的额外回合。施加点在
///         <see cref="Cards.Rare.PeasantDrill" /> 的 OnPlay, 不依赖本 power 的生命周期钩子。
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
        if (player != Owner.Player)
        {
            return false;
        }

        /*  ★2026-11-16 无敌人判断器(用户定调): 克隆老农功会让炁婴在额外回合自动打出
            其它老农功, 连锁触发更多额外回合; 若敌人已全灭(典型: 带复活机制的敌人死亡后
            要等正常回合流程复活), 空转的额外回合会让复活流程一直没机会触发。
            这里在引擎询问「是否再给一个额外回合」时检查: 场上已无可攻击的敌人 → 不再
            兑换, 回合按正常流程直接结束(SwitchSides), 炁婴层数原样保留 —— 引擎不调
            AfterTakingExtraTurn, AfterSideTurnEnd 的递减也以 _hasTakenExtraTurn 为门槛,
            两者都不会走到; 敌人复活或下回合出现敌人后可继续兑换。
            注: 无法用 PlayersTakingExtraTurn 判断「当前是额外回合链」—— 引擎在每次询问前
                都会先 Clear 该列表(CombatManager.SwitchFromPlayerToEnemySide IL 已核实),
                所以按「场上无可攻击的敌人」直接拦截, 与额外回合链场景语义等价且更稳:
                正常回合结束时敌人全灭, 给额外回合同样是空转。 */
        try
        {
            var combatState = player.Creature?.CombatState;
            if (combatState != null && !combatState.HittableEnemies.Any())
            {
                MainFile.Logger.Info("[yyl] 炁婴: 场上已无可攻击的敌人, 暂停兑换额外回合并保留层数。");
                return false;
            }
        }
        catch (Exception e)
        {
            // ★铁律: 钩子绝不抛异常。判断失败按原行为放行, 不影响额外回合。
            MainFile.Logger.Warn($"[yyl] 炁婴 ShouldTakeExtraTurn 无敌人判断异常, 按原行为放行: {e.Message}");
        }

        return true;
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

        // ★铁律: 钩子绝不抛异常。
        try
        {
            await PowerCmd.Decrement(this);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 炁婴层数递减失败, 已忽略: {e.Message}");
        }
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

        // ★铁律: 钩子绝不抛异常。自动出牌循环会打出任意卡牌, 任何一张牌的 OnPlay 抛异常
        // 都会直冲引擎并冻结整场战斗 —— 这里整体兜住, 异常只告警, 保证额外回合能安全收尾。
        try
        {
            var combatState = player.Creature.CombatState;
            Flash();

            // v0.113.0: CardSelectCmd.PushSelector 已移除。原版 WhisperingEarring 现在的写法是
            // RunManager.Instance.CardSelectorStack.PushSelector(player, selector)，返回的 scope 同样可 Dispose。
            using (RunManager.Instance.CardSelectorStack.PushSelector(player, new VakuuCardSelector()))
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
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 炁婴自动出牌回合异常, 已安全中止该额外回合: {e.Message}");
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
