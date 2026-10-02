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
///         <item><b>兑现</b>: 到达下一层(通过一场战斗)后, 此牌离开牌组,
///             并给予遗物「蛇花小姐」与卡牌「蛇花咬」。</item>
///     </list>
///     <para>
///         范式照抄 LexNinja2 的 <c>ISeeYou</c>: <see cref="CardType.Quest" /> +
///         <see cref="CardRarity.Quest" /> + 注册进 <see cref="TokenCardPool" />(只由事件发放)。
///     </para>
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SnakeFlower()
    : yylCardModel(-1, CardType.Quest, CardRarity.Quest, TargetType.Self)
{
    private int _floorsSeen;

    public override int MaxUpgradeLevel => 0;

    /// <summary>敌人每次被你打中后获得的格挡, 由补丁读取。</summary>
    public const decimal BlockGrantedToEnemy = 4m;

    /// <summary>需要"经过"几场战斗才兑现(1 = 到达下一层)。</summary>
    public const int FloorsToComplete = 1;

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
            //   ② 已经达到目标层数就再也不进这段;
            //   ③ 已经拿到过奖励 (遗物/卡任一存在) 就不再发 —— 三重保险。
            if (Pile is not { Type: PileType.Deck }) return;
            if (FloorsSeen >= FloorsToComplete) return;
            if (Owner.Relics.Any(r => r is Relics.SnakeFlowerMiss)) return;
            if (PileType.Deck.GetPile(Owner).Cards.OfType<Cards.Token.SnakeFlowerBite>().Any()) return;

            FloorsSeen++;
            if (FloorsSeen < FloorsToComplete) return;

            // 奖励一: 遗物「蛇花小姐」
            var relic = ModelDb.Relic<SnakeFlowerMiss>().ToMutable();
            await RelicCmd.Obtain(relic, Owner);

            // 奖励二: 卡牌「蛇花咬」
            var bite = Owner.RunState.CreateCard<SnakeFlowerBite>(Owner);
            await CardPileCmd.Add(bite, PileType.Deck);

            // 任务完成, 本牌离场
            await CardPileCmd.RemoveFromDeck(this);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 任务卡「蛇花」兑现失败: {ex}");
        }
    }
}
