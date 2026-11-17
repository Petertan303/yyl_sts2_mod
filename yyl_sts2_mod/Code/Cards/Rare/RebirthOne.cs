using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Cards.Token;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Core;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     逆生一重: 3 费, 进入逆生一重姿态, 并把「逆生二重」加入手牌。
///     逆生三重姿态链的入口; 基础带消耗与保留 (★2026-11-16 用户定调: 保留改为基础自带,
///     升级效果只剩「放入手牌的是升级版二重」)。
///     <para>
///         ★ 悬浮说明照原版「冲锋!<c>Charge</c>」/「护驾!<c>Guards</c>」同款写法:
///         <c>ExtraHoverTips</c> + <c>HoverTipFactory.FromCardWithCardHoverTips&lt;T&gt;(IsUpgraded)</c>,
///         <b>把升级状态作为参数传进去</b>, 于是预览/卡面能正确显示"升级后给的是升级版二重/三重"。
///         <para/>
///         【旧写法的问题】之前用 <c>WithTip(TooltipSource)</c> 在<b>构造时静态固化</b>了
///         一个"未升级"的规范实例悬浮框 —— BaseLib 的 <c>WithTip</c> 只在构造时求值一次,
///         卡牌升级后不会重算, 于是升级预览里看不到变化、文字说明里也没有"升级版"字样。
///         原版两张参考卡都是<b>每次求值时读 <c>IsUpgraded</c></b>, 所以不存在这个问题。
///         </para>
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public class RebirthOne(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>
    ///     悬浮说明: 展示实际给手的「逆生二重」卡面。
    ///     ★ 必须用 <c>WithTips(Func&lt;CardModel, ...&gt;)</c>(BaseLib 的动态入口),
    ///     它在<b>每次求值时</b>调用委托(见 <c>ConstructedCardModel.ExtraHoverTips</c>:
    ///     <c>_multiHoverTips.SelectMany(mt =&gt; mt(this))</c>) ⇒ 能读到实时的 <c>IsUpgraded</c>。
    ///     <para/>
    ///     对比: <c>WithTip(TooltipSource)</c> 走的是 <c>_hoverTips.Select(t =&gt; t.Tip(this))</c>,
    ///     构造时就把 tooltip 对象固化了 ⇒ 升级后仍显示未升级卡面(旧写法的 bug)。
    ///     ★ <c>ExtraHoverTips</c> 在 BaseLib 里是 <c>sealed override</c>, 不能自己覆写。
    /// </para>
    /// </summary>
    public RebirthOne() : this(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        // ★2026-11-16 用户定调: 保留改为基础自带 (原来是升级才 Add)。
        WithKeyword(CardKeyword.Retain);
        WithKeywords(CardKeyword.Exhaust);

        // 照原版「冲锋!Charge」/「护驾!Guards」的思路: 把升级状态作为参数传进预览工厂,
        // 于是预览/卡面能正确显示"升级后给的是升级版二重/三重"。
        // ★ 2026-10-07 二次修正: 原版写法 `HoverTipFactory.FromCardWithCardHoverTips<RebirthTwo>(IsUpgraded)`
        //   自身有缺陷(反编译实证): 它 `.Concat(ModelDb.Card<T>().HoverTips)` 取**规范单例**的 hovertip,
        //   而我们的 ExtraHoverTips 是递归的 ⇒ 规范单例 IsUpgraded 恒 false
        //   ⇒ **嵌套的下一层(逆生三重)必然退回未升级版**。
        //   所以改用自家的 yylHoverTipFactory.FromCardChain<T>: 预览与其自身 hovertip 共用同一份升级克隆,
        //   递归传下去的 card.IsUpgraded 也是 true ⇒ 一重→二重→三重 整条链自上而下全部一致。
        // ★2026-11-16 用户定调 (照观者 mod 卡牌预览悬浮解释的做法): 卡牌预览时额外显示
        //   「姿态 + 逆生一重 + 逆生二重 + 逆生三重」的解释窗 (见 yylHoverTipFactory.RebirthStanceChain);
        //   现有的 FromCardChain 卡面预览链保留, WithTips 多次调用叠加、按添加顺序显示。
        WithTips(_ => yylHoverTipFactory.RebirthStanceChain(minLevel: 1));
        WithTips(card => yylHoverTipFactory.FromCardChain<RebirthTwo>(card.IsUpgraded));
        // WithKeyword(CardKeyword.Innate, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await StanceCmd.EnterReverseLife1(ctx, Owner, cardPlay.Card);
        await yylCmd.GiveCard<RebirthTwo>(Owner, PileType.Hand, CardPilePosition.Random, upgraded: IsUpgraded);
    }
}
