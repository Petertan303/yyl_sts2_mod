using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     生生不息: 1 费, 造成 5 点伤害。<b>本场战斗每获得过 1 点炁, 额外造成 2 → 3 点伤害</b>。
///     <para>
///         机制照搬储君「超质量体」(SUPERMASSIVE) —— 原版统计"本场战斗<b>生成过</b>的牌数"
///         (走 <c>CombatManager.History</c> 的<code>CardGeneratedEntry</code>),
///         我们换成伊林的炁: 由 <see cref="QiGainTracker.CombatAmount" /> 读本场战斗累计获炁量
///         (在 <c>yylCmd.GainQi</c> 内打标, 全 mod 唯一产炁入口, 计数用修正后实际入账的量)。
///         于是它是一张<b>局内成长</b>牌: 前期炁少伤害低, 后期炁堆起来后一发可期。
///     </para>
///     <para>
///         卡面: 伤害用 <c>WithDamage</c>(基础值), 加成用 <c>WithVar("PerQiBonus", 2, 1)</c>
///         声明 —— <b>刻意不用 <c>WithCalculatedDamage</c></b>: 那是"伤害变量"(带
///         <c>ValueProp.Move</c>), 会被「炁」的增伤乘区乘一遍, 卡面会显示错乱的数字。
///         <c>IntVar</c> 不带 ValueProp, 恒为 2 → 3。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShengShengBuXie(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShengShengBuXie() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(BaseDamage, 0);
        WithVar("PerQiBonus", PerQiBonus, PerQiBonusUpgrade);
    }

    /// <summary>基础伤害 (固定, 不随成长变化)。</summary>
    public const int BaseDamage = 5;

    /// <summary>每点本场获炁的额外伤害 (基础值)。</summary>
    public const int PerQiBonus = 2;

    /// <summary>每点获炁加成的升级增量: 2 → 3。</summary>
    public const int PerQiBonusUpgrade = 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 本场战斗累计获炁量 (含起手遗物给的炁 —— 与「崩拳」那套判定同源)。
        var qiGained = QiGainTracker.CombatAmount(Owner, Owner.Creature.CombatState);
        var perQi = DynamicVars["PerQiBonus"].IntValue;
        var total = DynamicVars.Damage.BaseValue + perQi * qiGained;

        await DamageCmd.Attack(total)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}