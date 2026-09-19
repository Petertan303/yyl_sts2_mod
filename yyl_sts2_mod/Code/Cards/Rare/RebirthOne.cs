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
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     逆生一重: 3 费, 进入逆生一重姿态, 并把「逆生二重」加入手牌。
///     逆生三重姿态链的入口; 基础带消耗, 升级后添加保留。
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
    public RebirthOne() : this(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithKeywords(CardKeyword.Exhaust);
        HoverTipFactory.FromCardWithCardHoverTips<RebirthTwo>()
            .Select(m => new TooltipSource(_ => m))
            .ToList()
            .ForEach(t => WithTip(t));
        // WithKeyword(CardKeyword.Innate, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await StanceCmd.EnterReverseLife1(ctx, Owner, cardPlay.Card);
        await yylCmd.GiveCard<RebirthTwo>(Owner, PileType.Hand, CardPilePosition.Random, upgraded: IsUpgraded);
    }
}
