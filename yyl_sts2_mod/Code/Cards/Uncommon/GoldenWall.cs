using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     金光壁: 2 费, 获得 3 → 4 层金光护体。
///     一次性堆厚金光 (金光护体源是"每回合薄薄一层")。金光获取整体很稀, 这张是少数明文来源之一。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GoldenWall(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GoldenWall() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<GoldenAegis>(3, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}
