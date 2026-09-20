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
///         [2026-09-20] 由姿态模型 (<c>RebirthStanceOne</c>) 迁移而来。姿态模型虽经
///         ModHelper.SubscribeForCombatStateHooks 订阅了 CombatState 钩子, 但块值分发
///         (<see cref="ModifyBlockMultiplicative" />) 不吃订阅模型 —— 姿态上的重写从不
///         触发。改按原版「坚定不移」(<c>UnmovablePower</c>) 同款管线: 挂在玩家身上的
///         Power 必然被块值分发枚举到。由 <c>yylModel.SetStance</c> 在进入/离开
///         逆生一重时同步挂上/移除。
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

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || props.HasFlag(ValueProp.Unpowered) || FirstBlockUsed[target])
            return 1m;

        FirstBlockUsed[target] = true;
        return 2m;
    }
}
