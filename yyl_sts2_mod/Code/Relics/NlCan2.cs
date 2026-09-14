using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Relics;

[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class NlCan2 : yylRelicModel
{
    // 通常升级替换的遗物稀有度可以视情况定义
    public override RelicRarity Rarity => RelicRarity.Ancient; 

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        // 组合当前战斗所有的实体：敌人 + 玩家的队友 + 玩家自己
        // var allCreatures = combatState.Enemies
        //     .Concat(combatState.GetTeammatesOf(player.Creature))
        //     .Append(player.Creature)
        //     .Where(c => c != null && c.IsAlive);
        //
        // foreach (var creature in allCreatures)
        // {
        //     await PowerCmd.Apply<NlPowerPlus>(choiceContext, creature, 1m, null, null);
        // }
        if (!Owner.Creature.HasPower<NlPowerPlus>())
            await PowerCmd.Apply<NlPowerPlus>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
        await PowerCmd.Apply<NlPowerPlus>(choiceContext, combatState.Allies, 1m, Owner.Creature, null); 
        await PowerCmd.Apply<NlPowerPlus>(choiceContext, combatState.HittableEnemies, 1m, Owner.Creature, null);
    }
}