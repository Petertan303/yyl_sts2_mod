using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     太极: 敌方对你的<b>真实攻击</b>原封不动转嫁给一名随机敌人 (借力打力)。
///     <para>
///         ★ 2026-10-19 重写。玩家报"联机下太极表现为<b>先扣血后回血</b>" —— 那是旧
///         <see cref="AfterDamageReceived" /> 兜底路径的观感: 玩家先真的掉一次血, 再被
///         <c>CreatureCmd.Heal</c> 补回, 视觉上就是"掉下去又弹回来"。根因是它只是
///         <b>事后补救</b>: 主路径 (Harmony 改写 Damage 目标) 在联机下常常接管不上,
///         伤害已经结算完才轮到这个钩子。
///     </para>
///     <para>
///         现在改成<b>结算瞬间归零</b> (与金光同一套机制: 实现引擎内置的
///         乘算钩子, 命中时把这次伤害乘成 0), 玩家从头到尾<b>不掉血、不掉格挡</b>,
///         再把"欠下的"这笔伤害在 <see cref="AfterDamageReceived" /> 里原样拍给随机敌人。
///         ★ 治疗式兜底 (C 路径) 已删除 —— 先掉血再回血的观感差, 且存在
///         "结算瞬间到 Heal 之间被致死判定击杀"的窗口, 实战出过意外死亡 (用户定调)。
///     </para>
///     <para>
///         两条路径互斥, 可同时开着当双保险:
///         <list type="bullet">
///             <item>
///                 <b>主路径</b> = <see cref="Patches.TaiChiRedirectPatch" /> 的 Harmony 目标改写
///                 (联机下配合"任一玩家有印记全转"的团队语义)。命中时这次伤害的 <c>target</c>
///                 已经是敌人, 本类的乘算钩子因 <c>target != Owner</c> 直接放行。
///             </item>
///             <item>
///                 <b>归零路径</b> = 本类乘算钩子。主路径放弃时由它接管, 玩家不掉血,
///                 伤害转给随机敌人。
///             </item>
///         </list>
///         另外 <see cref="BeforeSideTurnEnd" /> 会在印记散去前补打未转移的挂起伤害,
///         防止引擎在"伤害为 0"时跳过 <see cref="AfterDamageReceived" /> 事件造成静默丢失。
///     </para>
///     <para>
///         ★ 两条铁律 (都来自实战卡死, 2026-09-20):
///         ① 转移出去的伤害必须<b>无来源</b> (dealer=null) —— 保留原攻击者等于"怪物攻击怪物",
///         蜂群术士 (ENTOMANCER) 的蜂巢会往怪物手里塞牌直接卡死;
///         ② 只转嫁 <see cref="ValuePropExtensions.IsPoweredAttack" /> 且敌方来源的伤害,
///         中毒/状态/自伤类一律不动。
///     </para>
/// </summary>
public sealed class TaiChiMark : yylPowerModel, IModifyDamageMultiplicative
{

