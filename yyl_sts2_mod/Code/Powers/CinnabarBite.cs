using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Compatibility;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     丹噬: <b>获得炁时</b>, 对所有敌人造成 4 → 6 点伤害。
///     <para>
///         设计意图: 产炁牌本身偏防御, 缺输出 —— 丹噬把"获得炁"这个动作换成伤害,
///         让防转攻。层数即伤害值(同原版「荆棘」的写法), 多张丹噬叠加。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="IGainQi" />, 在每次获得炁后作为后续副作用结算。
///     </para>
/// </summary>
public sealed class CinnabarBite : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>卡牌默认给予的数值 (升级后 6)。</summary>
    public const int DefaultAmount = 4;

    /// <summary>升级时增加的数值。</summary>
    public const int UpgradeAmount = 2;

    public int ModifyQiGain(Player player, int amount)
    {
        // 不参与数值修改, 只挂 AfterModifyingQiGain 后续结算。
        return amount;
    }

    public async Task AfterModifyingQiGain(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        var damage = Amount;
        if (damage <= 0) return;

        foreach (var enemy in combatState.HittableEnemies)
        {
            await CompatibilityCreatureCmd.Damage(
                ctx, enemy, damage, default(ValueProp), cardSource: null!, cardPlay: null);
        }
    }
}
