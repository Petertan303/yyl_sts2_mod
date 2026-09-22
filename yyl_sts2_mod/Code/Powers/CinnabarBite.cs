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
///     丹噬: <b>获得炁时</b>, 对所有敌人造成「本次获得的炁量 × 层数」点伤害。
///     <para>
///         设计意图: 产炁牌本身偏防御, 缺输出 —— 丹噬把"获得炁"这个动作换成伤害,
///         让防转攻。★2026-09-21 用户定调: 伤害<b>跟随实际获得的炁量</b> (不再是固定的
///         4 → 6), 层数作为<b>倍率</b> —— 攒一大笔炁 (炁流源体 / 大瓶黄桃罐头 / 投喂)
///         再吃下去才是这张卡的正确用法, 与「天火」(结算时倾泻全部炁) 形成两条路线。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="IGainQi" />, 在每次获得炁后作为后续副作用结算。
///     </para>
/// </summary>
public sealed class CinnabarBite : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>卡牌默认给予的数值 (基础 1 层, 升级后 2 层 — 2026-09-21 用户下调)。</summary>
    public const int DefaultAmount = 1;

    /// <summary>升级时增加的数值 (1 → +1 = 2 层)。</summary>
    public const int UpgradeAmount = 1;

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

        // ★伤害 = 本次获得的炁量 × 丹噬层数 (2026-09-21 用户定调)。
        var damage = (decimal)modifiedAmount * Amount;
        if (damage <= 0) return;

        foreach (var enemy in combatState.HittableEnemies)
        {
            await CompatibilityCreatureCmd.Damage(
                ctx, enemy, damage, default(ValueProp), cardSource: null!, cardPlay: null);
        }
    }
}
