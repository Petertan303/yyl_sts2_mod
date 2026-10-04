using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
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

        // 已有的盯视 power 若还在(同一目标重复打出), 先移除再挂, 避免叠乘区。
        foreach (var existing in Owner.Creature.GetPowerInstances<DazzledTarget>().ToList())
            await PowerCmd.Remove(existing);

        var dazzle = new DazzledTarget(target);
        await PowerCmd.Apply(choiceContext, dazzle, Owner.Creature, 1m, Owner.Creature, this);
    }
}