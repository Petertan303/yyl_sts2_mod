using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     抢步: 0 费, 抽 2 张, 消耗; 升级附加保留 (Retain)。
///     <para>
///         [balance 2026-09-19] 用户定调: 固定抽 2 (不再升级加抽),
///         升级改为附加 Retain —— 留在手里下回合再用一次。
///         暂时移出卡池中: 收起方式 = 稀有度 Basic (Basic 不进卡牌奖励池, 同打击/防御)。
///         ⚠ 不能用"注释 [Pool]"的方式 —— BaseLib 要求每个 CardModel 必须带
///         PoolAttribute, 缺失会在游戏启动时直接抛异常 (v0.111.0 实测)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QuickStep(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QuickStep() : this(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithCards(2, 0);
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Draw(this, choiceContext);
    }
}
