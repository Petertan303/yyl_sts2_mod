using MegaCrit.Sts2.Core.Entities.Players;

namespace yyl_sts2_mod.Code.Events;

public interface IModifyWrathDamage
{
    decimal ModifyWrathDamage(Player player, decimal multiplier);
}