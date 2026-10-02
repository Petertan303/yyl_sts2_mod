using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     「蛇花咬」专属伤害变量 —— 照原版 <c>OstyDamageVar</code>（戳击 POKE 同款）的思路：
///     卡面预览时伤害<b>不经过玩家自己的增幅器</b>（炁/金光系都按 dealer 判归属）。
///     <para>
///         与原版的差别: OstyDamageVar 以 <c>card.Owner.Osty</c> 作为 dealer 传进
///         <see cref="Hook.ModifyDamage" />，而我们的蛇花小姐不是 Osty 类型
///         （Osty 是 sealed，且 Player.Osty 不属于伊林），所以这里直接传
///         <b>dealer = null</b> —— 任何"按来源判归属"的增幅器都不会命中，
///         附魔增伤等全局部分照常参与。
///     </para>
/// </summary>
public class PetDamageVar : DynamicVar
{
    public const string DefaultName = "PetDamage";

    public ValueProp Props { get; set; }

    public PetDamageVar(decimal damage, ValueProp props)
        : base(DefaultName, damage)
    {
        Props = props;
    }

    public PetDamageVar(string name, decimal damage, ValueProp props)
        : base(name, damage)
    {
        Props = props;
    }

    public override void UpdateCardPreview(
        CardModel card,
        CardPreviewMode previewMode,
        Creature? target,
        bool runGlobalHooks)
    {
        var num = BaseValue;

        // 附魔加成照常参与 (与 OstyDamageVar 一致)。
        var enchantment = card.Enchantment;
        if (enchantment != null)
        {
            num += enchantment.EnchantDamageAdditive(num, Props);
            num *= enchantment.EnchantDamageMultiplicative(num, Props);
            if (!card.IsEnchantmentPreview)
            {
                EnchantedValue = num;
            }
        }

        if (runGlobalHooks)
        {
            var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
            // dealer = null: 不命中任何按来源判定的增幅器 (炁/狂热/驭龙等)。
            num = Hook.ModifyDamage(
                card.Owner.RunState, combatState, target, null, BaseValue, Props, card, null,
                ModifyDamageHookType.All, previewMode, out _);
        }

        PreviewValue = num;
    }
}
