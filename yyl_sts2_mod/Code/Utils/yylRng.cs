using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     战斗内随机取目标的统一入口 (2026-10-15 建立)。
///     <para>
///         ★★mod 里凡是"随机选一个目标/随机选一个 debuff"的地方, 一律走这里,
///         <b>禁止</b>用 <c>Random.Shared</c> / <c>new Random()</c> / <c>GD.Rand</c>。
///     </para>
///     <para>
///         原因: 联机是<b>各端独立模拟 + 定时同步 RNG 状态</b>的 lockstep 架构
///         (引擎侧 <c>Multiplayer.CombatStateSynchronizer.OnSyncRngMessageReceived</c>
///         会广播并校正 RNG)。每个客户端都会跑一遍同一段战斗逻辑, 于是任何
///         随机都必须取自引擎那颗<b>有种子、可复现、可同步</b>的 RNG ——
///         <c>RunState.Rng.CombatTargets</c>(与引擎自己挑攻击目标用的是同一颗)。
///         用进程级随机 (Random.Shared) 的结果是: A 端把伤害转嫁给敌人1,
///         B 端转嫁给敌人2 → 血量/状态分叉 → 数据不同步。
///     </para>
///     <para>
///         原版"随机目标"卡 (飞剑回旋镖等) 之所以不出问题, 是因为它们走声明式的
///         <c>TargetType.RandomEnemy</c> / <c>AttackCommand.TargetingRandomOpponents</c>,
///         随机发生在引擎的同步流程内部; 我们是在 Harmony 前缀里自己改 Damage 的
///         目标参数, 必须自己保证确定性。
///     </para>
/// </summary>
public static class yylRng
{
    /// <summary>
    ///     从候选列表里用<b>确定性</b> RNG 挑一个。取不到 RNG (理论上不该发生) 时
    ///     退化为"取第一个" —— 宁可失去随机性, 也不能让各端挑出不同的结果。
    /// </summary>
    public static T? PickCombatTarget<T>(ICombatState? combatState, IReadOnlyList<T> candidates)
        where T : class
    {
        if (candidates == null || candidates.Count == 0) return null;
        try
        {
            var rng = combatState?.RunState?.Rng?.CombatTargets;
            if (rng == null) return candidates[0];
            return rng.NextItem(candidates) ?? candidates[0];
        }
        catch (Exception e)
        {
            // 铁律: 钩子绝不抛异常 (未捕获异常会冻结整场战斗)。
            // 退化到"取第一个"仍是确定性的, 各端一致, 只是失去随机性。
            MainFile.Logger.Warn($"[yyl_sts2_mod] 确定性 RNG 取用失败, 退化为取第一个候选: {e.Message}");
            return candidates[0];
        }
    }
}
