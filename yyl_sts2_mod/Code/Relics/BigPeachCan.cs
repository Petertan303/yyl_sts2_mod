using System.Collections.Generic;
using System.Linq;
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
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     大瓶黄桃罐头 (先古): 合并了原「心魔」与「大瓶黄桃罐头」。
///     战斗第一回合抽牌前: 获得 5 点炁; 将玩家自身、队友与所有敌人视作心魔(奶龙);
///     并播放奶龙的声音。持有时每点炁额外造成 20% 伤害。
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class BigPeachCan : yylRelicModel, IModifyDamageMultiplicative
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    /// <summary>战斗开始时获得的炁。</summary>
    public const int InitialQi = 5;

    /// <summary>每点炁在炁自身 10% 之外额外提供的伤害加成。</summary>
    public const decimal PerStackDamageBonus = 0.20m;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        await yylCmd.GainQi(choiceContext, player, InitialQi, this, null);

        // 心魔: 把所有人(自己、队友、敌人)都视作奶龙
        var targets = combatState.Allies
            .Append(Owner.Creature)
            .Concat(combatState.HittableEnemies)
            .Where(c => c.IsAlive)
            .Distinct();
        await PowerCmd.Apply<InnerDemon>(choiceContext, targets, 1m, Owner.Creature, null);

        // 心魔 buff 被赋予实体时: 发出奶龙的声音
        yylAudio.PlaySfx(yylAudio.Sfx("nailong/nailong_voice.ogg"), 0.9f);
    }

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner.Creature) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        var qi = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (qi <= 0) return 1m;
        return 1m + PerStackDamageBonus * qi;
    }
}
