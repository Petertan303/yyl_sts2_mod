using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     攻击前的「最终伤害」预览 —— 走引擎同一条 <see cref="Hook.ModifyDamage" /> 管线,
///     含炁增伤、力量、易伤/虚弱等所有乘区 (即玩家实际会造成的伤害),
///     供攻击前摇的门槛/斜率判断使用。
///     <para>
///         ★2026-11-16 用户定调: 「伤害大于 10 才触发前摇」中的伤害指<b>实际造成的伤害</b>
///         (含各种增伤区间之后), 不是卡面基础值 —— 所以不能用卡面公式手算,
///         必须让引擎按真实 dealer / target 跑一遍 ModifyDamage。
///         范式照 <c>PetDamageVar.UpdateCardPreview</c>, 差别是 dealer 传真实出牌者
///         (炁/狂热等按来源判定的增幅器才生效), target 传被命中的敌人 (易伤等才生效)。
///     </para>
/// </summary>
public static class FinalDamagePreview
{
    /// <summary>
    ///     取这张卡对指定目标的一次命中在<b>当前状态</b>下会造成的最终伤害。
    ///     <paramref name="baseDamage" /> 为未乘增伤的卡面基数 (CalculatedDamageVar 的值)。
    ///     整体 try/catch: 预览失败时退回基础值, 绝不打断打牌。
    /// </summary>
    public static decimal ForCard(
        CardModel card,
        Player dealer,
        Creature target,
        decimal baseDamage,
        ValueProp props = DamageProps.card)
    {
        try
        {
            var combatState = card.CombatState ?? dealer.Creature.CombatState;
            if (combatState == null) return baseDamage;

            return Hook.ModifyDamage(
                dealer.RunState,
                combatState,
                target,
                dealer.Creature,
                baseDamage,
                props,
                card,
                null,
                ModifyDamageHookType.All,
                CardPreviewMode.Normal,
                out _);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[yyl] 最终伤害预览失败, 按卡面基数处理: {e.Message}");
            return baseDamage;
        }
    }
}
