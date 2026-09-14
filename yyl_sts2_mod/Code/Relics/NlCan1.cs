using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
public sealed class NlCan1 : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        // var enemies = combatState.Enemies.Where(e => e.IsAlive);
        // foreach (var enemy in enemies)
        // {
        //     await PowerCmd.Apply<NlPower>(choiceContext, enemy, 1m, Owner.Creature, null);
        // }
        await PowerCmd.Apply<NlPower>(choiceContext, combatState.HittableEnemies, 1m, Owner.Creature, null);
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<NlCan2>();
    }
}