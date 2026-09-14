using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Core;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Commands;

public static class StanceCmd
{
    public static Task EnterWrath(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<WrathStance>(ctx, player, cardSource);
    }

    public static Task EnterCalm(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<CalmStance>(ctx, player, cardSource);
    }

    public static Task EnterDivinity(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<DivinityStance>(ctx, player, cardSource);
    }

    public static Task ExitStance(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<NoStance>(ctx, player, cardSource);
    }

    public static Task EnterReverseLife1(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<ReverseLife1Stance>(ctx, player, cardSource);
    }

    public static Task EnterReverseLife2(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<ReverseLife2Stance>(ctx, player, cardSource);
    }

    public static Task EnterReverseLife3(PlayerChoiceContext ctx, Player player, CardModel? cardSource)
    {
        return yylModel.SetStance<ReverseLife3Stance>(ctx, player, cardSource);
    }
}
