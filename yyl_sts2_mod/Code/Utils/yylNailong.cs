using MegaCrit.Sts2.Core.Entities.Creatures;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

public static class yylNailong
{
    public static bool IsNailong(Creature? creature) =>
        creature is { IsAlive: true } &&
        (creature.HasPower<NailongMark>() || creature.HasPower<InnerDemon>());

    public static bool IsNailongSource(Creature? source) =>
        IsNailong(source) || IsNailong(source?.PetOwner?.Creature);
}
