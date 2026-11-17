using System;
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
///     思维膨大: **0 费**。造成 4 点伤害 <b>本回合每获得 1 点炁, 追加一次</b>。
///     <para>
///         ★ [2026-10-20 用户定调] 每段 5 → 8 改为 <b>4 → 6</b> (与原版多段卡标准数值对齐)。
///         段数机制不变 (1 + 本回合获炁)。锚点: 本回合产 10 炁 = 11 段 × 6 = 66 点 (旧 88);
///         产 20 炁 = 21 段 × 6 = 126 点 (旧 168)。
///     </para>
///     <para>
///         与「生生不息」(按<b>本场战斗</b>累计获炁加伤) 的区别: 本卡是<b>本回合</b>、
///         <b>转成段数</b>而非加伤 —— 打的是「量」而不是「单发强度」,
///         因此 0 费也合理(段数随资源增长, 但每一段只有 4 点)。
///     </para>
///     <para>
///         ★动态段数的写法照原版储君「决胜一击」<c>FINISHER</c> (反编译实证) ——
///         它是全引擎<b>唯一</b>用 <c>WithHitCount(动态值)</c> 的卡, 做法是:
///         <code>
///         new DamageVar(6m, ValueProp.Move),                        // 每段伤害
///         new CalculationBaseVar(0m), new CalculationExtraVar(1m),  // 段数 = 0 + 1 × multiplier
///         new CalculatedVar("CalculatedHits").WithMultiplier(本回合已打出的攻击牌数)
///         // OnPlay: WithHitCount((int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(target))
///         </code>
///         即<b>段数本身也是一个 CalculatedVar</b>, 卡面用
///         <c>{InCombat:\n(Hits {CalculatedHits:diff()} ...)|}</c> 显示
///         (原版文案实证: "Deal {Damage} damage for each Attack already played this turn.
///         {InCombat:\n(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}")
///         —— <c>{InCombat:|}</c> 保证<b>战斗外不显示段数</b>。
///         我们换乘数: multiplier = 本回合累计获炁量(<see cref="QiGainTracker.RoundAmount" />)。
///     </para>
///     <para>
///         ★ <b>刻意保留下限 1 段</b>: 引擎 <c>CalculatedVar.Calculate</c> 在战斗外强制
///         multiplier 返回 0, 而本回合没产炁时 multiplier 也是 0 —— 段数必须钳到
///         至少 1, 否则会出现「0 费牌打出 0 点伤害」这种废牌。
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
    /// <summary>段数变量名 (卡面用 <c>{Hits}</c> 绑定)。</summary>
    public const string HitsVarName = "Hits";

    /// <summary>每段伤害。</summary>
    public const int PerHitDamage = 4;

    /// <summary>每段伤害的升级增量: 4 → 6 (与原版多段卡标准数值对齐, 2026-10-20 用户定调)。</summary>
    public const int PerHitUpgrade = 2;

    public SiWeiPengDa() : this(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(PerHitDamage, PerHitUpgrade);

        // ★ 段数 = HitsBase(1) + HitsExtra(1) × 本回合累计获炁量, 再钳到至少 1 段。
        //   multiplier 必须是 **static** 委托(引擎会检查并抛
        //   "Multiplier calc must be static!"); 战斗外引擎强制返回 0 ⇒ 卡面不显示段数。
        WithCalculatedVar(
            HitsVarName,
            1,      // HitsBase: 至少 1 段
            1,      // HitsExtra: 每点炁追加 1 段
            static (card, _) => QiGainTracker.RoundAmount(card.Owner, card.CombatState));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 段数与卡面同源(同一个 CalculatedVar), 再钳到至少 1 段。
        // ★ DynamicVarSet 的索引器返回 DynamicVar(基类没有 Calculate),
        //   必须像原版 Finisher 那样强转成 CalculatedVar 才能调 Calculate。
        var hitsVar = (CalculatedVar)DynamicVars[HitsVarName];
        var hits = hitsVar.Calculate(cardPlay.Target);
        var hitCount = (int)Math.Max(hits, 1m);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
