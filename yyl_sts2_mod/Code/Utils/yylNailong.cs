using MegaCrit.Sts2.Core.Entities.Creatures;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

public static class yylNailong
{
    /// <summary>
    ///     只判断"是不是奶龙"(身上带着奶龙标记/心魔), 不要求存活。
    ///     <para>
    ///         击杀钩子 (<c>AfterDeath</c>) 里怪物 <c>IsAlive</c> 已经是 false,
    ///         所以「养龙」遗物计数必须用这个版本, 否则永远不涨层。
    ///     </para>
    /// </summary>
    public static bool IsNailongMarked(Creature? creature) =>
        creature != null &&
        (creature.HasPower<NailongMark>() || creature.HasPower<InnerDemon>());

    public static bool IsNailong(Creature? creature) =>
        creature is { IsAlive: true } && IsNailongMarked(creature);

    public static bool IsNailongSource(Creature? source) =>
        IsNailong(source) || IsNailong(source?.PetOwner?.Creature);
}
