using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
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
///         [fix 2026-09-20 晚] <b>结算顺序坑</b>: 最初把"标记已用"写在
///         <see cref="ModifyBlockMultiplicative" /> 里, 但它是纯计算函数 ——
///         卡面<b>预览</b>也会调用, 预览先跑一遍就把标记烧成已用,
///         真实结算时判定"已经给过"而不再翻倍 (日志实证: 预览 used:False → 结算 used:True)。
///         现改为: 计算函数纯函数化 (只读标记); 标记移到
///         <see cref="AfterBlockGained" /> (真实获得格挡后触发, 预览不走该管线)。
///     </para>
/// </summary>
public sealed class RebirthStanceOnePower : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家隐藏 (姿态标签已经承担了展示职责, 不占能力栏)。</summary>
    protected override bool IsVisibleInternal => false;

    private static readonly SpireField<Creature, bool> FirstBlockUsed = new(() => false);

    /// <summary>每回合重置 (自己回合开始时)。</summary>
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    {
        if (side == Owner.Side)
            FirstBlockUsed[Owner] = false;
        return Task.CompletedTask;
    }

    /// <summary>纯计算: 只读标记, 绝不写入 (预览与结算共用, 写入会烧掉标记)。</summary>
    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || props.HasFlag(ValueProp.Unpowered) || FirstBlockUsed[target])
            return 1m;

        return 2m;
    }

    /// <summary>真实获得格挡后 (预览不走此管线) 标记本回合的"第一次"已消费。</summary>
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0) return Task.CompletedTask;
        // 与翻倍条件对齐: Unpowered 的格挡没吃到翻倍, 也不消耗"第一次"。
        if (props.HasFlag(ValueProp.Unpowered)) return Task.CompletedTask;
        if (!FirstBlockUsed[creature])
            FirstBlockUsed[creature] = true;
        return Task.CompletedTask;
    }
}
