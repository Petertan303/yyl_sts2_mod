using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Ancient;

/// <summary>
///     辟邪剑法: 先古卡, 由先古 NPC 赠予 (坦克斯最合适 —— 台词里他就在找武器)。
///     造成 8 → 11 点伤害; 若目标身上带有任何负面状态 (易伤 / 虚弱 / 中毒 / 奶龙),
///     本次伤害翻倍。
///     <para>
///         「辟邪」= 专克邪祟: 目标越脏, 这一剑越重。
///         和现有的铺 debuff 体系 (掌心雷 / 风后奇门 / 破防 / 投喂) 天然闭环,
///         是先古卡里"越到中后期越强"的那一张。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class WardingBlade(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public WardingBlade() : this(2, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (HasAnyDebuff(cardPlay.Target))
        {
            DynamicVars.Damage.BaseValue *= 2;
        }

        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>目标是否带有任一可被"辟邪"利用的负面状态。</summary>
    private static bool HasAnyDebuff(Creature? target)
    {
        if (target is not { IsAlive: true }) return false;
        return target.HasPower<VulnerablePower>()
               || target.HasPower<WeakPower>()
               || target.HasPower<PoisonPower>()
               || yylNailong.IsNailong(target);
    }
}
