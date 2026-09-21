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
///     对所有敌人造成 2 → 3 点伤害 5 次, 无视格挡, 并挂 1 → 2 层易伤与虚弱;
///     若身上有金光护体, 消耗 1 层使本次伤害 +1。
///     <para>
///         阴雷走内伤、穿透: 同样的一发雷, 比掌心雷更重且不吃格挡。
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
        // 伤害变量名仍叫 Damage, 但带上 Unblockable —— 阴雷吃"内伤", 不吃格挡。
        // (AttackCommand 没有 WithValueProp, 伤害的 ValueProp 只能在声明伤害变量时给。)
        //
        // 注意: CalculatedDamageVar 没有可写的 BaseValue 属性, 因此"金光护体 +1 伤害"
        // 不能像掌心雷那样 DynamicVars.Damage.BaseValue += 1 —— 那会在拥有金光护体时
        // 抛异常, 导致卡牌卡在待打出区、不生效。改为在 calc 里根据"当前是否拥有金光护体"
        // 返回 +1 / 0, 由攻击结算时按次读取。
        WithCalculatedDamage(
            "Damage",
            2,
            (card, _) => card.Owner.HasPower<GoldenAegis>() ? card.DynamicVars["Bonus"].IntValue : 0m,
            ValueProp.Unblockable,
            1,
            0);
        WithVars(new RepeatVar(5));
        // 消耗金光护体时每段额外伤害 (卡面用)
        WithCalculatedDamage("Bonus", 1, (_, _) => 0m, 0, 0, 0);
        WithPower<VulnerablePower>(1, 1);
        WithPower<WeakPower>(1, 1);

        WithPower<GoldenAegis>(-1);
        // 仅用于卡面显示: 这次要消耗几层
        WithPower<GoldenAegis>("AegisCost", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<VulnerablePower>(choiceContext, enemies, this);
        await CommonActions.Apply<WeakPower>(choiceContext, enemies, this);

        // 先结算攻击: calc 会在读取伤害时根据"是否拥有金光护体"给出 +1/次。
        // ⚠ 同掌心雷 (2026-09-20): 必须显式传数值 + 弱类型访问器 DynamicVars["Damage"] ——
        // 无参 CardAttack / 强类型 DynamicVars.Damage 对 calc 变量会抛 InvalidCastException。
        var damage = DynamicVars["Damage"].IntValue;
        var hits = DynamicVars.Repeat.IntValue;
        foreach (var enemy in CombatState?.HittableEnemies ?? [])
        {
            if (enemy == null || !enemy.IsHittable) continue;
            // 连射演出: 每段从角色中线附近的随机偏移处发射一道光束 (照搬炁冲), 段间 0.3s。
            for (var i = 0; i < hits; i++)
            {
                yylVfx.KinBeam(Owner.Creature, flipX: true, positionOffset:
                    new Vector2(0, Random.Shared.Next(-90, 30)));
                await CommonActions.CardAttack(this, cardPlay, enemy, damage, ValueProp.Unblockable)
                    .Execute(choiceContext);
                if (i < hits - 1)
                    await Cmd.Wait(0.1f, false);
            }
        }

        // 攻击结算完再消耗 1 层金光护体 (这样本次攻击已经吃到 +1, 消耗发生在之后)。
        if (Owner.HasPower<GoldenAegis>())
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}
