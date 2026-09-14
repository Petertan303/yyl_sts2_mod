using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace yyl_sts2_mod.Code.Commands;

public class yylCmd
{
    public static async Task<CardModel?> GiveCard<T>(Player player,
        PileType pileType,
        CardPilePosition pos = CardPilePosition.Bottom,
        float animationTime = 0.6f,
        CardPreviewStyle animationStyle = CardPreviewStyle.HorizontalLayout,
        bool upgraded = false,
        bool skipAnimation = false) where T : CardModel
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return null;
        var card = combatState.CreateCard(ModelDb.Card<T>(), player);
        if (upgraded)
            CardCmd.Upgrade(card);
        var result = await CardPileCmd.AddGeneratedCardToCombat(card, pileType, player, pos);
        if (skipAnimation) return card;
        CardCmd.PreviewCardPileAdd(result, animationTime, animationStyle);
        return card;
    }

    public static async Task GiveCards<T>(Player player,
        int amount,
        PileType pileType,
        CardPilePosition pos = CardPilePosition.Bottom,
        float animationTime = 0.6f,
        CardPreviewStyle animationStyle = CardPreviewStyle.HorizontalLayout,
        bool upgraded = false,
        bool skipAnimation = false) where T : CardModel
    {
        var cardsToGive = new List<CardModel>();
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;
        for (var i = 0; i < amount; i++)
        {
            var card = combatState.CreateCard(ModelDb.Card<T>(), player);
            if (upgraded)
                CardCmd.Upgrade(card);
            cardsToGive.Add(card);
        }

        var result = await CardPileCmd.AddGeneratedCardsToCombat(cardsToGive, pileType, player, pos);
        if (skipAnimation || pileType == PileType.Hand) return;
        CardCmd.PreviewCardPileAdd(result, animationTime, animationStyle);
    }
}