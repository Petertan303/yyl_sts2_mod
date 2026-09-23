using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     金光咒: 2 费, 获得 8 → 12 点格挡, 并获得 2 层金光护体 (每层受伤 -2) — 2026-09-22 由 1 层上调。
///     金光护体体系的第一条获取途径; 防御端核心牌。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GoldenCharm(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GoldenCharm() : this(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithBlock(8, 4);
        // 2026-09-22 用户定调: 初始金光咒给予 2 层金光护体 (原 1 层)。
        WithPower<GoldenAegis>(2);
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}