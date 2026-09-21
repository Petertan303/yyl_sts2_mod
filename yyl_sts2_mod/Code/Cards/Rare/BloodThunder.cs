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
///     血雷: 2 费, 造成 22 → 28 点伤害, 将一张[拉伤]加入你的弃牌堆。
///     高数值 + 明确的后续代价 (拉伤在手里每回合结束都会掉血),
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
    public BloodThunder() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(22, 6);
        // ★耗炁限制 (2026-09-21): 打出时失去 2 点炁; 炁不足则"以血代炁" (失去 6 点生命)。
        //   效果本身不变 —— 代价只是从炁换成血, 给产炁体系一个稳定的消耗出口。
        WithPower<Qi>("QiLoss", 2, 0);
        WithCalculatedDamage("HpLoss", 6, (_, _) => 0m, 0, 0, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PayQiOrHp(choiceContext, cardPlay);
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
        await yylCmd.GiveCard<LaShang>(Owner, PileType.Discard, skipAnimation: true);
    }
}
