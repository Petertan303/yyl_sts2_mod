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
using yyl_sts2_mod.Code.Utils;
using Godot;

namespace yyl_sts2_mod.Code.Cards.Ancient;

/// <summary>
///     白长虫: 先古卡 (原「阴五雷」), 与掌心雷对应的 ancestral 形态。
///     对所有敌人造成 2 → 3 点伤害 5 次 (普通伤害, 吃格挡), 并挂 2 → 3 层易伤与虚弱;
///     若身上有金光护体, 消耗 1 层使本次伤害 +1。
///     <para>
///         阴雷比掌心雷更重 (2→3 vs 1→2 每段), 但仍走普通伤害管线 —— 声明与结算
///         都带 <c>ValueProp.Move</c>, 才能吃到力量/易伤等常规攻击加成 (⚠不能传
///         <c>ValueProp.None</c>: 那不算"攻击", 力量等加成会全部失效)。
///         白长虫是独立卡牌 (见构造函数里的独立升级 2 → 3),
///         本体通过「古老牙齿」(Ancient Tooth) 遗物从掌心雷转化而来,
///         不应在普通升级流程中互相变形。
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
        // 伤害变量名仍叫 Damage; props = Move (普通攻击, 吃格挡也吃力量/易伤)。
        // 主伤害 = 2 → 3 (基础 2, 升级 +1); 金光护体每段 +1 在 OnPlay 内结算。
        // 卡面 {Damage}=2 / {Bonus}=1 为普通 DynamicVar, 战斗外也能正确显示 (不再 0/花括号)。
        WithDamage(2, 1);
        WithVars(new RepeatVar(5));
        // 消耗金光护体时每段额外伤害 (卡面用): 普通 DynamicVar, 战斗外显示预期值 1。
        WithVar("Bonus", 1, 0);
        WithPower<VulnerablePower>(2, 1);
        WithPower<WeakPower>(2, 1);

        WithPower<GoldenAegis>(-1);
        // 仅用于卡面显示: 这次要消耗几层
        WithPower<GoldenAegis>("AegisCost", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<VulnerablePower>(choiceContext, enemies, this);
        await CommonActions.Apply<WeakPower>(choiceContext, enemies, this);

        // 先结算攻击: 基础伤害 + 金光护体每段 +1 (卡面 {Damage}=2/{Bonus}=1 为预期值)。
        var damage = DynamicVars["Damage"].IntValue + (Owner.HasPower<GoldenAegis>() ? 1 : 0);
        foreach (var enemy in CombatState?.HittableEnemies ?? [])
        {
            if (enemy == null || !enemy.IsHittable) continue;
            // 五条光束上下并列同时发射 (演出), 结算仍为一次 5 段攻击。
            yylVfx.KinBeamColumn(Owner.Creature, DynamicVars.Repeat.IntValue, flipX: true);
            await CommonActions.CardAttack(this, cardPlay, enemy, damage,
                    ValueProp.Move, hitCount: DynamicVars.Repeat.IntValue)
                .Execute(choiceContext);
        }

        // 攻击结算完再消耗 1 层金光护体 (这样本次攻击已经吃到 +1, 消耗发生在之后)。
        if (Owner.HasPower<GoldenAegis>())
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}
