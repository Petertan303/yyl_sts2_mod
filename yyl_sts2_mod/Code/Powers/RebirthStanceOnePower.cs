using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     逆生一重伴随 Power (对玩家隐藏): <b>每回合第一次获得格挡时，该次格挡翻倍</b>。
///     <para>
///         [2026-09-20] 由姿态模型迁移而来 —— 块值分发不吃 ModHelper 订阅的模型,
///         必须挂在 Power 上 (原版坚定不移 UnmovablePower 同款管线)。
///     </para>
///     <para>
///         [2026-09-20 晚] 改为原版<b>历史查询式</b>(与 UnmovablePower 完全同构):
///         不维护任何标记 —— "是否为本回合第一次" 直接查战斗历史里
///         本回合 / 本人 / 攻击来源的 <see cref="BlockGainedEntry" /> 数量。
///         历史只在真实获得格挡后落账 (GainBlock 管线末尾), 所以预览与结算
///         天然一致, 不存在"预览烧掉标记"的结算顺序坑。
///         (此前两版: 姿态模型上挂钩子从不触发 → Power+SpireField 标记被
///         预览烧掉, 均已废弃。)
///     </para>
/// </summary>
public sealed class RebirthStanceOnePower : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家隐藏 (姿态标签已经承担了展示职责, 不占能力栏)。</summary>
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // 与原版坚定不移一致: 只翻倍"卡牌/敌人攻击"来源的格挡。
        if (target != Owner || !ValuePropExtensions.IsCardOrMonsterMove(props))
            return 1m;

        // ★联机隔离 (对齐原版 UnmovablePower): 只有**自己打出的牌**带来的格挡才翻倍。
        //   缺少这层校验时, 队友的牌给本体加格挡同样会触发翻倍; 而下面的计数只统计
        //   自己出牌产生的格挡, 这次压根消耗不到计数 —— 于是同一回合内可以被
        //   多名队友反复触发。 (cardSource.Owner 可能为空, 故用 ?. 而非原版裸取。)
        if (cardSource != null && cardSource.Owner?.Creature != Owner)
            return 1m;

        // 历史查询式"每回合第一次": 本回合本人已有的攻击来源格挡次数 == 0 → 翻倍。
        // 历史条目在真实落账后才追加, 因此预览/结算天然一致。
        return CountCardBlocksThisTurn(Owner) == 0 ? 2m : 1m;
    }

    /// <summary>
    ///     统计"本回合 / 本人 / 攻击来源"的、已经落账的格挡次数。
    ///     <para>
    ///         性能优化 (针对联机卡顿): 原写法每次都全量枚举 <c>History.Entries</c>,
    ///         该列表随战斗持续增长 (后期上千条), 而本钩子在每次获得格挡时都会触发,
    ///         4 人联机下开销被成倍放大。
    ///         <b>依据</b>: 引擎 <c>CombatHistory</c> 内部用
    ///         <c>List&lt;CombatHistoryEntry&gt;</c> 支撑, 仅对外声明为
    ///         <c>IEnumerable&lt;CombatHistoryEntry&gt;</c> —— 故可用 <c>as IList</c> 反向索引;
    ///         历史按时间追加, 本回合的条目必然是<b>尾部连续的一段</b>,
    ///         扫到第一条不属于本回合的条目即可停止。
    ///         复杂度从 O(整场历史) 降为 O(本回合条目数), 判定条件与原写法逐条等价。
    ///         <c>as</c> 若失败则自动回退全量扫描, 行为不变。
    ///     </para>
    /// </summary>
    private static int CountCardBlocksThisTurn(Creature owner)
    {
        var combatState = owner.CombatState;
        if (combatState == null) return 0;

        if (CombatManager.Instance.History.Entries is IList<CombatHistoryEntry> entries)
        {
            var count = 0;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (!entries[i].HappenedThisTurn(combatState)) break;

                if (entries[i] is BlockGainedEntry block
                    && block.CardPlay != null
                    && block.CardPlay.Player?.Creature == owner
                    && ValuePropExtensions.IsCardOrMonsterMove(block.Props))
                {
                    count++;
                }
            }

            return count;
        }

        // 回退: 与改动前完全一致的全量扫描
        return CombatManager.Instance.History.Entries
            .OfType<BlockGainedEntry>()
            .Count(e => e.HappenedThisTurn(combatState)
                     && e.CardPlay != null
                     && e.CardPlay.Player?.Creature == owner
                     && ValuePropExtensions.IsCardOrMonsterMove(e.Props));
    }
}
