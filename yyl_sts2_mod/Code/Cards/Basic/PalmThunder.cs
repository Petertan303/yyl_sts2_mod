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

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     掌心雷: 初始牌 (原「阳五雷」)。
///     对所有敌人造成 1 → 2 点伤害 5 次, 并挂 1 层易伤与虚弱;
///     若身上有金光护体, 消耗 1 层使本次伤害 +1。
///     <para>
///         张楚岚的招牌雷法。升级后不再是数值强化, 而是「入阴」——
///         雷法转入阴面, 化为白长虫 (见 <see cref="Ancient.WhiteWorm"/>),
///         由 <c>PalmThunderUpgradePatch</c> 在执行升级时完成变形。
///         原作里阳五雷与阴五雷本就互斥, 用"同一张牌的先后形态"来表达这层关系。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class PalmThunder(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public PalmThunder() : this(1, CardType.Attack, CardRarity.Basic, TargetType.AllEnemies)
    {
        WithDamage(1, 1);
        WithVars(new RepeatVar(5));
        WithPower<VulnerablePower>(1);
        WithPower<WeakPower>(1);

        WithPower<GoldenAegis>(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<VulnerablePower>(choiceContext, enemies, this);
        await CommonActions.Apply<WeakPower>(choiceContext, enemies, this);

        if (Owner.HasPower<GoldenAegis>())
        {
            DynamicVars.Damage.BaseValue += 1;
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
        }

        await CommonActions.CardAttack(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
