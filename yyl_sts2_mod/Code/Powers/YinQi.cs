using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     引炁: 你下一次打出<b>攻击牌</b>时, 获得等同层数的炁, 然后自行散去 (由「引炁」给予)。
///     <para>
///         把"产炁"挪到攻击之后 —— 先出招、再攒炁, 与「蓄势」(下回合才到账) 和
///         「马步」(立刻到账) 在节奏上错开, 是攻击流最便宜的产炁方式。
///     </para>
/// </summary>
public sealed class YinQi : yylPowerModel
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
            await yylCmd.GainQi(choiceContext, Owner.Player, amount, this, card);
    }
}
