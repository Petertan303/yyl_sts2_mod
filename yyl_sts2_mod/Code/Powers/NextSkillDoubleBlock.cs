using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     抱山: <b>下一张打出的技能牌获得双倍格挡</b> (由「抱山」给予, 2026-10-03)。
///     <para>
///         实现走引擎的格挡乘区钩子 <see cref="AbstractModel.ModifyBlockMultiplicative" />
///         (与原版 <c>UnmovablePower</c>「坚定不移」同款思路): 技能牌结算时返回 2m。
///     </para>
///     <para>
///         为什么必须用<b>乘法</b>而不是"额外给等同其格挡的格挡":
///         乘法直接作用在结算值上, 能吃到卡牌自身的格挡加成(金光/弃牌堆等),
///         也不会与「守势」等已有格挡乘区冲突(它们都是乘法, 依次相乘)。
///     </para>
///     <para>
///         触发范围: 仅自己打出的<b>技能牌</b>(<see cref="CardType.Skill" />)。
///         打出攻击牌/能力牌时<b>不消耗</b>本 power(留着给下一张技能牌)。
///     </para>
///     <para>
///         ★★<b>2026-10-05 修「不翻倍也不消失」</b>—— 根因是<b>用错层数载体</b>:
///         原来把"还能生效几次"存在<b>私有字段</b> <c>_charges</c>(构造函数置 1)里,
///         但引擎判定层数只看 <see cref="PowerModel.Amount" />:
///         <list type="number">
///             <item><c>PowerCmd.Apply&lt;T&gt;</c> 内部走 <c>ModelDb.Power&lt;T&gt;().ToMutable()</c>,
///             无参 ⇒ <c>initialAmount = 0</c>;</item>
///             <item>而 <c>PowerModel.ShouldRemoveDueToAmount()</c> 在 <c>Amount == 0</c> 时
///             返回 <b>true</b> ⇒ power 挂上后<b>立刻被移除</b>, 玩家连图标都看不到;</item>
///             <item>于是既"不翻倍"(实例已消失, 钩子根本不参与), 也"不消失"(它是被引擎
///             立即清掉的, 不是我们的逻辑没写)。</item>
///         </list>
///         修法: 计数<b>直接读 <see cref="PowerModel.Amount" /></b>(引擎认可的层数来源,
///         照 <c>UnmovablePower</c> 用 <c>Amount</c> 判定的做法), 命中后由
///         <see cref="AfterCardPlayed" /> 用 <c>PowerCmd.Apply(power, -1)</c> 真正减一层,
///         归零时引擎自动移除。彻底删掉 <c>Charges</c>/<c>_charges</c> 与 <c>[SavedProperty]</c>
///         (BaseLib 本就警告过 "SavedProperty Charges will not work on this type")。
///         <para/>
///         ★ <b>为什么扣层放在 <see cref="AfterCardPlayed" /> 而不放在乘区钩子里</b>:
///         引擎 <c>CardModel.OnPlayWrapper</c> 的顺序是 <c>OnPlay</c>(内含格挡结算)
///         → <c>History.CardPlayFinished</c> → <c>Hook.AfterCardPlayed</c>。
///         乘区钩子 <c>ModifyBlockMultiplicative</c> 是<b>同步</b>计算钩子, 在里面改层数
///         会让同一个格挡值既被翻倍又被立刻移除, 时序不稳; 挂到 <c>AfterCardPlayed</c>
///         则由引擎在卡牌结算完成后回调, 时序确定。
///         </para>
///     </para>
/// </summary>
public sealed class NextSkillDoubleBlock : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>
    ///     ★必须是 <c>Counter</c> 而非 <c>Single</c>: 引擎靠 <see cref="PowerModel.Amount" />
    ///     承载层数, <c>Single</c> 下 <c>Amount</c> 恒为 1, 无法表达"已消耗"。
    /// </summary>
    public override PowerStackType StackType => PowerStackType.Counter;

    public NextSkillDoubleBlock()
    {
    }

    /// <summary>
    ///     技能牌结算<em>完成后</em>扣一层(引擎保证此时格挡已算完)。
    ///     打出攻击/能力牌时<b>不扣</b>, 留着给下一张技能牌。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0) return;
        if (cardPlay?.Card == null) return;
        if (cardPlay.Card.Type != CardType.Skill) return;
        if (cardPlay.Player.Creature != Owner) return;

        // -1 ⇒ Amount 归零时 ShouldRemoveDueToAmount() 为 true, 引擎自动移除本 power。
        await PowerCmd.Apply(choiceContext, this, Owner, -1m, Owner, cardPlay.Card);
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (Amount <= 0) return 1m;
        if (target != Owner) return 1m;
        // 只认卡牌获得的格挡(与原版 UnmovablePower 一致), 不影响自然回块等。
        if (!props.IsCardOrMonsterMove()) return 1m;
        if (cardSource == null) return 1m;
        // ★只认技能牌: 打出攻击/能力牌时 return 1m(层数在 AfterCardPlayed 里不动)。
        if (cardSource.Type != CardType.Skill) return 1m;
        if (cardSource.Owner.Creature != Owner) return 1m;

        return 2m;
    }
}
