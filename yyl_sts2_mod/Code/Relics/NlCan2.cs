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
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     大瓶黄桃罐头 (先古): 合并了原「心魔」与「大瓶黄桃罐头」。
///     战斗第一回合抽牌前: 获得 5 点炁; 将玩家自身、队友与所有敌人视作心魔(奶龙);
///     并播放奶龙的声音。
///     <para>
///         [balance 2026-09-22] 移除了原本自带的「每点炁额外造成 20% 伤害」线性增伤:
///         它与炁自身的对数加成<b>相乘</b>, 40 炁时可达约 18 倍伤害, 过于失控。
///         本遗物现在<b>不再自行增伤</b>, 伤害完全交给炁的对数曲线处理
///         (见 <see cref="Powers.Qi" />), 其价值集中在「开局 5 点炁 + 全员心魔」上。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class NlCan2 : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    /// <summary>战斗开始时获得的炁。</summary>
    public const int InitialQi = 5;

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
        // ★统一走 yylNailong.ApplyMark(demon: true): 联机去重 (同一目标只挂一次),
        //   且目标已带普通奶龙时**先移除普通奶龙再用心魔覆盖** (心魔优先)。
        await yylNailong.ApplyMark(choiceContext, targets, Owner.Creature, demon: true);

        // 3) 挂上手牌金光驱动 (隐藏 Power): 与黄桃罐头一致, 让条件牌在战斗中随状态发光。
        //    ⚠ 不能省略 —— 否则升级到 NlCan2 后金光不会被战斗事件钩子刷新 (回归)。
        await PowerCmd.Apply<CardGlowDriver>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, null);

        // 心魔 buff 被赋予实体时: 发出奶龙的声音
        yylAudio.PlaySfx(yylAudio.Sfx("nailong/nailong_voice.ogg"), 0.9f);
    }
}
