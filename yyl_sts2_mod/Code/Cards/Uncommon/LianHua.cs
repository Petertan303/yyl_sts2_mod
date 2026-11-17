using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     炼化: 1 → 0 费, 消耗手中所有的牌, 每消耗一张获得 2 层金光护体。**本身不消耗**。
///     <para>
///         [new 2026-09-19] 用户定调的"烧牌/牌堆控制"补位之一: 把整手牌炼成金光,
///         是金光体系的一张爆发型产出来源 (通常 3~5 层, 回合末打空手牌时最赚)。
///         与金光壁 (无代价厚产) 区分: 炼化烧掉的是手牌资源。
///     </para>
///     <para>
///         ★ [2026-10-20] 本身不再消耗 (用户定调, 最初版本即如此)。
///         当年加消耗是因金光每层 -2 时滚雪球过强; 金光砍至 -1 后重新测算:
///         20 张卡组 / 每回合默认抽 5 ⇒ 炼化约每 4 回合一次、每次烧 4 张(手牌 5 - 本卡),
///         即 **烧牌 ≈ 1 张/回合、金光 ≈ 2 层/回合** 的慢性洁卡引擎;
///         带运转(额外抽牌)时按比例加速, 这是无限构筑(生旺死衰 0 费化 + 逗龙/风后互抽)的基建。
///         注意: 烧手牌会把抽进手的循环件一并烧掉 —— 先打出循环件(进弃牌堆)再打炼化, 属本卡时序技巧。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class LianHua(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public LianHua() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        // ★ [2026-10-20 用户定调] 本身不再消耗 —— 最初版本即不消耗, 当时金光为每层 -2
        //   而显得过强遂加消耗; 金光已砍至每层 -1, 前提改变。重算: 20 张卡组 / 每回合抽 5,
        //   炼化约每 4 回合抽到一次、每次烧手牌 4 张(5-1) ⇒ 烧牌 ≈ 1 张/回合、金光 ≈ 2 层/回合,
        //   慢性洁卡引擎而非滚雪球。无限构筑(生旺死衰 0 费化 + 逗龙互抽)的洁卡基建。
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ★排查日志 (2026-09-29: 用户报告打出时卡死, 但所有日志里都没有本卡的打出行,
        //   无法定位卡死点) —— 开始/每张消耗/结束 全程打点, 复现后按日志定位, 定位完可删。
        MainFile.Logger.Info("[炼化] OnPlay 开始");
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null)
        {
            MainFile.Logger.Info("[炼化] 手牌为空, 结束");
            return;
        }

        // 迭代副本: 消耗会改变牌堆内容。
        var cards = hand.Cards.ToList();
        MainFile.Logger.Info($"[炼化] 待消耗 {cards.Count} 张: {string.Join(", ", cards.Select(c => c.Id.Entry))}");
        var n = 0;
        foreach (var card in cards)
        {
            n++;
            MainFile.Logger.Info($"[炼化] 消耗第 {n}/{cards.Count} 张: {card.Id.Entry}");
            await CardCmd.Exhaust(choiceContext, card);
        }

        if (cards.Count > 0)
        {
            MainFile.Logger.Info($"[炼化] 施加金光护体 {cards.Count * 2} 层");
            await PowerCmd.Apply<GoldenAegis>(choiceContext, new[] { Owner.Creature },
                cards.Count * 2, Owner.Creature, cardPlay.Card);
        }

        MainFile.Logger.Info("[炼化] OnPlay 结束");
    }
}
