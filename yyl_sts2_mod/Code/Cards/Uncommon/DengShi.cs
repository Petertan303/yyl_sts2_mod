using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     瞪视: 1 费, 造成 3 → 5 点伤害。<b>本回合你对该敌人的伤害翻倍</b>。
///     <para>
///         引入一个<b>独立增伤乘区</b>(不是复用「炁」那条线): 由隐藏 power
///         <see cref="DazzledTarget" /> 承载, 只对<b>被选中的那一个敌人</b>翻倍,
///         换目标即失效, 目标死亡则立即消失。
///     </para>
///     <para>
///         为什么单独造一个乘区: 全库的增伤都走「炁」(按持有量对数增伤) 与「驭龙」(对奶龙)。
///         再叠一层<b>时限性 + 单目标</b>的乘区, 能让「盯着某只怪集火」成为一个可行策略,
///         而不会被「炁」的全局放大掩盖(否则 ×2 会被炁的乘区再乘一遍, 收益不可控)。
///     </para>
///     <para>
///         ★★ <b>2026-10-07 两次返工后定稿: 施加 power 一律走泛型
///         <c>PowerCmd.Apply&lt;T&gt;(...)</c></b>, 与全库其他卡(定身/化劲/聚炁/金光…)完全一致。
///         泛型重载内部就是 <c>ModelDb.Power&lt;T&gt;().ToMutable()</c>(无参)+ <c>Apply(amount)</c>,
///         由引擎负责实例化时机 —— <b>绝不要在卡里手动 ToMutable</b>:
///         <list type="bullet">
///             <item><c>ToMutable(非0初值)</c> → <c>Amount</c> setter 走
///             <c>PowerModel.SetAmount</c>, 里面 <c>Owner.InvokePowerModified(...)</c> 时
///             <c>Owner</c>仍是 null(要等 <c>ApplyInternal</c> 才赋值) ⇒ NullReferenceException。
///             卡牌异常 ⇒ 卡在待打出区。★这就是引擎自己用无参 <c>ToMutable()</c> 的原因。</item>
///             <item>手动 <c>ToMutable()</c> 还会绕过 <c>FindExistingInstanceForStacking</c>,
///             重复打出时会得到多个实例、增伤乘区叠乘。</item>
///         </list>
///         为此 power 改成<b>挂在被打的那个敌人身上</b>(而不是玩家), 这样
///         <see cref="DazzledTarget.ModifyDamageMultiplicative" /> 只要判断
///         「攻击者是不是它的 Owner」即可, <b>无需保存目标引用</b>,
///         也就无需在 Apply 之前注入任何自定义数据。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DengShi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DengShi() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(BaseDamage, DamageUpgrade);
    }

    /// <summary>基础伤害 (卡面显示值)。</summary>
    public const int BaseDamage = 3;

    /// <summary>伤害升级增量: 3 → 5。</summary>
    public const int DamageUpgrade = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 先打伤害, 再挂"盯着他"增伤 —— 保证本次伤害不被自己的 buff 额外翻倍。
        await DamageCmd.Attack(DynamicVars.Damage.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // ★ 与全库其他卡一致: 泛型 Apply, 引擎负责实例化(幂等, 已有实例则走 ModifyAmount 加层)。
        //   挂在**被打的敌人**身上 —— 增伤乘区靠 "dealer == Owner" 判定, 不需要保存目标引用。
        //   同一目标重复打出时 FindExistingInstanceForStacking 会命中已有实例并加层,
        //   叠到第 2 层时下面按"单目标只生效一次"处理: 见 DazzledTarget 的语义说明。
        await PowerCmd.Apply<DazzledTarget>(
            choiceContext, target, 1m, Owner.Creature, cardPlay.Card);
    }
}