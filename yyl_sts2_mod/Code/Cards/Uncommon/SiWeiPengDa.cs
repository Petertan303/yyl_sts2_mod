using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     思维膨大: **0 费**。造成 5 点伤害 <b>本回合每获得 1 点炁, 追加一次</b>。
///     <para>
///         与「生生不息」(按<b>本场战斗</b>累计获炁加伤) 的区别: 本卡是<b>本回合</b>、
///         <b>转成段数</b>而非加伤 —— 打的是「量」而不是「单发强度」,
///         因此 0 费也合理(段数随资源增长, 但每一段只有 5 点)。
///     </para>
///     <para>
///         段数由 <see cref="QiGainTracker.RoundAmount" /> 提供(本回合累计获炁量)。
///         刻意<b>不加下限</b>: 本回合没产过炁就只打 5 点(相当于一张 0 费白卡),
///         这让它的上限完全由玩家本回合的资源投入决定。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class SiWeiPengDa(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public SiWeiPengDa() : this(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithVars(new RepeatVar(1));
        WithDamage(PerHitDamage, PerHitUpgrade);
    }

    /// <summary>每段伤害。</summary>
    public const int PerHitDamage = 5;

    /// <summary>每段伤害的升级增量: 5 → 8。</summary>
    public const int PerHitUpgrade = 3;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 本回合累计获炁量 → 段数(至少 1 段, 保证这张牌永远至少打出 5 点)。
        var qiThisRound = QiGainTracker.RoundAmount(Owner, Owner.Creature.CombatState);
        var hits = Math.Max(1, qiThisRound);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitCount(hits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}