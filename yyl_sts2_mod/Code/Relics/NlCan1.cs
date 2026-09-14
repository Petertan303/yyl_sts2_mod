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
public sealed class NlCan1 : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>Initial Qi on combat start.</summary>
    public const int InitialQi = 3;

    /// <summary>SpireField guard: ensures combat-start effects only fire once per combat.</summary>
    private static readonly SpireField<MegaCrit.Sts2.Core.Entities.Creatures.Creature, bool> CombatStartFired = new(() => false);

    /// <summary>
    ///     战斗开始时: 获得 3 炁 + 把所有敌人视作奶龙(贴 NlPower tag)。
    ///     <para>
    ///         TODO(API): 临时使用 <c>BeforeHandDraw</c> + SpireField 守门实现
    ///         "战斗开始一次性触发"。BaseLib/StS2 Relic 上真正"战斗开始"的钩子名
    ///         (可能是 <c>OnCombatStart</c> / <c>AtBattleStart</c> / <c>OnBattleStart</c>)
    ///         待用户确认后改回。
    ///     </para>
    /// </summary>
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (CombatStartFired[Owner.Creature]) return;
        CombatStartFired[Owner.Creature] = true;

        // 1) 获得 3 炁
        await yylCmd.GainQi(choiceContext, player, InitialQi, this, null);
        // 2) 把所有敌人视作奶龙 (NlPower 兼任 "tag + 击杀回血 3")
        await PowerCmd.Apply<NlPower>(choiceContext, combatState.HittableEnemies, 1m, Owner.Creature, null);
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<NlCan2>();
    }
}
