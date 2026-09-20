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
///     撩掌: 1 费, 造成 6 → 8 点伤害; 若你有金光护体, 改为 9 → 12 点 (不消耗护体)。
///     金光护体的第二种用法: 掌心雷是"消耗换加伤", 这张是"持有即有加伤",
///     让站着挨打的回合也能把防御资源换成输出。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class RisingPalm(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public RisingPalm() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // 主伤害 = 6 → 8, 活 calc: 有金光时改为 9 → 12, 预览与结算同源。
        WithCalculatedDamage("Damage", 6,
            (card, _) => card.Owner.Creature.HasPower<GoldenAegis>() ? 3 : 0, default(ValueProp), 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        // 伤害变量自带金光加成 (活 calc, 与预览同源); 不消耗护体。
        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue,
                ValueProp.Move)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
