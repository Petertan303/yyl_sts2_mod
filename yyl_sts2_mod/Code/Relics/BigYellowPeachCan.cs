using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     大瓶黄桃罐头 (Ancient): 战斗开始时获得 5 炁, 每点炁额外造成 20% 伤害,
///     并将所有人(包括队友)视作奶龙。
///     <para>
///         "视所有人为奶龙"复用了 <see cref="NlPowerPlus" /> 的施加逻辑
///         (与 <see cref="NlCan2" /> 一致); 5 炁与 +20%/层 伤害为本遗物独占。
///     </para>
///     <para>
///         伤害叠加: 与 Qi 的 10%/层相乘 — 例如 5 炁时, Qi 给 1.5x, 大瓶给 2.0x, 合计 3.0x。
///     </para>
///     <remarks>通过第一回合抽牌前的钩子执行，避免每次抽牌重复结算。</remarks>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class BigYellowPeachCan : yylRelicModel, IModifyDamageMultiplicative
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    /// <summary>Initial Qi on combat start.</summary>
    public const int InitialQi = 5;

    /// <summary>Extra damage per Qi on top of the base Qi 10%/stack.</summary>
    public const decimal PerStackDamageBonus = 0.20m;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        // 1) 5 炁
        await yylCmd.GainQi(choiceContext, player, InitialQi, this, null);
        // 2) 把所有人视作奶龙
        var targets = combatState.Allies
            .Append(Owner.Creature)
            .Concat(combatState.HittableEnemies)
            .Where(c => c.IsAlive)
            .Distinct();
        await PowerCmd.Apply<NlPowerPlus>(choiceContext, targets, 1m, Owner.Creature, null);
    }

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // 仅对 yyl 自己造成的伤害生效
        if (dealer != Owner.Creature) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        var qi = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (qi <= 0) return 1m;
        return 1m + PerStackDamageBonus * qi;
    }
}
