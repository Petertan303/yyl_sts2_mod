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
///     扫堂腿: 1 费, 造成 5 → 7 点伤害, 给予 1 层虚弱。
///     普通位的"带一点功能性"的攻击: 伤害低于同费裸卡, 换来一层虚弱。
///     升级只加伤害, 不再增加虚弱层数 —— 虚弱浓度需要严格收口。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class SweepingLeg(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public SweepingLeg() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithPower<WeakPower>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, new[] { target }, this);
    }
}
