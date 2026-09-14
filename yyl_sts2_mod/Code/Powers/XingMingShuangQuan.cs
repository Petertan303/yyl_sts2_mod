using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     性命双全: 持有时, "下一张牌" 打出 2 次。
///     <para>
///         设计为一次性 buff: <c>Amount = 1</c> 时下一张牌打 2 次, 之后 Power 自动移除。
///         升级后可改为"持有期间每张牌都打 2 次"或"剩余 N 张牌打 2 次"。
///     </para>
/// </summary>
public sealed class XingMingShuangQuan : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>Initial amount when applied (one-shot).</summary>
    public const int InitialCharges = 1;

    private CardModel? _modifiedCard;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (_modifiedCard != null || card.Owner.Creature != Owner) return playCount;
        _modifiedCard = card;
        return playCount + 1;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card != _modifiedCard) return;
        _modifiedCard = null;
        await PowerCmd.Remove(this);
    }
}