    // ★★★ 2026-10-19: B 路径（乘算归零 + 快照转正 + AfterDamageReceived 转移 + 补打）
    // 整体停用, 只保留 A 主路径（Harmony 改写 Damage 目标, 见 Patches/TaiChiRedirectPatch）。
    // 理由: A 已覆盖 CreatureCmd.Damage 全部 10 个重载 + 守护全队语义, 覆盖所有转嫁场景;
    // 且 B 的乘算钩子会连敌人意图/卡面预览一起归零（意图显示 0 的来源）。
    // 保留接口声明: IModifyDamageMultiplicative 的 DIM 默认实现返回 1m ⇒ 本 power 对伤害中性。
    // ⚠ 若联机实测发现 A 有漏网（敌人攻击玩家却无 [太极] 重定向 日志）,
    //   恢复下方被注释的 B 路径即可（机制完整, 含快照转正污染修复）。
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

//    /// <summary>已转正、等待转嫁给替身的伤害总量 (由 BeforeDamageReceived 从快照转正)。</summary>
//    private decimal _pendingRedirect;


//    /// <summary>
//    ///     最近一次被归零的伤害快照。
//    ///     ★ [2026-10-19 晚] 不再直接累加 <see cref="_pendingRedirect" />：乘算钩子不只在
//    ///     真实结算时被调 —— <b>敌人意图计算</b>（AttackIntent.GetSingleDamage）与
//    ///     卡面预览走的是同一条 Hook.ModifyDamage 管线（两者 previewMode 都传 None，
//    ///     反汇编确认无法区分）。若在意图阶段就累加挂起量，转移数会被"从未发生的
//    ///     伤害"污染（双倍/空转移）。改为快照式：钩子只覆盖写快照，真实结算流程里的
//    ///     <see cref="BeforeDamageReceived" />（引擎在 Damage 命令内、ModifyDamage 之后、
//    ///     扣血之前调用，预览流程不会调它）把快照转正为挂起量 —— 意图阶段的快照
//    ///     永远等不到转正，会被真实结算覆盖，自然作废。
//    /// </summary>
//    private decimal _lastZeroed;


//    /// <summary>
//    ///     ★ 结算瞬间把这次伤害乘成 0 (与金光/心防同一个引擎接口, 机制同源)。
//    ///     返回 0 意味着玩家<b>既不掉血也不掉格挡</b> —— 不会再出现"先扣后回"。
//    /// </summary>
//    public decimal ModifyDamageMultiplicativeCompability(
//        Creature? target,
//        decimal amount,
//        ValueProp props,
//        Creature? dealer,
//        CardModel? cardSource,
//        CardPlay? cardPlay)
//    {
//        if (Amount <= 0 || amount <= 0) return 1m;
//        if (!IsProtectedTeammate(target)) return 1m;
//        if (!ShouldRedirect(props, dealer)) return 1m;
//
//        _lastZeroed = amount; // 覆盖式快照: 意图阶段的快照会被真实结算覆盖作废
//        MainFile.Logger.Info($"[太极] 伤害归零: {amount} 点 (快照待转正)");
//        return 0m;
//    }


//    /// <summary>
//    ///     真实伤害结算流程内 (ModifyDamage 之后、扣血之前) 把快照转正为挂起量。
//    ///     意图/卡面预览不会走到这里，快照自然作废 —— 这就是污染修复的核心。
//    /// </summary>
//    public override async Task BeforeDamageReceived(
//        PlayerChoiceContext choiceContext,
//        Creature target,
//        decimal amount,
//        ValueProp props,
//        Creature? dealer,
//        CardModel? cardSource)
//    {
//        if (IsProtectedTeammate(target) && _lastZeroed > 0)
//        {
//            _pendingRedirect += _lastZeroed;
//            MainFile.Logger.Info($"[太极] 快照转正: {_lastZeroed} 点 (待转移合计 {_pendingRedirect})");
//            _lastZeroed = 0m;
//        }
//        await base.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource);
//    }


//    /// <summary>
//    ///     把挂起的伤害原样拍给一名随机敌人 (无来源)。
//    ///     ★ 2026-10-19: <b>治疗式兜底路径已删除</b> (用户定调) —— 旧的"先 Heal 再转移"
//    ///     会造成"先扣血后回血"的观感, 且玩家在结算瞬间到 Heal 之间存在被
//    ///     致死判定击杀的窗口 (低血量意外死亡)。归零 (B) 本身已是完整兜底:
//    ///     乘算钩子在扣血前生效, 只要它命中玩家就不掉血。
//    /// </summary>
//    public override async Task AfterDamageReceived(
//        PlayerChoiceContext choiceContext,
//        Creature target,
//        DamageResult result,
//        ValueProp props,
//        Creature? dealer,
//        CardModel? cardSource)
//    {
//        if (Amount <= 0) return;
//        if (!IsProtectedTeammate(target)) return;
//        if (_pendingRedirect <= 0) return;
//
//        await FlushPending(choiceContext, "AfterDamageReceived");
//    }


    /// <summary>敌方回合收尾时散去 (保证敌方回合内的攻击已被转移)。</summary>
    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext ctx,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side) return;
        if (Amount <= 0) return;

