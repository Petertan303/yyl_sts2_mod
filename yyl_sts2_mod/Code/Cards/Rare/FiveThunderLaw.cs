using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     五雷正法: 3 费, 对所有敌人造成 3 → 4 点伤害 5 次;
///     若身上有金光护体, 消耗 1 层使本次每段伤害 +1 (与掌心雷/白长虫同款联动)。
///     <para>
///         [balance 2026-09-19] 用户定调: 由 18 → 24 单段改为 5 段小伤害 + 金光联动,
///         与掌心雷形成"单点 / 全场"的镜像: 同一套金光消耗, 一个打单体一个打群体。
///         结算顺序同掌心雷: 先攻击 (吃到加成), 后消耗金光护体。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class FiveThunderLaw(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public FiveThunderLaw() : this(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        // 主伤害 = 3 → 4, 活 calc: 有金光时预览也显示 +1 (白长虫模式)。
        WithCalculatedDamage("Damage", 3,
            (card, _) => card.Owner.Creature.HasPower<GoldenAegis>() ? 1 : 0, default(ValueProp), 1, 0);
        WithVars(new RepeatVar(5));
        // 金光护体联动: 声明 -1 层供 ApplySelf 消耗 (与掌心雷同款写法)。
        WithPower<GoldenAegis>(-1);
        // 仅用于卡面显示: 这次要消耗几层
        WithPower<GoldenAegis>("AegisCost", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 伤害变量自带金光加成 (活 calc, 与预览同源)。
        // ⚠ 同掌心雷: 显式传数值 + 弱类型访问器 DynamicVars["Damage"] ——
        // 强类型 DynamicVars.Damage 对 calc 变量会抛 InvalidCastException (卡死在待打出区)。
        var hits = DynamicVars.Repeat.IntValue;
        var damage = DynamicVars["Damage"].IntValue;
        // 五雷 = 五道光束齐射扫过敌阵 (一次, 不随敌人数重复)。
        yylVfx.KinBeamColumn(Owner.Creature, hits, flipX: true);
        foreach (var enemy in CombatState?.HittableEnemies ?? [])
        {
            if (enemy == null || !enemy.IsHittable) continue;
            await CommonActions.CardAttack(this, cardPlay, enemy, damage,
                    ValueProp.Move, hitCount: hits)
                .WithHitFx("vfx/vfx_attack_lightning")
                .Execute(choiceContext);
        }

        // 攻击结算完再消耗 1 层金光护体。
        if (Owner.HasPower<GoldenAegis>())
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}
