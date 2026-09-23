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
using yyl_sts2_mod.Code.Cards.Basic;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

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
        // 主伤害 = 3 → 4 (基础 3, 升级 +1); 金光护体 +1 在 OnPlay 内结算。
        WithDamage(3, 1);
        WithVars(new RepeatVar(5));
        // 卡面 {Bonus} = 普通 DynamicVar, 战斗外显示预期值 1。
        WithVar("Bonus", 1, 0);
        // 金光护体联动: 声明 -1 层供 ApplySelf 消耗 (与掌心雷同款写法)。
        WithPower<GoldenAegis>(-1);
        // 仅用于卡面显示: 这次要消耗几层
        WithPower<GoldenAegis>("AegisCost", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = DynamicVars.Repeat.IntValue;
        var damage = DynamicVars["Damage"].IntValue + (Owner.HasPower<GoldenAegis>() ? 1 : 0);
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
