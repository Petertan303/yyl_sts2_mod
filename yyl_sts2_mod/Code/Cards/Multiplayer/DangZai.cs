using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Multiplayer;

/// <summary>
///     挡灾: 1 费联机卡 (罕见)。所有队友获得心防 (来自奶龙伤害减半)；
///     伊林自身承受来自奶龙的伤害翻倍 (龙怒)。无队友时不生效。
///     <para>
///         联机专属 (TargetType.AllAllies)。太极"伤害重定向"在联机里的镜像 ——
///         伊林替全队挡下奶龙的刀，与心防一减一增构成干净攻守交易。
///         龙怒作为 Debuff 可被涤荡清除 (清心咒为当回合免疫新 debuff，不清除已有)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DangZai(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    // 原版联机卡同款标记 (Sneaky/Fade/Coordinate 等): 单人局不入卡池
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public DangZai() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ★联机健壮性: Owner / Creature / CombatState 任一为空时静默退出。
        //   4 人联机的切换与预览场景会出现这些空值, 一旦把异常抛向引擎就会冻结整场战斗。
        if (Owner?.Creature is not { } self) return;
        if (self.CombatState is not { } combatState) return;

        var allies = combatState
            .GetTeammatesOf(self)
            .Where(c => c != self && c.IsAlive && c.Player != null)
            .ToList();
        if (allies.Count == 0) return;

        // 队友获得心防 (可叠加 buff, 来自奶龙伤害减半, 本回合)。
        await PowerCmd.Apply<Powers.HeartGuard>(choiceContext, allies, 1m, Owner.Creature, cardPlay.Card);
        // 伊林自身承受来自奶龙的伤害翻倍 (龙怒, 单回合 Debuff)。
        await PowerCmd.Apply<Powers.LongNu>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, cardPlay.Card);
    }
}
