using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using yyl_sts2_mod.Code.Cards.Quests;

// 原版诅咒「愧疚」—— 用户明确要求用原版那张, 不再自建。
using VanillaGuilty = MegaCrit.Sts2.Core.Models.Cards.Guilty;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     事件「“蛇花”？」—— 路上遇见一朵娇嫩的蛇花。
///     <list type="bullet">
///         <item>小心翼翼地捧起 → 获得任务卡「蛇花」</item>
///         <item>踩碎 → 获得诅咒「愧疚」+ 一件随机罕见(Uncommon)遗物</item>
///     </list>
///     <para>
///         实现参考 LexNinja2 的 <c>TheSpectre</c>（同样是 ModEventTemplate + 两选项 + 任务卡）。
///         注册用 <c>[RegisterActEvent]</c>；该特性的扫描由 MainFile 里的
///         <c>ModTypeDiscoveryHub.RegisterModAssembly</c> 触发。
///     </para>
/// </summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Glory))]
[RegisterActEvent(typeof(Hive))]
public sealed class SnakeFlowerEvent : ModEventTemplate
{
    /// <summary>
    ///     本地化键前缀。★必须与 RitsuLib 注册出的 Id.Entry 完全一致 ——
    ///     实测日志: "Registered act event: SnakeFlowerEvent (id=YYL_STS2_MOD_EVENT_SNAKE_FLOWER_EVENT)",
    ///     即全大写+下划线, 不是 cards 那种 `MODID-NAME` 形式。
    /// </summary>
    private const string KeyPrefix = "YYL_STS2_MOD_EVENT_SNAKE_FLOWER_EVENT";

    public override string? CustomInitialPortraitPath =>
        "res://yyl_sts2_mod/images/events/snake_flower.png";

    /// <summary>供文案引用的卡名。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("QuestCard", ModelDb.Card<SnakeFlower>().Title),
        new StringVar("CurseCard", ModelDb.Card<VanillaGuilty>().Title)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(
            this,
            PickUpGently,
            $"{KeyPrefix}.pages.INITIAL.options.PICK_UP",
            HoverTipFactory.FromCardWithCardHoverTips<SnakeFlower>()
        ),
        new(
            this,
            CrushIt,
            $"{KeyPrefix}.pages.INITIAL.options.CRUSH",
            HoverTipFactory.FromCardWithCardHoverTips<VanillaGuilty>()
        )
    ];

    /// <summary>选项一: 捧起蛇花 —— 获得任务卡。</summary>
    private async Task PickUpGently()
    {
        try
        {
            var card = Owner!.RunState.CreateCard<SnakeFlower>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"SnakeFlowerEvent.PickUpGently 发卡失败。\n{ex}");
        }

        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.PICKED.description"));
    }

    /// <summary>选项二: 踩碎蛇花 —— 获得诅咒「愧疚」与一件随机罕见遗物。</summary>
    private async Task CrushIt()
    {
        try
        {
            var curse = Owner!.RunState.CreateCard<VanillaGuilty>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, PileType.Deck));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"SnakeFlowerEvent.CrushIt 发诅咒失败。\n{ex}");
        }

        try
        {
            var relic = RollUncommonRelic();
            if (relic != null)
            {
                await RelicCmd.Obtain(relic, Owner!);
            }
            else
            {
                MainFile.Logger.Warn("SnakeFlowerEvent: 罕见遗物池为空, 未发放遗物。");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"SnakeFlowerEvent.CrushIt 发遗物失败。\n{ex}");
        }

        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.CRUSHED.description"));
    }

    /// <summary>
    ///     从全游戏(含原版与本 mod)的罕见遗物池里随机取一件, 排除已经持有的。
    ///     用 <see cref="Rng" /> 保证同一局内结果可复现(回放/联机同步需要)。
    /// </summary>
    private RelicModel? RollUncommonRelic()
    {
        var player = Owner;
        if (player == null) return null;

        var owned = new HashSet<string>(
            player.Relics.Select(r => r.Id.Entry).Where(e => e != null)!);

        var candidates = ModelDb.AllRelics
            .Where(r => r.Rarity == RelicRarity.Uncommon)
            .Where(r => !owned.Contains(r.Id.Entry))
            .ToList();

        if (candidates.Count == 0) return null;

        // ModelDb 里的实例是"模板", 必须 ToMutable() 出一份可发放的副本。
        return new Rng(player, base.Id, 0uL).NextItem(candidates).ToMutable();
    }
}
