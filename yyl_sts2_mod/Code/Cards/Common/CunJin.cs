using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     寸劲: 1 费, 造成 6 → 9 点伤害。<b>你每有 1 点炁, 造成的伤害 +2 点</b>。
///     <para>
///         与「生生不息」的区别: 那张读<b>本场战斗累计获炁量</b>(只增不减, 越打越高),
///         本张读<b>当前持有的炁量</b>(花掉炁就立刻变弱) —— 因此它是一张
///         「把存量炁换成爆发」的牌, 与「食炁」「天火」这类耗炁牌形成配合/制约关系。
///     </para>
///         数值参照「一人之下」里的<em>寸劲</em>: 极短距离内爆发寸力的劲, 一击即穿。
///     机制上刻意<b>不加技能/金光联动</b>, 作为一张纯粹的"存量换伤害"基准牌。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class CunJin(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public CunJin() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(BaseDamage, DamageUpgrade);
        WithVar("PerQi", PerQiBonus, PerQiUpgrade);
    }

    /// <summary>基础伤害。</summary>
    public const int BaseDamage = 6;

    /// <summary>伤害升级增量: 6 → 9。</summary>
    public const int DamageUpgrade = 3;

    /// <summary>每点持有炁的加伤 (基础)。</summary>
    public const int PerQiBonus = 2;

    /// <summary>每点炁加伤的升级增量: 2 → 4。</summary>
    public const int PerQiUpgrade = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // ★当前持有的炁量(不是本回合获得的量) —— 耗炁牌会让这张牌变弱。
        var qi = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        var total = DynamicVars.Damage.BaseValue + DynamicVars["PerQi"].IntValue * qi;

        await DamageCmd.Attack(total)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .Execute(choiceContext);
    }
}