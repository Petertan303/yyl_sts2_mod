using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     「蛇花护体」——挂在蛇花小姐身上的隐藏能力。
///     <para>
///         ★为什么需要它: 原版「为你而死」(DieForYouPower) 的重定向发生在
///         <b>未格挡段</b> (Hook.ModifyUnblockedDamageTarget)，重定向后的数额直接
///         <c>LoseHpInternal</c> 扣宠物血 —— <b>宠物的格挡永远不会被结算</b>
///         (实测: 投喂给蛇花上格挡，受到攻击仍然直接掉血)。
///     </para>
///     <para>
///         引擎在重定向之后、扣血之前留了一个钩子
///         <see cref="AbstractModel.ModifyHpLostAfterOsty" />(target 已是宠物)，
///         本能力在这里消费蛇花小姐自身的格挡，剩余部分才走扣血 ——
///         使「给蛇花上格挡」这件事真正有意义。
///     </para>
/// </summary>
public sealed class SnakeFlowerGuard : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        try
        {
            // 只处理打在蛇花小姐身上的、带来源的攻击伤害。
            if (target != Owner) return amount;
            if (amount <= 0m) return amount;
            if (!props.IsPoweredAttack()) return amount;

            var block = Owner.Block;
            if (block <= 0) return amount;

            var absorbed = Math.Min(block, amount);
            // 同步扣格挡 (Creature 公开 API; 钩子是同步的, 不能走 async 的 CreatureCmd)。
            Owner.LoseBlockInternal(absorbed);
            return amount - absorbed;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花护体结算异常: {ex.Message}");
            return amount;
        }
    }
}
