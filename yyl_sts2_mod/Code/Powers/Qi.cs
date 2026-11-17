using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

public sealed class Qi : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     对数 + 线性两段增伤 (2026-11-17 用户定调, 取代 11-15 的两段线性):
    ///       Q ≤ 12 : 对数 1 + 0.27291·ln(1+Q)  —— 前段收益递减, 12 炁恰好 ×1.70
    ///       Q > 12 : 每点 +2.25%              →  1.70 + 0.0225·(Q−12)
    ///     系数 0.27291 = 0.70 / ln(13), 令 12 炁正好 ×1.70 (旧对数系数 0.27 时为 1.694)。
    ///     锚点: 3炁 ×1.378 (开局 ≈ 旧 1.374 / 两段线性 1.375, 基准不变),
    ///           8炁 ×1.600, 12炁 ×1.700 (甜蜜点), 33炁 ×2.172, 151炁 ×4.827。
    ///     设计意图: 前段对数递减、12 炁给一个够得着的甜点; 之后固定 2.25%/点,
    ///       叠出超高炁仍有回报 (回应"叠炁无法作为终端"的反馈), 同时把旧曲线的
    ///       151 炁 ×7.72 二次增长压到 ×4.83。祖炁形态走独立单段线性, 不受本曲线影响。
    /// </summary>
    public const decimal LogCoeff = 0.27291m;
    public const decimal LogCap = 12m;
    public const decimal LogCapBonus = 0.70m;   // 12 炁时的加成 (倍率 = 1 + 此值 = 1.70)
    public const decimal FarSlope = 0.0225m;

    /// <summary>
    ///     「祖炁形态」下每 1 点炁的加成百分比取自 <see cref="ZuQiForm" /> 能力的层数
    ///     (基础 9 / 升级 12.5), 不分段、全程线性, 便于直接通过卡牌数值调整强度。
    /// </summary>

    // ── 特效刷新 (纯视觉层, 异常不外抛) ──────────────────────────────────────
    // 炁首次获得时点亮丹田气旋。层数变化由 QiVfx 内部幂等订阅 DisplayAmountChanged 驱动
    // (DevConsole SetAmount 也能捕获); 移除时注销订阅并熄灭。
    public override Task AfterApplied(Creature target, CardModel source)
    {
        try
        {
            QiVfx.Refresh(target ?? Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 炁vfx AfterApplied 失败: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature target)
    {
        try
        {
            QiVfx.Refresh(target ?? Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 炁vfx AfterRemoved 失败: {ex.Message}");
        }
        QiVfx.Unsubscribe(this);
        return Task.CompletedTask;
    }

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered))
        {
            // 「守势」: 把炁的攻击收益换成金光的防御收益 —— 有守势时炁不提供增伤。
            if (Owner.HasPower<ShouShi>()) return 1m;
            if (Amount <= 0) return 1m;

            // 「祖炁形态」: 单段线性, 取代默认的两段曲线 (8 炁后的斜率衰减消失)。
            // 每 1 点炁的加成百分比取自形态能力自身的层数 (基础 9 / 升级 12.5)。
            var form = Owner.GetPower<ZuQiForm>();
            decimal bonus;
            if (form != null)
            {
                bonus = (decimal)form.Amount / 100m * Amount;
            }
            else
            {
                // 默认: Q ≤ 12 对数 (12 炁 ×1.70), 之后每点 +2.25% 线性。
                var stacks = Math.Max(0m, Amount);
                bonus = stacks <= LogCap
                    ? LogCoeff * (decimal)Math.Log((double)(1m + stacks))
                    : LogCapBonus + FarSlope * (stacks - LogCap);
            }
            return 1m + bonus;
        }
        return 1m;
    }
}
