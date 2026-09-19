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

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     引光: 1 费, 获得 1 层金光护体, 抽 1 → 2 张。
///     金光获取被刻意压得很稀 (初始遗物已经自带), 所以这张走"顺带过一张牌"的路线。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinGuang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinGuang() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<GoldenAegis>(1);
        WithCards(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
