using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     引光: 1 费, 获得 1 → 2 层金光护体, 从弃牌堆选择 1 张牌放到抽牌堆顶端。
///     <para>
///         [rework 2026-09-19] 用户定调: 抢步/套步/引光三张廉价过牌定位重叠,
///         保留套步, 本卡改为"金光 + 回收"—— 类似原版头槌/全息的弃牌堆调度。
///         与套步 (抽牌但烧 1 张) 形成"过滤方向"上的两种选择。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinGuang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinGuang() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<GoldenAegis>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);

        // 从弃牌堆选 1 张 → 放到抽牌堆顶端 (下回合抽到的正是它)。
        // 必须用 CardSelectCmd.FromCombatPile, CommonActions 的选牌界面不会弹出 (见移穴的实现笔记)。
        var discard = PileType.Discard.GetPile(Owner);
        if (discard == null || discard.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, discard, Owner, prefs)).ToList();
        if (selected.Count == 0) return;

        await CardPileCmd.Add(selected[0], PileType.Draw, CardPilePosition.Top, this, true);
    }
}
