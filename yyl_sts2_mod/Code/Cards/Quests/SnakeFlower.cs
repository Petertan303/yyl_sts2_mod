using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Cards.Token;
using yyl_sts2_mod.Code.Relics;

namespace yyl_sts2_mod.Code.Cards.Quests;

/// <summary>
///     任务卡「蛇花」(事件「“蛇花”？」小心翼翼捧起的那一朵)。
///     <list type="bullet">
///         <item><b>负面</b>: 此牌在手牌中时, 你每次攻击敌人并造成伤害后, 该敌人获得 4 点格挡
///             (由 <c>SnakeFlowerPenaltyPatch</c> 补丁实现)。</item>
///         <item><b>兑现</b>: 经过 3 场战斗后, 此牌离开牌组,
///             并给予遗物「蛇花小姐」与卡牌「蛇花咬」。</item>
///     </list>
///     <para>
///         范式参考 LexNinja2 的 <c>ISeeYou</c>: <see cref="CardType.Quest" /> +
///         <see cref="CardRarity.Quest" /> + 注册进 <see cref="TokenCardPool" />(只由事件发放)。
///     </para>
///     <para>
///         ⚠ 与 LexNinja2 版的差异: 本卡<b>有构造函数并声明 <c>CardKeyword.Unplayable</c></b>。
///         任务牌理应不可打出(它的作用是"在牌组里待够场次", 不是被玩家主动使用),
///         否则玩家会白白浪费一个动作打出它而毫无收益。
///     </para>
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SnakeFlower(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /*  ★2026-10-02 补构造函数 + 声明不可打出:
         原写法是 `SnakeFlower() : yylCardModel(-1, CardType.Quest, ...)` —— 把参数
         直接写在基类调用里, 因此**没有任何 With* 声明的机会**, 卡片不被标记为不可打出,
         玩家可以真的把它打出去(而它的 OnPlay 是空的, 打出等于白费一个动作 + 任务进度不受影响)。
         改为标准 `this(...)` 转发后即可声明 WithKeywords(CardKeyword.Unplayable)。
         cost = -1: 与原版状态牌(感染/灼伤)一致 —— 能量球不渲染, 手中只显示划线图标。
         范式同 Cards/Token/LaShang.cs。*/
    public SnakeFlower() : this(-1, CardType.Quest, CardRarity.Quest, TargetType.Self)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    private int _floorsSeen;

    public override int MaxUpgradeLevel => 0;

    /// <summary>敌人每次被你打中后获得的格挡, 由补丁读取。</summary>
    public const decimal BlockGrantedToEnemy = 4m;

    /// <summary>
    ///     兑现时机: 拿到本牌后**经过 3 场战斗**(每次 <c>AfterCombatEnd</c> 计数 +1),
    ///     第 3 场战斗结束时移除本牌并发奖。
    ///     <para>
    ///         [2026-10-02] 曾改为 <c>ActFloor >= 3</c>(照 LexNinja2 ISeeYou 的层号门槛) ——
    ///         但层号是绝对值: 玩家在第 3 层**之后**拿到本牌时(事件可出现在任意层),
    ///         下一场战斗结束就立即兑现 ⇒ 用户实测"一场战斗就完成"。
    ///         [2026-10-20] 改回**战斗计数**并定档 3 场 (用户定调)。
    ///         <c>FloorsSeen</c> 字段名保留 (存档兼容), 语义已变为"已完成的战斗数"。
    ///     </para>
    /// </summary>
    public const int CompleteAfterBattles = 3;

    // BaseLib 要求 public 才能被扫描保存
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SavedProperty]
    public int FloorsSeen
    {
        get => _floorsSeen;
        set
        {
            AssertMutable();
            _floorsSeen = value;
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        try
        {
            if (Owner == null) return;

            // ★防重复兑现 (照 LexNinja2 ISeeYou 的写法):
            //   ① 只有此牌仍在牌组里才继续 —— 多人联机下该钩子会被多次调用;
            //   ② 已经发过奖就不再进这段;
            //   ③ 已经拿到过奖励 (遗物/卡任一存在) 就不再发 —— 三重保险。
            if (Pile is not { Type: PileType.Deck }) return;
            if (FloorsSeen >= CompleteAfterBattles) return;
            if (Owner.Relics.Any(r => r is Relics.SnakeFlowerMiss)) return;
            if (PileType.Deck.GetPile(Owner).Cards.OfType<Cards.Token.SnakeFlowerBite>().Any()) return;

            // ★ 兑现条件: 战斗计数。每场战斗结束 +1, 满 3 场兑现 ——
            //   与层号/幕完全解耦, 在哪一层拿到本牌都是完整的 3 场惩罚期。
            FloorsSeen++;
            if (FloorsSeen < CompleteAfterBattles)
            {
                MainFile.Logger.Info($"[yyl_sts2_mod] 任务卡「蛇花」: 第 {FloorsSeen}/{CompleteAfterBattles} 场战斗完成");
                return;
            }
            MainFile.Logger.Info("[yyl_sts2_mod] 任务卡「蛇花」: 3 场战斗完成, 兑现");

            // 奖励一: 遗物「蛇花小姐」
            var relic = ModelDb.Relic<SnakeFlowerMiss>().ToMutable();
            await RelicCmd.Obtain(relic, Owner);

            // 奖励二: 卡牌「蛇花咬」
            var bite = Owner.RunState.CreateCard<SnakeFlowerBite>(Owner);
            await CardPileCmd.Add(bite, PileType.Deck);

            // 任务完成, 本牌离场。
            // ★ showPreview=true (2026-10-20): 照原版藏宝图 SpoilsMap.OnQuestComplete 的写法 ——
            //   RemoveFromDeck 本身就播放移除动画, 第二参 true 额外弹出被移除卡牌的大图预览,
            //   即"任务完成"的仪式感特效。旧写法只传 card ⇒ showPreview 默认 false, 卡牌无感消失。
            await CardPileCmd.RemoveFromDeck(this, true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 任务卡「蛇花」兑现失败: {ex}");
        }
    }
}
