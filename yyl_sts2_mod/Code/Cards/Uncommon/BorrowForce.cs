using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     借力: 1 费, 造成 7 → 9 点伤害; 若目标身上有虚弱, 改为 12 → 15 点。
///     与顶肘 (易伤 → 加一段) 成对设计: 虚弱流的即时回报卡。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BorrowForce(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BorrowForce() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // 主伤害 = 7 → 9, 活 calc: 目标有虚弱时改为 12 → 15, 预览与结算同源。
        WithCalculatedDamage("Damage", 7,
            (card, creature) => creature != null && creature.HasPower<WeakPower>() ? 5 : 0,
            default(ValueProp), 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        // 伤害变量自带虚弱条件加成 (活 calc, 预览指向虚弱敌人时显示 12)。
        // ⚠ 用弱类型访问器: 强类型 DynamicVars.Damage 对 calc 变量会抛 InvalidCastException。
        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars["Damage"].IntValue,
                ValueProp.Move)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
