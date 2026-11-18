using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Ancient;

/// <summary>
///     辟邪剑法: 先古卡, 由先古 NPC 赠予 (坦克斯最合适 —— 台词里他就在找武器)。
///     造成 6 → 8 点伤害 ×3 段; 若目标身上带有易伤 / 虚弱 / 中毒, 本次伤害翻倍。
///     <para>
///         「辟邪」= 专克邪祟: 目标越脏, 这一剑越重。
///         和现有的铺 debuff 体系 (掌心雷 / 风后奇门 / 破防 / 投喂) 天然闭环,
///         是先古卡里"越到中后期越强"的那一张。
///     </para>
///     <para>
///         ★2026-11-18 降强度(用户定调): ①伤害 8→11 下调为 6→8 (三段翻倍态 48→66 降为 36→48);
///         ②去掉「奶龙标记」作为翻倍条件 —— 初始遗物黄桃罐头开局就把所有敌人标记成奶龙,
///         等于白送翻倍, 让"需要铺 debuff"的设计前提形同虚设。现在只认易伤/虚弱/中毒,
///         必须真的铺出负面状态才能吃到翻倍。
///         ⚠ 三段各吃一次力量 (引擎 Execute 按 hitCount 循环调用伤害管线), 故段伤不宜再高。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class WardingBlade(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public WardingBlade() : this(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
        WithDamage(6, 2); // 基础 6, 升级 +2 → 8 (三段翻倍态 36 → 48)
        WithVars(new RepeatVar(3));
        // WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (HasAnyDebuff(cardPlay.Target))
        {
            DynamicVars.Damage.BaseValue *= 2;
        }

        // 辟邪剑法: 三道剑气<b>依次落在目标身上</b> (第 2/3 道各延后 0.2s), 纵向分布在
        // 受击者的头顶 / 中段 / 脚尖 —— 飞斩是落点类特效, 挂[受击者]而非施法者
        // (区别于光束那种从施法者射出的发射类特效)。
        yylVfx.ArcVolley(cardPlay.Target!, "vfx/vfx_flying_slash",
            DynamicVars.Repeat.IntValue, intervalSeconds: 0.2f);

        await CommonActions.CardAttack(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue) // ★段数必须显式接上, 否则只结算 1 段
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>目标是否带有任一可被"辟邪"利用的负面状态。</summary>
    private static bool HasAnyDebuff(Creature? target)
    {
        if (target is not { IsAlive: true }) return false;
        // ★2026-11-18: 去掉奶龙标记 —— 初始遗物黄桃罐头开局全员标记奶龙, 会白送翻倍。
        return target.HasPower<VulnerablePower>()
               || target.HasPower<WeakPower>()
               || target.HasPower<PoisonPower>();
    }
}
