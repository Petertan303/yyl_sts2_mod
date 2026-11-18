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
using yyl_sts2_mod.Code.Cards.Token;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     血雷: 1 费, 造成 16 → 22 点伤害, 将一张[内伤]加入你的手牌。
///     2026-11-18 用户定调: 原 22 → 32 伤害 + 塞 2 张内伤过高, 下调为 16 → 22 + 1 张;
///     同时内伤由不可打出 (-1 费) 改为 3 费可打出 (花 3 能量清掉手牌里的账单)。
///     高数值 + 明确的后续代价 (内伤在手里每回合结束都会掉血),
///     属于"负面 + 强正面"的稀有位。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BloodThunder(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BloodThunder() : this(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(16, 6); // 基础 16, 升级 +6 → 22
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
        await yylCmd.GiveCards<LaShang>(Owner, 1, PileType.Hand, skipAnimation: true);
    }
}