//        // 引擎若在"伤害为 0"时没派发 AfterDamageReceived, 欠下的伤害会滞留在这里,
//        // 趁印记还没散补打出去, 免得太极变成纯免疫。
//        if (_pendingRedirect > 0)
//            await FlushPending(ctx, "BeforeSideTurnEnd(补打)");
//
//        // 意图阶段留下的未转正快照一并作废。
//        _lastZeroed = 0m;


        await PowerCmd.Remove(this);
    }

//    /// <summary>
//    ///     把挂起的伤害发给随机敌人; 无来源, 绝不递归回自己。
//    ///     ★ [2026-10-19 晚] **N 次独立转嫁** (用户定调): 发的次数 = 当前带太极印记的
//    ///     玩家数 N。单机恒为 1 次; 联机 2 人都有太极时同一笔挂起会转嫁 2 次
//    ///     (每次独立随机敌人) —— 这是"多人持有太极"的核心收益。
//    /// </summary>
//    private async Task FlushPending(PlayerChoiceContext ctx, string tag)
//    {
//        var pending = _pendingRedirect;
//        _pendingRedirect = 0m;
//        var combatState = Owner.CombatState;
//        if (combatState == null || Owner.IsDead) return;
//
//        var marks = CountMarkedPlayers(combatState);
//        MainFile.Logger.Info($"[太极] {tag}: 转移 {pending} 点 × {marks} 次 (印记数)");
//
//        for (var i = 0; i < marks; i++)
//        {
//            var victim = PickVictim(combatState);
//            if (victim == null)
//            {
//                MainFile.Logger.Warn($"[太极] {tag}: 第 {i + 1}/{marks} 次无可命中敌人, 该份伤害丢失");
//                continue;
//            }
//            // ⚠ 必须无来源 (dealer=null): 见类注释铁律①, 且 null 来源保证不会递归回本钩子。
//            await CreatureCmd.Damage(ctx, victim, pending, ValueProp.Unblockable, null, null, null);
//        }
//    }
//
//    /// <summary>当前带有效太极印记的存活玩家数 (N 次独立转嫁的倍数)。</summary>
//    private static int CountMarkedPlayers(ICombatState combatState)
//    {
//        var n = 0;
//        foreach (var p in combatState.Players)
//        {
//            if (p?.Creature == null || !p.Creature.IsAlive) continue;
//            if (p.Creature.GetPower<TaiChiMark>() is { Amount: > 0 }) n++;
//        }
//        return Math.Max(n, 1);
//    }


    private Creature? PickVictim(ICombatState combatState)
    {
        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable)
            .ToList();
        if (candidates.Count == 0) return null;
        // ★确定性 RNG (同 Patches/TaiChiRedirectPatch 的联机 desync 修复说明)。
        return yylRng.PickCombatTarget(combatState, candidates);
    }

//    /// <summary>
//    ///     守护范围: 同队的玩家 (含 Owner 自己)。守护全队语义下, 队友挨打也由本印记接管。
//    /// </summary>
//    private bool IsProtectedTeammate(Creature? target)
//    {
//        if (target == null || !target.IsPlayer) return false;
//        return target.Side == Owner.Side;
//    }
//
//    /// <summary>
//    ///     该不该转嫁: 真实攻击 + 敌方来源。
//    ///     dealer 为空时用"当前处于敌方回合"兜底 —— 联机下部分伤害命令拿不到 dealer,
//    ///     旧规则要求 dealer 必须非空, 会让太极在多人里彻底哑火。
//    /// </summary>
//    private bool ShouldRedirect(ValueProp props, Creature? dealer)
//    {
//        if (!ValuePropExtensions.IsPoweredAttack(props)) return false;
//        if (dealer != null) return dealer.Side != Owner.Side;
//        try
//        {
//            return Owner.CombatState?.CurrentSide == CombatSide.Enemy;
//        }
//        catch (Exception e)
//        {
//            MainFile.Logger.Warn($"[太极] 读取当前回合失败, 按\"非敌方回合\"处理: {e.Message}");
//            return false;
//        }
//    }

}