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
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        var targets = combatState.Allies
            .Append(Owner.Creature)
            .Concat(combatState.HittableEnemies)
            .Where(c => c.IsAlive)
            .Distinct();
        await PowerCmd.Apply<NlPowerPlus>(choiceContext, targets, 1m, Owner.Creature, null);
    }
}
