using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
// using STS2RitsuLib.Interop.AutoRegistration;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     掌心雷: 初始牌 (原「阳五雷」)。
///     1 费, 造成 3 → 4 点伤害 2 次, 并挂 1 → 2 层易伤 (无虚弱)。
///     作为易伤铺点, 不再做 AoE / 金光护体联动, 以降超模。
///     白长虫 (WhiteWorm) 是独立先古卡, 通过「古老牙齿」获得, 不在升级时变形。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
// 若日后接「古老牙齿」(Ancient Tooth) 遗物，应在此注册对应先古牌:
// [RegisterArchaicToothTranscendence(typeof(WhiteWorm))]
#pragma warning disable STS004
public sealed class PalmThunder(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public PalmThunder() : this(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithVars(new RepeatVar(2));
        WithPower<VulnerablePower>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        await CommonActions.Apply<VulnerablePower>(choiceContext, new[] { target }, this);
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
