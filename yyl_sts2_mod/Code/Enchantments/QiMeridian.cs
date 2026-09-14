using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Enchantments;

/// <summary>
///     炁脉: 自定义附魔。被附魔的牌每次打出时, 额外获得 1 点炁。
///     <para>
///         这是本 mod 的「局外资源」—— 附魔写在牌组里, 跨战斗永久保留,
///         而原版角色的金币 / 药水 / 最大生命都已被占用, 只有炁是本角色独有的东西。
///         由卡牌「开脉」在战斗结束后施加 (见 <c>Powers.MeridianOpening</c>)。
///     </para>
/// </summary>
public sealed class QiMeridian : CustomEnchantmentModel
{
    /// <summary>每次打出被附魔的牌, 额外获得的炁。</summary>
    public const int QiPerPlay = 1;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay is null || !HasCard) return;

        var card = Card!;
        // 只有被附魔的那张牌打出时才结算。
        if (cardPlay.Card != card) return;

        var player = card.Owner;
        if (player is null) return;

        await yylCmd.GainQi(choiceContext, player, QiPerPlay, card, null);
    }
}
