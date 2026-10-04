using System.Linq;
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
///     食炁: 1 费, 造成 26 → 32 点伤害。<b>消耗 3 点炁</b>; 若此击<b>击杀</b>敌人, <b>获得 6 点炁</b>。
///     <para>
///         机制照搬储君「决胜一击」(<c>KnockoutBlow</c>: 3 费 30 伤, 击杀后获 5 星),
///         把"星"换成伊林的「炁」, 并把费用压到 1 费 + 改为**消耗炁**——
///         形成"高伤爆发 → 斩杀 → 回炁"的循环, 与「大啖食粮」(击杀回血) 同一思路,
///         但走的是**资源回收**而非治疗。
///     </para>
///     <para>
///         ★耗炁用<b>「不足则少给效果」</b>的既定约定(与天火/散炁同款), 不做"改失血"。
///         炁不足 3 点时: 跳过耗炁, 伤害打折为"不耗炁时的基础值"。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShiQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShiQi() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(BaseDamage, DamageUpgrade);
        WithVar("QiCost", QiCost, 0);
        WithVar("QiRefund", QiRefund, QiRefundUpgrade);
    }

    /// <summary>基础伤害 (炁足够时的全额值)。</summary>
    public const int BaseDamage = 26;

    /// <summary>升级伤害增量: 26 → 32。</summary>
    public const int DamageUpgrade = 6;

    /// <summary>击杀需要消耗的炁量 (固定, 升级不变)。</summary>
    public const int QiCost = 3;

    /// <summary>击杀后返还的炁量 (基础)。</summary>
    public const int QiRefund = 6;

    /// <summary>返还炁的升级增量: 6 → 9。</summary>
    public const int QiRefundUpgrade = 3;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        var qi = Owner.Creature.GetPower<Powers.Qi>()?.Amount ?? 0;
        var cost = DynamicVars["QiCost"].IntValue;

        // ★不足则少给效果(既定约定): 炁不够就不耗炁, 但伤害打折(下限基础值的 60%)。
        var hasQi = qi >= cost;
        var damage = hasQi
            ? DynamicVars.Damage.BaseValue
            : DynamicVars.Damage.BaseValue * 6 / 10;

        if (hasQi)
        {
            await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
        }

        var killed = (await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_blunt", null, "heavy_attack.mp3")
                .Execute(choiceContext))
            .Results.SelectMany(r => r)
            .Any(r => r.WasTargetKilled);

        if (!killed) return;

        // 击杀 ⇒ 返还炁 (照 KnockoutBlow 的"击杀才给资源"结构)。
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["QiRefund"].IntValue, this, cardPlay.Card);
    }
}