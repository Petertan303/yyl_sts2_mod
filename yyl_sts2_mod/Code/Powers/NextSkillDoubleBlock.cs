using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     抱山: <b>下一张打出的技能牌获得双倍格挡</b> (由「抱山」给予, 2026-10-03)。
///     <para>
///         实现走引擎的格挡乘区钩子 <see cref="AbstractModel.ModifyBlockMultiplicative" />
///         (与原版 <c>UnmovablePower</c>「坚定不移」同款思路): 打出技能牌时返回 2m,
///         该牌结算完毕后 power 自动移除 → 天然"只生效一张牌", 无需监听出牌事件。
///     </para>
///     <para>
///         ★为什么必须用<b>乘法</b>而不是"额外给等同其格挡的格挡":
///         乘法直接作用在结算值上, 能吃到卡牌自身的格挡加成(金光/弃牌堆等),
///         也不会与「守势」等已有格挡乘区冲突(它们都是乘法, 依次相乘)。
///     </para>
///     <para>
///         触发范围: 仅自己打出的<b>技能牌</b>(<see cref="CardType.Skill" />)。
///         打出攻击牌/能力牌时<b>不消耗</b>本 power(留着给下一张技能牌)。
///     </para>
/// </summary>
public sealed class NextSkillDoubleBlock : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>只有一层(要"下一张", 可叠多层无意义)。</summary>
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>剩余可生效的技能牌数量 (未来支持"接下来 N 张")。</summary>
    [SavedProperty]
    public int Charges
    {
        get => _charges;
        set
        {
            AssertMutable();
            _charges = value;
        }
    }

    private int _charges;

    public NextSkillDoubleBlock()
    {
        _charges = 1;
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (_charges <= 0) return 1m;
        if (target != Owner) return 1m;
        // 只认卡牌获得的格挡(与原版 UnmovablePower 一致), 不影响自然回血等。
        if (!props.IsCardOrMonsterMove()) return 1m;
        if (cardSource == null) return 1m;
        // ★只认技能牌: 打出攻击/能力牌时 return 1m 且**不消耗**charges。
        if (cardSource.Type != CardType.Skill) return 1m;
        if (cardSource.Owner.Creature != Owner) return 1m;

        // 消耗一次 —— 该牌结算后本 power 归零, 下一张技能牌恢复原格挡。
        _charges = 0;
        return 2m;
    }
}