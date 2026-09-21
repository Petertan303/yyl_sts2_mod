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
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

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
        // 2b) 队友也一并视为奶龙: 让"给奶龙加防/回血"的卡在联机里有正收益
        //     (单机没有队友, 行为与之前完全一致)。
        //     ⚠ Allies 是"己方全体", 包含玩家自己 —— 必须排除;
        //     把自己标记为奶龙是大瓶黄桃罐头 (NlCan2) 的专属效果。
        // ★统一走 yylNailong.ApplyMark: 联机时同一目标不会被两个玩家的罐头各挂一次,
        //   且已带心魔(大瓶)的目标不会被普通奶龙覆盖。
        await yylNailong.ApplyMark(choiceContext,
            combatState.HittableEnemies.Concat(
                combatState.Allies.Where(c => c.IsAlive && c != Owner.Creature)),
            Owner.Creature);
        // 3) 挂上手牌金光驱动 (隐藏 Power): 条件牌 (崩拳/金光联动/耗炁卡) 满足条件时发光。
        await PowerCmd.Apply<CardGlowDriver>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, null);
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<NlCan2>();
    }
}
