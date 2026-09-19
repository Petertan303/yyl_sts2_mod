using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     点穴: 被点中穴位留下的"标记"。本身不产生任何效果, 只记录层数;
///     「点穴」这张牌在施加层数后, 按目标当前的层数扣除生命 (无视格挡)。
///     <para>
///         层数会持续累积 —— 反复点同一个目标会让下一次的点穴更痛,
///         这是普通攻击位里少见的"滚雪球"钩子。
///     </para>
/// </summary>
public sealed class DianXue : yylPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
