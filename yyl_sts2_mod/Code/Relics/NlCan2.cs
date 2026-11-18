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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using Godot;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Powers;
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
        if (player != Owner) return;
        // ★ [2026-10-19 联机修复] 同 PeachCan: 去掉 TurnNumber == 1 守卫 (联机各玩家
        //   起始回合数会错开 ⇒ 整个初始化被跳过 ⇒ 开局 0 炁、耗炁卡全废), 改用幂等标记。
        if (Owner.Creature.GetPower<StarterSetup>() != null) return;

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

        // ★2026-11-18: 持有大瓶罐头 → 立绘变奶龙 (独立场景, 按格挡切换马步/立正姿势)。
        //   幂等: 控制器内部检测已安装则只刷新姿势; 视觉失败被 try/catch 吞掉, 不影响战斗。
        NailongVisuals.Apply(Owner.Creature);

        // ★2026-11-18: 恢复主题出牌音效 CardSfxDriver —— 但改成"按具体卡点题"版:
        //   只给清单里有意象的卡 (雷/剑/拳/蛇/火/禅/金光) 播 Kenney CC0 短音, 纯数值牌不响,
        //   不像旧版按卡类型"每张牌都响"那样吵。挂自己身上即可 (驱动内判 Owner.Player 联机隔离)。
        await PowerCmd.Apply<CardSfxDriver>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, null);
        yylAudio.PlaySfx("combat_start.mp3", 0.5f);

        // 打上幂等标记 (最后挂: 中途异常也不会漏发)
        await PowerCmd.Apply<StarterSetup>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, null);
    }

    /// <summary>
    ///     持有大瓶罐头时, 商店/休息处的角色立绘换成奶龙(立正)。
    ///     <para>
    ///         两个场景都<b>预置</b>了一个隐藏的 <c>NailongIdle</c> 节点 (yylMerchant.tscn /
    ///         yylRest.tscn); 这里只切显隐 —— 影子、蛇花小姐(商店)、篝火等原有节点全部保留,
    ///         与战斗场景的"隐藏原立绘、插入奶龙"是两套独立路径。
    ///     </para>
    ///     <para>
    ///         本钩子只在持有本遗物时触发, 天然完成"识别遗物"; UI 异步构建, 逐帧等待最多约 1.5 秒。
    ///         离开房间随场景销毁天然重置 (下次进入重新把 NailongIdle 设为可见)。
    ///     </para>
    /// </summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // 房间类型 → (立绘场景 group, 要隐藏的原立绘节点名, group挂在节点自身?)
        // ★休息处 group 必须挂在 NailongIdle 自身: yylRest.tscn 根节点会被 NodeFactory
        //   从裸 Node2D 转成 NRestSiteCharacter, 根节点 group 随之丢失 (实测空转90帧 → 进房延迟)。
        var (group, originalNode, groupOnSelf) = room switch
        {
            MerchantRoom => ("yyl_merchant_visual", "Yyl", false),
            RestSiteRoom => ("yyl_rest_visual", "Sprite2D", true),
            _ => (null, null, false)
        };
        if (group == null) { await Task.CompletedTask; return; }

        try
        {
            if (Engine.GetMainLoop() is not SceneTree tree) return;

            var shown = 0;
            for (var i = 0; i < 90; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                foreach (var node in tree.GetNodesInGroup(group))
                {
                    // 休息处: group 就在 NailongIdle 自身 (根节点被转换替换, 不能再从根节点找子节点)
                    var nailong = groupOnSelf ? node as Sprite2D : node.GetNodeOrNull("NailongIdle") as Sprite2D;
                    if (nailong == null) continue;
                    nailong.Visible = true;
                    // 隐藏原立绘: 休息处是 NailongIdle 的兄弟节点 "Sprite2D", 商店是根节点下的 "Yyl"
                    var parent = groupOnSelf ? node.GetParent() : node;
                    if (parent?.GetNodeOrNull(originalNode) is CanvasItem original)
                        original.Visible = false;
                    shown++;
                }
                if (shown > 0) break;
            }

            if (shown == 0)
                MainFile.Logger.Warn($"[yyl] 奶龙立绘: 未找到 {group} 的 NailongIdle 节点");
            else
                MainFile.Logger.Info($"[yyl] 奶龙立绘: {room.GetType().Name} 已切换 x{shown}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 奶龙立绘切换失败: {ex.Message}");
        }
        await Task.CompletedTask;
    }
}
