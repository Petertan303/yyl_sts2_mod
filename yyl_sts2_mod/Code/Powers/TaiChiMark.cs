using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     太极: 持有者<b>未被格挡的伤害</b>转移到一名随机敌人 (借力打力)。
///     <para>
///         [重做 2026-09-20] 参考原版「倒映」(ReflectPower) 用 <b>AfterDamageReceived</b>
///         事件钩子: 玩家每受到一次敌方来源的真实攻击伤害, 取
///         <see cref="DamageResult.UnblockedDamage" /> (穿过格挡的部分), 先把等量生命
///         补回玩家, 再把这笔伤害以<b>无来源</b>拍给随机敌人 (Unblockable, 足额落地)。
///     </para>
///     <para>
///         ★两个必须遵守的约束 (都来自实战卡死):
///         ① <b>不保留 Harmony 目标改写补丁</b> —— 改写攻击目标会让"怪物攻击怪物",
///         触发蜂群术士蜂巢 (PersonalHive, 受攻击时给攻击者塞晕眩牌) 这类
///         假设"攻击者是玩家"的怪物 Power, 往怪物手里塞牌直接卡死;
///         ② 转移伤害<b>无来源 + 只响应 IsPoweredAttack</b> —— 反噬/Power 伤害
///         (非 Move) 不会再次进入本钩子, 且无来源不触发按来源反制的 Power。
///         回合结束 (敌方回合收尾) 自动移除, 保护窗口 = 打出后的本回合 + 敌方回合。
///     </para>
/// </summary>
public sealed class TaiChiMark : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>事件式转移 (Reflect 同款): 玩家受到敌方伤害后, 把未格挡部分转嫁随机敌人。</summary>
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Amount <= 0) return;
        // 只对"自己挨打"生效。
        if (target != Owner) return;
        // 只转嫁敌方来源 (dealer 为空 = 中毒/状态牌/自伤类, 不转嫁)。
        if (dealer == null || dealer.Side == Owner.Side) return;
        // ★只转嫁【真实攻击】的伤害 (原版倒映 ReflectPower 同款过滤器)。
        //   反噬/Power 产生的伤害不是 Move, 不会再次进入本钩子 ——
        //   蜂群术士 (ENTOMANCER) "受伤→反噬→再转移→再反噬"死循环的修复点。
        if (!ValuePropExtensions.IsPoweredAttack(props)) return;

        var hpLost = result.UnblockedDamage;
        if (hpLost <= 0) return;
        // 已被打死就不救了 (死亡结算先于本钩子; 救活语义过强)。
        if (Owner.IsDead) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;
        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable)
            .ToList();
        if (candidates.Count == 0) return;
        var victim = candidates[Random.Shared.Next(candidates.Count)];

        // 先把玩家补回等量生命 (等效于这次掉血被"搬走"), 再把伤害拍给替身。
        // ⚠ 转移伤害必须【无来源】(dealer=null, 2026-09-20 实测):
        //   若保留原攻击者为来源, 当替身恰好是攻击者本身、且其带有荆棘类
        //   "伤害来源"反制 Power 时, 会 自伤 → 再触发 → 死循环 (蜂群术士 ENTOMANCER 卡死)。
        //   null 来源同时保证不会递归进入本钩子 (本钩子对 dealer==null 直接跳过)。
        await CreatureCmd.Heal(Owner, hpLost);
        await CreatureCmd.Damage(choiceContext, victim, hpLost, ValueProp.Unblockable, null, null, null);
    }

    /// <summary>敌方回合收尾时散去 (保证敌方回合内的攻击已被转移)。</summary>
    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext ctx,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side) return Task.CompletedTask;
        if (Amount <= 0) return Task.CompletedTask;
        return PowerCmd.Remove(this);
    }
}
