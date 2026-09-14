using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Ancient;

/// <summary>
///     白长虫: 先古卡, 掌心雷「入阴」之后的形态 (原「阴五雷」)。
///     对所有敌人造成 2 → 3 点伤害 5 次, 无视格挡, 并挂 1 → 2 层易伤与虚弱;
///     若身上有金光护体, 消耗 1 层使本次伤害 +1。
///     <para>
///         阴雷走内伤、穿透: 同样的一发雷, 比掌心雷更重且不吃格挡。
///         由 <c>PalmThunderUpgradePatch</c> 在掌心雷被升级时变形而来,
///         因此正常游玩中不会与掌心雷同时出现。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class WhiteWorm(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public WhiteWorm() : this(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        // 变量名仍叫 Damage, 但带上 Unblockable —— 阴雷吃"内伤", 不吃格挡。
        // (AttackCommand 没有 WithValueProp, 伤害的 ValueProp 只能在声明伤害变量时给。)
        WithCalculatedDamage("Damage", 2, (_, _) => 0m, ValueProp.Unblockable, 1, 0);
        WithVars(new RepeatVar(5));
        WithPower<VulnerablePower>(1, 1);
        WithPower<WeakPower>(1, 1);

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
