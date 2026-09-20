using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     逆生一重伴随 Power (对玩家隐藏): <b>每回合第一次获得格挡时，该次格挡翻倍</b>。
///     <para>
///         [2026-09-20] 由姿态模型迁移而来 —— 块值分发不吃 ModHelper 订阅的模型,
///         必须挂在 Power 上 (原版坚定不移 UnmovablePower 同款管线)。
///     </para>
///     <para>
///         [2026-09-20 晚] 改为原版<b>历史查询式</b>(与 UnmovablePower 完全同构):
///         不维护任何标记 —— "是否为本回合第一次" 直接查战斗历史里
///         本回合 / 本人 / 攻击来源的 <see cref="BlockGainedEntry" /> 数量。
///         历史只在真实获得格挡后落账 (GainBlock 管线末尾), 所以预览与结算
///         天然一致, 不存在"预览烧掉标记"的结算顺序坑。
///         (此前两版: 姿态模型上挂钩子从不触发 → Power+SpireField 标记被
///         预览烧掉, 均已废弃。)
///     </para>
/// </summary>
public sealed class RebirthStanceOnePower : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家隐藏 (姿态标签已经承担了展示职责, 不占能力栏)。</summary>
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // 与原版坚定不移一致: 只翻倍"卡牌/敌人攻击"来源的格挡。
        if (target != Owner || !ValuePropExtensions.IsCardOrMonsterMove(props))
            return 1m;

        // 历史查询式"每回合第一次": 本回合本人已有的攻击来源格挡次数 == 0 → 翻倍。
        // 历史条目在真实落账后才追加, 因此预览/结算天然一致。
        var count = CombatManager.Instance.History.Entries
            .OfType<BlockGainedEntry>()
            .Count(e => e.HappenedThisTurn(Owner.CombatState)
                     && e.CardPlay != null
                     && e.CardPlay.Player?.Creature == Owner
                     && ValuePropExtensions.IsCardOrMonsterMove(e.Props));

        return count == 0 ? 2m : 1m;
    }
}
