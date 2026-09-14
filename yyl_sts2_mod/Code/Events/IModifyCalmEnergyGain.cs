using MegaCrit.Sts2.Core.Entities.Players;

namespace yyl_sts2_mod.Code.Events;

public interface IModifyCalmEnergyGain
{
    int ModifyCalmEnergyGain(Player player, int amount);
}