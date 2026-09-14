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
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Relics;

[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class PeachCan : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>Initial Qi on combat start.</summary>
    public const int InitialQi = 3;

    /// <summary>第一回合抽牌前：获得 3 炁，并将所有敌人视作奶龙。</summary>
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        // 1) 获得 3 炁
        await yylCmd.GainQi(choiceContext, player, InitialQi, this, null);
        // 2) 把所有敌人视作奶龙 (NailongMark 兼任 "tag + 击杀回血 3")
        await PowerCmd.Apply<NailongMark>(choiceContext, combatState.HittableEnemies, 1m, Owner.Creature, null);
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<NlCan2>();
    }
}
