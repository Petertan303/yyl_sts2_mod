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
///     太极: 敌方对你的<b>真实攻击</b>原封不动转嫁给一名随机敌人 (借力打力)。
///     <para>
///         [重做 2026-09-20 晚] 主路径 = <see cref="Patches.TaiChiRedirectPatch" /> /
///         <see cref="Patches.TaiChiRedirectMultiPatch" /> 的 Harmony 目标改写:
///         敌方攻击的目标被改写成随机敌人, 伤害数值/格挡结算/易伤等<b>原封不动</b>,
///         你不掉血, 同时把 dealer 置 null —— 反汇编确认蜂群术士的蜂巢
///         (PersonalHivePower, 受攻击时给攻击者塞晕眩牌) 对 dealer==null 的伤害
///         直接跳过, 不会重演"往怪物手里塞牌→卡死"。
///     </para>
///     <para>
///         ★两条铁律 (都来自实战卡死):
///         ① 重定向必须伴随 dealer=null —— 保留原攻击者等于"怪物攻击怪物";
///         ② 只转嫁 <see cref="ValuePropExtensions.IsPoweredAttack" /> 且敌方来源的
///         伤害 —— 中毒/状态/自伤类一律不动。
///     </para>
///     <para>
///         下面的 <see cref="AfterDamageReceived" /> 事件转移 (倒映式) 保留作
///         未覆盖路径的兜底: 重定向成功时玩家掉血为 0, 该钩子因 target != Owner
///         自然跳过, 两条路径互斥。回合结束 (敌方回合收尾) 自动移除,
///         保护窗口 = 打出后的本回合 + 敌方回合。
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
