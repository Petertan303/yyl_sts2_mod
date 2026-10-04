using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     头脑风暴: 2 费, 对<b>所有敌人</b>造成 12 → 16 点伤害。
///     <b>你每获得 1 点炁, 此牌费用 -1</b> (打出后复原)。
///     <para>
///         减费/复原机制照搬故障机器人「火箭飞拳」(<c>ROCKET_PUNCH</c>):
///         覆写 <c>AfterCardGeneratedForCombat</c> —— 本 mod 里"生成牌"的唯一入口是
///         <c>QiGainTracker</c> 配套的产炁流程, 因此直接重写为
///         <c>AfterPowerAmountChanged</c>: <b>炁的数量每变化一次</b>(增或减)就 -1 费,
///         最多降到 0 费。打出后 <c>EnergyCost</c> 自动复原(引擎的
///         <c>AddUntilPlayed</c> 语义)。
///     </para>
///     <para>
///         ⚠ 与原版火箭飞拳的差别: 原版只在<b>生成状态牌</b>时减费(不是所有情况);
///         我们简化为"炁一变就减费", 因为伊林的炁本身就是主要资源, 语义更直观。
///         减费上限为 0(不会变成负费用)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TouNaoFengBao(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TouNaoFengBao() : this(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithDamage(BaseDamage, DamageUpgrade);
    }

    /// <summary>基础伤害。</summary>
    public const int BaseDamage = 12;

    /// <summary>伤害升级增量: 12 → 16。</summary>
    public const int DamageUpgrade = 4;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);
    }

    /// <summary>
    ///     炁的数量变化 → 此牌费用 -1 (照火箭飞拳 <c>AfterCardGeneratedForCombat</c> 的减费思路,
    ///     但改用炁作为触发源)。<c>reduceOnly: true</c> 保证<b>费用不会降到 0 以下</b>,
    ///     且因为用的是 <c>AddUntilPlayed</c>(<c>WhenPlayed</c> 过期), 打出后自动复原。
    /// </summary>

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource
        )
    {
        // 只有"炁"本身变化才减费。
        if (power is not Qi) return Task.CompletedTask;
    
        // 不给自己以外的来源减费(联机时队友的炁不该让你这张牌变便宜)。
        if (applier != null && applier != Owner.Creature) return Task.CompletedTask;
    
        // reduceOnly = true → 引擎保证不会把费用减成负数。
        EnergyCost.AddUntilPlayed(-1, reduceOnly: true);
        return Task.CompletedTask;
    }
}