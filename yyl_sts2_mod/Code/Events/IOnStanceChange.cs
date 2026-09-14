using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Events;

public interface IOnStanceChange
{
    Task OnStanceChange(PlayerChoiceContext ctx, Player player, yylStanceModel oldStance,
        yylStanceModel newStance);
}