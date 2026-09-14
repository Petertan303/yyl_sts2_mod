using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;

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

    /// <summary>
    ///     Gain Qi through the <see cref="IGainQi" /> hook chain (e.g. <c>OldFarm</c> modifies
    ///     the amount, <c>WenYang</c> deals damage as a follow-up side effect).
    /// </summary>
    public static async Task GainQi(
        PlayerChoiceContext ctx,
        Player player,
        int amount,
        AbstractModel? source,
        CardModel? cardSource = null)
    {
        if (amount <= 0) return;
        var modified = yylHook.ModifyQiGain(player, amount, out var modifiers);
        if (modified > 0)
            await PowerCmd.Apply<Qi>(ctx, new[] { player.Creature }, modified, player.Creature, cardSource);
        await yylHook.AfterModifyingQiGain(ctx, player, modifiers, amount, modified);
    }

    /// <summary>
    ///     Lose Qi through the <see cref="ILoseQi" /> hook chain (e.g. <c>DanShi</c> gains block
    ///     as a follow-up side effect). The hook is allowed to scale the loss up or down; the
    ///     final value is applied to the existing <c>Qi</c> counter.
    /// </summary>
    public static async Task LoseQi(
        PlayerChoiceContext ctx,
        Player player,
        int amount,
        AbstractModel? source,
        CardModel? cardSource = null)
    {
        if (amount <= 0) return;
        var modified = yylHook.ModifyQiLoss(player, amount, out var modifiers);
        if (modified > 0)
        {
            // PowerCmd.Apply with negative amount reduces counter powers (Qi is Counter stack).
            // If a future BaseLib version rejects negative amounts, fall back to a direct
            // ReduceAmount on the existing Qi power.
            await PowerCmd.Apply<Qi>(ctx, new[] { player.Creature }, -modified, player.Creature, cardSource);
        }
        await yylHook.AfterModifyingQiLoss(ctx, player, modifiers, amount, modified);
    }
}
