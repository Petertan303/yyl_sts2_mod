using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Commands;

public class yylCmd
{
    private static readonly System.Reflection.MethodInfo? LoseHpInternalMethod = typeof(Creature).GetMethod(
        "LoseHpInternal",
        System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Public);

    /// <summary>
    ///     失去生命 (HP Loss): 绕过伤害管线 —— 不吃格挡、不吃炁/姿态/遗物的增减伤,
    ///     与原版"失去 N 点生命"语义一致。
    ///     <para>
    ///         ⚠ 不要用 <c>CreatureCmd.Damage</c> 打自己来实现"失去生命":
    ///         它走完整伤害管线, 燃炁/拉伤的"固定自伤"会被炁增伤等乘区放大 (v0.111.0 实测)。
    ///         底层反射 <c>Creature.LoseHpInternal</c> (原版同款私有入口)。
    ///     </para>
    /// </summary>
    public static void LoseHp(Creature creature, decimal amount)
    {
        if (creature == null || amount <= 0) return;
        LoseHpInternalMethod?.Invoke(creature, new object[] { amount, default(ValueProp) });
    }
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
    ///     Gain Qi through the <see cref="IGainQi" /> hook chain (e.g. <c>PeasantDrill</c> modifies
    ///     the amount, <c>Nurture</c> deals damage as a follow-up side effect).
    /// </summary>
    public static async Task GainQi(
        PlayerChoiceContext ctx,
        Player player,
        int amount,
        AbstractModel? source,
        CardModel? cardSource = null)
    {
        if (amount <= 0) return;
        // 记录"本回合获得过炁"(全 mod 唯一产炁入口, 供崩拳等条件牌查询)。
        QiGainTracker.Mark(player, player.Creature?.CombatState);
        var modified = yylHook.ModifyQiGain(player, amount, out var modifiers);
        if (modified > 0)
            await PowerCmd.Apply<Qi>(ctx, new[] { player.Creature }, modified, player.Creature, cardSource);
        await yylHook.AfterQiGained(ctx, player, amount, modified);
    }

    /// <summary>
    ///     Lose Qi through the <see cref="ILoseQi" /> hook chain (e.g. <c>CinnabarBite</c> gains block
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
        var modified = yylHook.ModifyQiLoss(player, amount, out _);
        var current = player.Creature.GetPower<Qi>()?.Amount ?? 0;
        var actualLoss = Math.Min(modified, Math.Max(current, 0));
        if (actualLoss > 0)
        {
            // PowerCmd.Apply with negative amount reduces counter powers (Qi is Counter stack).
            // If a future BaseLib version rejects negative amounts, fall back to a direct
            // ReduceAmount on the existing Qi power.
            await PowerCmd.Apply<Qi>(ctx, new[] { player.Creature }, -actualLoss, player.Creature, cardSource);
        }
        await yylHook.AfterQiLost(ctx, player, amount, actualLoss);
    }
}
