using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     寸劲: 0 费, 造成 3 点伤害。<b>你每持有 3 点炁, 额外造成 2 → 3 点伤害</b>。
///     <para>
///         与「生生不息」的区别: 那张读<b>本场战斗累计获炁量</b>(只增不减, 越打越高),
///         本张读<b>当前持有的炁量</b>(花掉炁就立刻变弱) —— 因此它是一张
///         「把存量炁换成爆发」的牌, 与「食炁」「天火」这类耗炁牌形成配合/制约关系。
///     </para>
///     <para>
///         ★ 2026-10-07 改为 <b>0 费</b>（用户定调）: 定位是"合格的 0 费输出", 前中期就能稳定打出,
///         不像 1 费 / 2 费那样会在道中卡手。斜率也从「每 1 点炁」改为「<b>每 2 点炁</b>」,
///         把线性成长压平一半, 避免高炁时费伤比爆炸（旧版 30 炁升级后可打 90 点）。
///     </para>
///     <para>
///         ★ 2026-10-15 再压一档（用户定调）: 斜率「每 2 点炁」→「<b>每 3 点炁</b>」,
///         稀有度 Uncommon → <b>Common</b>。0 费是它的立身之本, 不动; 砍的是斜率与稀有度。
///         升级后锚点: 3 炁 → 6 点 / 6 炁 → 9 点 / 15 炁 → 18 点 / 30 炁 → 33 点
///         (旧版同条件 6 / 12 / 24 / 48, 高炁段降约三成)。
///     </para>
///     <para>
///         ★变量结构照原版「灰烬打击」<c>ASHEN_STRIKE</c>(反编译实证):
///         <c>CalculationBaseVar</c> + <c>ExtraDamageVar</c> +
///         <c>CalculatedDamageVar(ValueProp.Move).WithMultiplier(...)</c>,
///         引擎 <c>CalculatedVar.Calculate()</c> 算:<c>CalculationBase + ExtraDamage × multiplier</c>。
///         这里 multiplier = <b>当前持有的炁量</b>, 于是卡面显示值 = 实际伤害值, 天然一致。
///         写法用 BaseLib 的 <c>WithCalculatedDamage</c> 官方封装。
///         ★两个 upgrade 参数含义不同: <c>upgrade</c> = 基础值增量(<c>DamageUpgrade</c>),
///         <c>bonusUpgrade</c> = 每点炁加成的增量(<c>PerQiUpgrade</c>)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class CunJin(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>基础伤害 (与持有炁量无关)。</summary>
    public const int BaseDamage = 3;

    /// <summary>基础伤害升级增量 (当前 0: 基础值不随升级变化, 升级只加每 3 炁那一项)。</summary>
    public const int DamageUpgrade = 0;

    /// <summary>每 <b>3 点</b>持有炁的加伤 (基础)。</summary>
    public const int PerQiBonus = 2;

    /// <summary>每 3 炁加伤的升级增量: 2 → 3。</summary>
    public const int PerQiUpgrade = 1;

    public CunJin() : this(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // ★ 0 费「合格输出」牌(用户 2026-10-07 定调): 基础 3 + 每 3 点持有炁 2→3。
        //   [2026-10-15] 斜率 2 点 → 3 点一档, 稀有度降为 Common。
        //   multiplier = **floor(持有炁 / 3)** —— 必须向下取整, 这是用户给的锚点反推出来的:
        //     初始 3 炁 → floor(1)=1 档   → 3+2=**5** 点(未敲) / 3+3=6(敲后)
        //     共 6 炁   → floor(2)=2 档   → 3+4=**7** 点(未敲) / 3+6=9(敲后)
        //    共 15 炁   → floor(5)=5 档   → 3+10=**13** 点(未敲) / 3+15=18(敲后)
        //    共 30 炁   → floor(10)=10 档 → 3+20=**23** 点(未敲) / 3+30=33(敲后)
        //   (旧版「每 2 点一档」在同样炁量下是 6 / 12 / 24 / 48, 高炁段砍掉约三成。)
        //   若用小数除法(不取整), 3 炁会得到 3+2×1=5 点但 4 炁会变 3+2×1.33=5.66, 卡面出现小数。
        //   照 ASHEN_STRIKE 同构; multiplier 必须是 **static** 委托(引擎检查
        //   multiplierCalc.Target is AbstractModel 会抛 "Multiplier calc must be static!"),
        //   且战斗外引擎强制返回 0 ⇒ 卡牌图鉴里回落显示基础值 3。
        WithCalculatedDamage(
            BaseDamage,
            PerQiBonus,
            static (card, _) => Math.Floor((card.Owner?.Creature.GetPower<Qi>()?.Amount ?? 0m) / 3m),
            upgrade: DamageUpgrade,
            bonusUpgrade: PerQiUpgrade);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 卡面伤害的同源手算 (CalculatedDamageVar 没有到 decimal 的转换,
        // 延迟映射需要数值): base + per × floor(持有炁 / 3), 与构造函数公式逐项一致。
        var per = IsUpgraded ? PerQiBonus + PerQiUpgrade : PerQiBonus;
        var totalDamage = BaseDamage + per * Math.Floor((Owner.Creature.GetPower<Qi>()?.Amount ?? 0m) / 3m);

        /*  ★2026-11-16 用户定调: 攻击前按伤害加聚光灯前摇 —— 伤害 10~100 每份 0.45~0.75s
            线性 (yylVfx.DamageCastDelay); ★2026-11-16 手感定调: 寸劲时延 0.6 份 /
            聚光 0.6 层, 生生不息 1 份 / 1 层, 天火约 3 份 / 3 层。
            低于 10 伤不触发特效和延时, 直接结算。
            ★2026-11-16 用户定调: 突刺/点破 —— 命中瞬间用原版 Finisher/Skewer 同款
            NStabVfx (WithHitVfxNode), 替换原 vfx_dramatic_stab; 大斩击已挪给生生不息。
            前摇结束不再单独放特效, 聚光暗示蓄力、命中瞬间一点穿透。 */
        // ★2026-11-16 用户定调: 门槛/斜率按**实际造成的伤害**(含炁增伤/易伤等所有乘区),
        // 不是卡面基础值 —— 走引擎 Hook.ModifyDamage 预览管线取最终伤害。
        var finalDamage = FinalDamagePreview.ForCard(this, Owner, cardPlay.Target, totalDamage);
        // ★2026-11-16 手感定调: 时延 0.6 份、聚光 0.6 层。
        var preDelay = yylVfx.DamageCastDelay(finalDamage, level: 0.6f, spotlightLayers: 0.6f);
        MainFile.Logger.Info(
            $"[yyl] 寸劲: 当前持有炁={Owner.Creature.GetPower<Qi>()?.Amount ?? 0m}, " +
            $"档数={Math.Floor((Owner.Creature.GetPower<Qi>()?.Amount ?? 0m) / 3m)}, " +
            $"卡面基础伤害={totalDamage}, 最终伤害={finalDamage}, 前摇={preDelay:0.00}s " +
            "(最终伤害 <10 不触发前摇与特效)");
        // ★2026-11-16 用户定调: 前摇只聚光蓄力 (DamageCastDelay 内部已开聚光),
        // 不再在前摇结束时单独放特效 —— 特效移到命中瞬间的 NStabVfx, 避免两次突刺撞车。
        // 审判大斩击已挪给生生不息。
        if (preDelay > 0f)
            await Cmd.Wait(preDelay);

        // ★与卡面严格同源: 结算用引擎的 CalculatedDamageVar 重载(会按当前 multiplier 重算),
        // 不用手算的 decimal —— 手算只服务于"按伤害算前摇"的判断。
        // ★2026-11-16 用户定调: 命中瞬间挂原版「终结技」Finisher/Skewer 同款 NStabVfx
        // (Create(target, true, VfxColor.Red)), 契合一寸短促穿透感。Func 必须是 static。
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(static target => NStabVfx.Create(target, true, VfxColor.Red))
            .Execute(choiceContext);
    }
}
