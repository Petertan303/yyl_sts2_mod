using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     引金光: 你下一次打出<b>攻击牌</b>时, 获得等同层数的金光护体, 然后自行散去 (由「抱山」给予)。
///     <para>
///         与「引炁」同款的延迟结构。给予方 (移山) 先消耗全部金光 (经「金光化炁」按量回炉成炁),
///         再把 2 倍于失去量的金光投放到下一次攻击 —— "花金光 → 打攻击 → 回更多金光"的翻倍循环。
///         层数由给予方按消耗量决定; 多次给予在 Counter 上叠加, 下一次攻击时一次性触发。
///     </para>
/// </summary>
public sealed class YinAegis : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card == null) return;
        // 只认自己打出的攻击牌。
        if (Owner.Player == null || card.Owner != Owner.Player) return;
        if (card.Type != CardType.Attack) return;

        var amount = (int)Amount;
        await PowerCmd.Remove(this);
        if (amount > 0)
            await PowerCmd.Apply<GoldenAegis>(choiceContext, new[] { Owner }, amount, Owner, card);
    }
}
