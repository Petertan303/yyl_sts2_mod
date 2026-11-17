using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Core;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     逆生二重: 3 费衍生牌 (由逆生一重加入手牌), 进入逆生二重姿态,
///     并把「逆生三重」加入手牌。消耗; 升级后添加保留。
///     <para>
///         ★2026-11-16 用户定调: 效果由「每回合回血 6」改为
///         「<b>立即获得 8 格挡, 之后每回合开始时获得 8 格挡</b>」
///         (每回合那份在 <see cref="Stances.RebirthStanceTwo" />), 且保留一重的首格挡翻倍
///         (SetStance 进二重时不再移除一重的隐藏 power)。
///     </para>
/// </summary>
[Pool(typeof(TokenCardPool))]
public class RebirthTwo(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>二重每回合收益的格挡量 (立即那份与每回合开始那份同值)。</summary>
    public const int StanceBlock = 8;

    public RebirthTwo() : this(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithBlock(StanceBlock, 0);
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);

        // ★ 2026-10-07 修「升级后的二重/三重在预览里看不到」——两轮才彻底修对:
        //   第 1 轮: `WithTip(typeof(RebirthThree))` 走 TooltipSource 的**隐式转换 Type → TooltipSource**,
        //     内部是 HoverTipFactory.FromCard(ModelDb.GetById<CardModel>(ModelDb.GetId(t)))
        //     —— 取ModelDb 的**规范单例(永远未升级)**, 而且完全没传 upgrade 参数。
        //     ⇒ 改成了 WithTips(Func<CardModel, ...>) 动态委托 (能读到实时 card.IsUpgraded)。
        //   第 2 轮 (本轮): 动态委托对了, 但引擎 `FromCardWithCardHoverTips<T>(upgrade)` 本身有缺陷 ——
        //     它 `.Concat(ModelDb.Card<T>().HoverTips)` 取的是**规范单例**的 hovertip,
        //     而我们的 ExtraHoverTips 是**递归**的 ⇒ 规范单例 IsUpgraded 恒 false
        //     ⇒ **嵌套的下一层(三重)必然退回未升级版**。表现为「二重预览正常, 三重仍是未升级」。
        //     `yylHoverTipFactory.FromCardChain<T>` 让预览与 hovertip 共用同一份升级克隆, 整条链一致。
        // ★2026-11-16 用户定调: 额外显示「姿态 + 逆生二重 + 逆生三重」解释窗 (不含一重),
        //   照观者 mod 卡牌预览悬浮解释的做法; FromCardChain 卡面预览链保留。
        WithTips(_ => yylHoverTipFactory.RebirthStanceChain(minLevel: 2));
        WithTips(card => yylHoverTipFactory.FromCardChain<RebirthThree>(card.IsUpgraded));

        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await StanceCmd.EnterReverseLife2(choiceContext, Owner, cardPlay.Card);
        // ★2026-11-16 用户定调: 打出立即获得 8 格挡 (每回合开始那份在 RebirthStanceTwo)。
        //   用 Unpowered: 不吃一重的首格挡翻倍 (翻倍额度留给玩家自己的格挡牌),
        //   与温养/鎏金镜的姿态类格挡同口径。
        await CreatureCmd.GainBlock(Owner.Creature, StanceBlock, ValueProp.Move, null);
        await yylCmd.GiveCard<RebirthThree>(Owner, PileType.Hand, CardPilePosition.Random, upgraded: IsUpgraded);
    }
}
