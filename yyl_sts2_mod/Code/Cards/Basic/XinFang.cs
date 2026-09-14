using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     心防: 1 费, 获得 3 → 5 格挡, 本回合受到来自奶龙的伤害 -50%。
///     <para>
///         "本回合受来自奶龙的伤害减半"用 per-turn 的 SpireField 标记实现; 真正的
///         50% 减伤需要在 damage 钩子里检查 (MultiDamage patch 之后, 例如扩展
///         IModifyDamageMultiplicative 的一个新接口 IModifyDamageFromNailong)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class XinFang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    private static readonly SpireField<MegaCrit.Sts2.Core.Entities.Players.Player, bool> ActiveThisTurn = new(() => false);

    public XinFang() : this(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var block = 3; // TODO upgrade: would be 5 if IsUpgraded
        // TODO: PlayerCmd.GainBlock not found in this BaseLib - hook via yylCmd.GainBlock helper or PowerCmd equivalent
        // await PlayerCmd.GainBlock(block, Owner);
        ActiveThisTurn[Owner] = true;
        // TODO: 在 turn end 时重置 ActiveThisTurn (BeforeSideTurnStart 检查自身侧)
        // TODO: 在 damage 钩子里检查来源是否为奶龙 + ActiveThisTurn,若是返回 0.5m
    }
}

