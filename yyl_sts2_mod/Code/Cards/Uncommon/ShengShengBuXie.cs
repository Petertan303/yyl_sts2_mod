using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     生生不息: 2 费, <b>无基础伤害</b>。<b>本场战斗每获得过 3 点炁, 额外造成 4 → 6 点伤害</b>。
///     <para>
///         ★ 2026-10-07 改为 <b>2 费</b>（用户定调）: 定位为<b>上限卡</b>。
///         初始 3 炁时 2 费只打 6 点（费伤比 3.0，远低于打击的 6.0）—— 前期很弱,
///         但斜率不变、且读的是<b>累计</b>获炁量, 所以越到后期越夸张:
///         20 炁 2 费 40 点、30 炁 2 费 60 点（敲后 90）。
///     <para>
///         ★ 2026-10-15 斜率由「每 1 点累计炁 2→3」改为「<b>每 3 点累计炁 4→6</b>」
///         （用户定调: 不加人为上限, 改为把斜率压平 —— 等效每点炁 3 → 2 点, 整体砍掉三分之一）。
///         升级后锚点（累计获炁 = 持有量的 2 倍估算）:
///         累计 10 → 18 点 / 累计 30 → 60 点 / 累计 60 → 120 点
///         (旧版同条件 30 / 90 / 180)。仍然<b>线性无顶</b> —— 这是刻意的:
///         它靠"累计"这一读法与寸劲区分, 不该被封顶抹平。
///     </para>
///         与「寸劲」（0 费 / 读<b>当前</b>持有炁）形成对照:
///         <list type="bullet">
///             <item><b>寸劲</b> = 过渡牌: 0 费随时可打, 但花炁就立刻变弱。</item>
///             <item><b>生生不息</b> = 上限牌: 2 费要攒资源, 但耗炁构筑里唯一能持续打满的成长牌。</item>
///         </list>
///     </para>
///     <para>
///         机制照搬原版「灰烬打击」<c>ASHEN_STRIKE</c> —— 它的三个变量是
///         <c>CalculationBaseVar(6)</c> + <c>ExtraDamageVar(3)</c> +
///         <c>CalculatedDamageVar(ValueProp.Move).WithMultiplier(乘「消耗堆」张数)</c>,
///         引擎在 <c>CalculatedVar.Calculate</c> 里算:
///         <c>CalculatedDamage = CalculationBase + ExtraDamage × multiplier</c>。
///         我们换成伊林的炁: multiplier = <see cref="QiGainTracker.CombatAmount" />
///         (本场战斗累计获炁量, 在 <c>yylCmd.GainQi</c> 内打标, 全 mod 唯一产炁入口,
///         计数用修正后实际入账的量)。于是它是一张<b>局内成长</b>牌:
///         前期炁少伤害低, 后期炁堆起来后一发可期。
///     </para>
///     <para>
///         ★ <b>为什么必须用 CalculatedVar 而不是 WithDamage + 手动加算</b>:
///         卡面显示的是 <c>DynamicVar._previewValue</c>, 只有 CalculatedVar 家族才会
///         在预览时按 multiplier 重算。之前用 <c>WithDamage(5,0)</c> +
///         <c>WithVar("PerQiBonus",2,1)</c> 再在 OnPlay 里自己加, 卡面只能显示
///         「5 + 2」两个孤立数字, <b>与实际打出的伤害完全不符</b>;
///         而这里用一个 <c>CalculatedDamageVar</c>, 卡面显示值 = 实际伤害值, 天然一致。
///         写法即 BaseLib 的 <c>WithCalculatedDamage(baseVal, multVal, mult, ...)</c>,
///         它内部就是上面那三件套的官方封装。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShengShengBuXie(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>基础伤害 (固定, 不随炁变化)。</summary>
    public const int BaseDamage = 0;

    /// <summary>本场战斗每 3 点炁提供的额外伤害。</summary>
    public const int PerQiBonus = 4;

    /// <summary>升级时每 3 点炁的额外伤害增量 (4 → 6)。</summary>
    public const int PerQiBonusUpgrade = 2;

    public ShengShengBuXie() : this(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // ★ 与原版 AshenStrike 完全同构:
        //   WithCalculatedDamage(baseVal, multVal, mult, props, upgrade, bonusUpgrade)
        //   展开后= CalculationBaseVar(5) + ExtraDamageVar(2) + CalculatedDamageVar(Move).WithMultiplier(炁量)
        //   卡面 {CalculatedDamage} 随炁实时变化, 与 OnPlay 里实际打的伤害同源。
        // 基础伤害不升级 (upgrade=0), 只有「每 3 点炁」这一项 4 → 6。
        // multiplier = floor(本场累计获炁 / 3), 向下取整 (与寸劲同款, 卡面不出现小数)。
        WithCalculatedDamage(
            BaseDamage,
            PerQiBonus,
            static (card, _) => Math.Floor(QiGainTracker.CombatAmount(card.Owner, card.CombatState) / 3m),
            upgrade: 0,
            bonusUpgrade: PerQiBonusUpgrade);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 卡面伤害的同源手算 (CalculatedDamageVar 没有到 decimal 的转换,
        // 延迟映射需要数值): per × floor(本场累计获炁 / 3), 与构造函数公式逐项一致。
        var per = IsUpgraded ? PerQiBonus + PerQiBonusUpgrade : PerQiBonus;
        var totalDamage = per * Math.Floor(QiGainTracker.CombatAmount(Owner, Owner.Creature.CombatState) / 3m);

        /*  ★2026-11-16 用户定调: 攻击前按伤害加聚光灯前摇 —— 伤害 10~100 每份 0.45~0.75s
            线性 (yylVfx.DamageCastDelay); ★2026-11-16 手感定调: 生生不息时延 1 份 /
            聚光 1 层, 寸劲 0.6 份 / 0.6 层, 天火约 3 份 / 3 层。
            低于 10 伤不触发特效和延时, 直接结算。
            ★2026-11-16 用户定调: 延时结束触发原版「审判」大斩击特效 (yylVfx.BigSlash:
            NBigSlashVfx + NBigSlashImpactVfx + heavy_attack.mp3), 替换原崩拳 GrandFinaleImpact
            —— 大斩击的厚重感更配生生不息「累计获炁越打越高」的爆发定位。 */
        // ★2026-11-16 用户定调: 门槛/斜率按**实际造成的伤害**(含炁增伤/易伤等所有乘区),
        // 不是卡面基础值 —— 走引擎 Hook.ModifyDamage 预览管线取最终伤害。
        var finalDamage = FinalDamagePreview.ForCard(this, Owner, cardPlay.Target, totalDamage);
        // ★2026-11-16 手感定调: 时延 1 份、聚光 1 层。
        var preDelay = yylVfx.DamageCastDelay(finalDamage, level: 1f, spotlightLayers: 1f);
        MainFile.Logger.Info(
            $"[yyl] 生生不息: 累计获炁={QiGainTracker.CombatAmount(Owner, Owner.Creature.CombatState)}, " +
            $"卡面基础伤害={totalDamage}, 最终伤害={finalDamage}, 前摇={preDelay:0.00}s");
        if (preDelay > 0f)
        {
            await Cmd.Wait(preDelay);
            yylVfx.BigSlash(cardPlay.Target);
        }

        // ★与卡面严格同源: 结算用引擎的 CalculatedDamageVar 重载(它会按当前 multiplier 重算),
        // 不用手算的 decimal —— 手算只服务于"按伤害算前摇"的判断。
        // ★2026-11-16 用户定调: 命中敌人时挂水波纹 (金光佛光同款 shader), 面积 2 倍、
        // 峰值更弱、挂在命中敌人身上; 在伤害结算后触发。
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        yylVfx.WaterRipple(
            cardPlay.Target,
            raiseFraction: 0f,
            sizePx: 1600f,          // 金光 800f 的 2 倍
            duration: 1.3f,
            peakIntensity: 0.0035f, // 金光 0.005f 的 70% (2026-11-16 手感微调)
            scaleMul: 1f);
    }
}