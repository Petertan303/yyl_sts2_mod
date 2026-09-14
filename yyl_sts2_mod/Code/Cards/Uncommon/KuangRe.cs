using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     狂热: 1 费, 对单体造成 4 伤害, 若目标是奶龙, 额外 +4 伤害。
///     <para>
///         原设计意图是"持有时, 攻击奶龙 +4"; 当前简化为"打出此牌时, 目标若是奶龙则 +4"。
///         若需要持久效果,把它改成 "Power 狂热(单层): 攻击奶龙 +4",卡牌施加 Power。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class KuangRe(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public KuangRe() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        var damage = DynamicVars.Damage.IntValue + (yylNailong.IsNailong(target) ? 4 : 0);
        await CommonActions.CardAttack(this, cardPlay, target, damage, ValueProp.Move)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

}
