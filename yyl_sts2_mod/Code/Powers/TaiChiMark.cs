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
///         [重做 2026-09-20] 旧实现 (Harmony 前缀改写 CreatureCmd.Damage 单体目标)
///         从未对敌方攻击生效 —— 敌方 AI 不走那 6 个单体重载。现参考原版「倒映」
///         (ReflectPower) 改用 <b>AfterDamageReceived</b> 事件钩子: 玩家每受到一次
///         敌方来源的伤害, 取 <see cref="DamageResult.UnblockedDamage" /> (穿过格挡的
///         部分), 先把等量生命补回玩家, 再以原攻击者为来源把这笔伤害拍给随机敌人
///         (Unblockable, 保证足额落地)。
///         Harmony 前缀 (<see cref="Patches.TaiChiRedirectPatch" />) 保留作"干净路径"
///         —— 若某次伤害真的走了 CreatureCmd.Damage 且被成功重定向, 玩家掉血为 0,
///         本事件钩子因 UnblockedDamage=0 自然不触发, 两条路径互斥不叠加。
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
