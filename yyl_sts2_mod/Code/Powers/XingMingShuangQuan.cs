using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     性命双全: 持有时, "下一张牌" 打出 2 次。
///     <para>
///         设计为一次性 buff: <c>Amount = 1</c> 时下一张牌打 2 次, 之后 Power 自动移除。
///         升级后可改为"持有期间每张牌都打 2 次"或"剩余 N 张牌打 2 次",目前实现为一次性。
///     </para>
///     <para>
///         TODO: 当前是占位实现,真正"打 2 次"的逻辑需要 hook 进 card-play 事件流。
///         可选路径:
///         <list type="number">
///             <item>Harmony patch <c>Card.OnPlay</c> 检测此 Power 是否在 Owner 上,若是则再触发一次</item>
///             <item>通过 <c>CombatState</c> 的事件订阅实现</item>
///             <item>若有 BaseLib 提供的 <c>Echo</c> / <c>NextCardDoubled</c> 原语,直接调用</item>
///         </list>
///     </para>
/// </summary>
public sealed class XingMingShuangQuan : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    /// <summary>Initial amount when applied (one-shot).</summary>
    public const int InitialCharges = 1;
}
