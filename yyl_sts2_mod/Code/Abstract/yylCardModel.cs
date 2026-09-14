using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Extensions;
// using yyl_sts2_mod.Code.Core;
// using yyl_sts2_mod.Code.DynamicVars;
// using yyl_sts2_mod.Code.Extensions;
// using yyl_sts2_mod.Code.Keywords;
// using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Abstract;

public abstract class yylCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public sealed override string CustomPortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
