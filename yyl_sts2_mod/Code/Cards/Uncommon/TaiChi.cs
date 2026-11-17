using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     太极: 2 费, 失去 2 点炁, 本回合你和队友受到的敌方攻击转移给一名随机敌人。
///     基础消耗, 升级后不消耗 (2026-11-17 用户定调)。
///     <para>
///         机制化 AoE / 防御 (设计笔记 §6-②): 借力打力, 以彼之道还施彼身。
///         转移由 <see cref="Powers.TaiChiMark" /> + Harmony 前缀
///         (<see cref="Patches.TaiChiRedirectPatch" />) 实现: 打出期间所有以你为
///         目标的单体重定向到随机敌人; 回合结束 (敌方回合收尾) 自动散去。
///         [rule 2026-09-18] 耗炁卡统一判定: 炁不足时无额外效果。
///         [balance 历史] 曾为 3 费 3 炁 / 1 费 0 费等版本, 均已作废, 勿引用。
///         [balance 2026-10-19] 守护全队语义: 任一玩家带印记即可转嫁打在队友身上的攻击。
///         [balance 2026-11-17] 定为 2 费 2 炁、基础消耗/升级移除 (用户定调)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TaiChi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TaiChi() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        // ★2026-11-17 用户定调: 2 费 2 炁, 基础消耗、升级后移除消耗。
        WithPower<TaiChiMark>(1);
        WithPower<Qi>("QiLoss", 2, 0); // 恒定耗 2 炁 (不随升级变化)
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove); // 消耗; 升级后不消耗
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/common/vfx_common_ring_polar_a"); // 打出特效: 阴阳环 // 打出动作: 施法帧动画
        // 炁不足时无额外效果 (耗炁卡统一判定); 消耗量恒定 4, 不随升级变化。
        var cost = DynamicVars["QiLoss"].IntValue;
        var currentQi = Owner.Creature.GetPower<Qi>()?.Amount ?? 0m;
        MainFile.Logger.Info(
            $"[太极] 打出: 消耗门槛={cost}, 当前炁={currentQi}, 战斗回合={Owner.PlayerCombatState?.TurnNumber}");
        if (cost > 0 && currentQi >= cost)
        {
            await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
            var amount = DynamicVars["TaiChiMark"].IntValue;
            if (amount > 0)
            {
                await PowerCmd.Apply<TaiChiMark>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
                MainFile.Logger.Info($"[太极] 印记已施加: {amount} 层");
            }
        }
        else
        {
            MainFile.Logger.Warn($"[太极] 未触发: 炁不足 (需要 {cost}, 只有 {currentQi})");
        }
    }
}
