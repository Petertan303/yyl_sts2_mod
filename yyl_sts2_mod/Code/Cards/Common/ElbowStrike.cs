using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     顶肘: 1 费, 造成 5 → 8 点伤害; 若目标身上有易伤, 额外再打一段。
///     与掌心雷 / 破绽 / 引雷这类"铺易伤"的牌联动: 易伤铺好后, 这张牌的价值翻倍。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ElbowStrike(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ElbowStrike() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        // 额外的一段: 有易伤时本次攻击命中两次 (伤害按每段结算, 因此吃满易伤加成)。
        var hitCount = target.HasPower<VulnerablePower>() ? 2 : 1;
        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue,
                ValueProp.Move, hitCount: hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
