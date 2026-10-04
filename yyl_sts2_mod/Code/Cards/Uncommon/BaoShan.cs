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
///     抱山: 2 费, 造成 10 → 13 点伤害。<b>你下一张打出的技能牌获得双倍格挡</b>。
///     <para>
///         立意取「土宗伊芙以土攻守兼备」—— 攻击与防御用同一张牌串联:
///         打完之后手上正好有一张技能牌要打, 把它格挡翻倍, 等于"攻击 + 防御"打包成一张牌。
///     </para>
///     <para>
///         该 buff 由隐藏 power <see cref="NextSkillDoubleBlock" /> 承载, 走引擎的
///         <c>ModifyBlockMultiplicative</c> 乘区钩子(与原版「坚定不移」同款思路):
///         技能牌结算时 ×2 后自动失效, 因此**只影响下一张技能牌**, 无需监听出牌事件。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BaoShan(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BaoShan() : this(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        // 攻击结算完再挂 buff —— 不管有没有打中人, buff 都给(它是"防御"而非"奖励")。
        await PowerCmd.Apply<NextSkillDoubleBlock>(
            choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
}