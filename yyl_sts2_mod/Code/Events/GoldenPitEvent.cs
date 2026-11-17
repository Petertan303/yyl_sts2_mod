using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using yyl_sts2_mod.Code.Cards.Basic;
using yyl_sts2_mod.Code.Enchantments;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     事件「坑边的金光」—— 崖底的金光里，有个穿得奇奇怪怪的女孩在填土，坑沿上伸出一只手。
///     <list type="bullet">
///         <item>默默离开 → 升级牌组里的「金光咒」；若它已升级，改为附魔「炁脉」。</item>
///         <item>上前询问 → 第一选项在「挨一铲子」/「塞钱」里随机出现一种（引擎事件级 Rng），
///               第二选项「以金光咒相抵」仅在牌组里有已升级的金光咒时可选；
///               抵挡不付出代价。三条路最后都给 2 份标准卡牌奖励。</item>
///     </list>
///     <para>
///         铲子伤害按进阶分档：进阶 5（<see cref="AscensionLevel.AscendersBane"/>）以下 4 点，
///         及以上 6 点；用 <c>AscensionHelper.GetValueIfAscension</c> 在事件开始时重算，
///         并据 <c>HasAscension</c> 选 SHOVEL / SHOVEL_HIGH 两套本地化键。
///     </para>
///     <para>
///         只有当前生命 &gt; 20 且金币 &gt;= 100 时才会出现（见 <see cref="IsAllowed"/>）。
///     </para>
///     <para>
///         注册全部三幕（Overgrowth / Underdocks / Glory）＝ 任意层都可能遇到。
///         本地化键前缀沿用 <see cref="SnakeFlowerEvent"/> 已实测的
///         <c>YYL_STS2_MOD_EVENT_&lt;类名全大写下划线&gt;</c>，全部手写完整键，
///         不依赖 <c>PageDescription</c> / <c>ModOptionKey</c> 的前缀规则。
///     </para>
///     <para>
///         「不可选」用引擎既有机制：<c>EventOption</c> 构造里
///         <c>IsLocked = (onChosen == null)</c>（已核对 sts2.dll 的 IL：ldnull; ceq; stfld），
///         所以条件不满足时传空回调即可让按钮变灰，而不是把选项从列表里删掉。
///     </para>
/// </summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Glory))]
public sealed class GoldenPitEvent : ModEventTemplate
{
    private const string KeyPrefix = "YYL_STS2_MOD_EVENT_GOLDEN_PIT_EVENT";

    /// <summary>三条询问路线共给的卡牌奖励份数；每份是标准的 3 选 1 卡牌奖励。</summary>
    private const int CardRewardCount = 2;
    private const int CardsPerReward = 3;

    /// <summary>铲子伤害：进阶 5 以下 4 点，进阶 5 及以上 6 点。</summary>
    private const AscensionLevel HighAscension = AscensionLevel.AscendersBane;
    private const decimal LowAscensionDamage = 4m;
    private const decimal HighAscensionDamage = 6m;

    /// <summary>
    ///     准入条件：当前生命严格大于 20、金币大于等于 100。
    ///     生成下一个事件房间时 <c>RoomSet.EnsureNextEventIsValid</c> 会调用
    ///     <c>EventModel.IsAllowed(IRunState)</c>，返回 false 就抽不到本事件。
    ///     血量用 <c>CurrentHp</c>（不是最大生命）；联机按原版
    ///     <c>StoneOfAllTime</c> / <c>DenseVegetation</c> 的口径要求所有玩家都满足。
    /// </summary>
    private const int HpExclusiveLowerBound = 20;
    private const int MinGold = 100;

    public override bool IsAllowed(IRunState runState)
    {
        try
        {
            return runState.Players.Count > 0 &&
                   runState.Players.All(p =>
                       p.Creature.CurrentHp > HpExclusiveLowerBound && p.Gold >= MinGold);
        }
        catch (Exception ex)
        {
            // 这个钩子在地图生成阶段跑，抛异常会卡住房间生成；出错时保守地不让事件出现。
            MainFile.Logger.Error($"GoldenPitEvent.IsAllowed 检查准入条件失败。\n{ex}");
            return false;
        }
    }

    public override string? CustomInitialPortraitPath =>
        "res://yyl_sts2_mod/images/events/golden_pit.png";

    /// <summary>
    ///     结算用的数值集中在这里。<c>Fate</c> 存随机结果（0＝铲子，1＝塞钱），会随事件存档走。
    ///     <para>
    ///         ★ 文案里没有用 <c>{ShovelDamage}</c> 之类的占位符：实测引擎的
    ///         <c>GetOptionTitle/GetOptionDescription</c> 只是 <c>LocString.GetIfExists</c>、
    ///         <c>L10NLookup</c> 只是 <c>new LocString</c>，都不会自动给事件的 LocString 挂
    ///         DynamicVars，占位符会原样显示。所以 events.json 里把 4/6 点生命 / 60 金币 /
    ///         金光咒写成固定文字，铲子按进阶选两套键；改数值要同步改 zhs+eng 的 events.json。
    ///     </para>
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("GoldenCharm", ModelDb.Card<GoldenCharm>().Title),
        new DamageVar("ShovelDamage", LowAscensionDamage, ValueProp.Unblockable | ValueProp.Unpowered),
        new IntVar("GoldCost", 60m),
        new IntVar("Fate", 0m)
    ];

    private bool PaysInsteadOfShovel => DynamicVars["Fate"].BaseValue > 0m;

    /// <summary>高进阶另用一套键；事件 LocString 不会自动挂 DynamicVars，只能按 key 切换。</summary>
    private string ShovelOptionKey
    {
        get
        {
            var high = false;
            try
            {
                high = AscensionHelper.HasAscension(HighAscension);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"GoldenPitEvent.ShovelOptionKey 读取进阶等级失败。\n{ex}");
            }

            return $"{KeyPrefix}.pages.ASK.options.{(high ? "SHOVEL_HIGH" : "SHOVEL")}";
        }
    }

    /// <summary>
    ///     用事件自带的 <see cref="EventModel.Rng"/> 掷随机选项，不用 Random.Shared / new Random()
    ///     （联机 lockstep 会 desync）。Drink 事件里的 <c>Rng.NextInt(101)</c> 就是同一套做法。
    /// </summary>
    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        try
        {
            // canonical 模型初始化时还没有当前 run，进阶数值必须在事件开始时重算。
            DynamicVars["ShovelDamage"].BaseValue = AscensionHelper.GetValueIfAscension(
                HighAscension, HighAscensionDamage, LowAscensionDamage);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"GoldenPitEvent.BeforeEventStarted 读取进阶等级失败。\n{ex}");
        }

        if (!isPreFinished)
            DynamicVars["Fate"].BaseValue = Rng.NextBool() ? 1m : 0m;
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(
            this,
            LeaveQuietly,
            $"{KeyPrefix}.pages.INITIAL.options.LEAVE",
            HoverTipFactory.FromCardWithCardHoverTips<GoldenCharm>()
        ),
        new(this, StepForward, $"{KeyPrefix}.pages.INITIAL.options.ASK")
    ];

    /// <summary>选项一：不作声退回去 —— 升级金光咒；已升级则改附魔炁脉。</summary>
    private Task LeaveQuietly()
    {
        var page = "LEFT_UPGRADED";
        try
        {
            var card = FindGoldenCharm();
            if (card == null)
            {
                page = "NO_CHARM";
            }
            else if (card.IsUpgraded)
            {
                CardCmd.Enchant<QiMeridian>(card, 1m);
                page = "LEFT_ENCHANTED";
            }
            else
            {
                CardCmd.Upgrade(card, CardPreviewStyle.EventLayout);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"GoldenPitEvent.LeaveQuietly 处理金光咒失败。\n{ex}");
            page = "NO_CHARM";
        }

        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.{page}.description"));
        return Task.CompletedTask;
    }

    /// <summary>选项二：上前询问 —— 切到第二页，随机决定第一选项，并按条件锁定抵挡选项。</summary>
    private Task StepForward()
    {
        var first = PaysInsteadOfShovel
            ? new EventOption(this, PayHer, $"{KeyPrefix}.pages.ASK.options.PAY")
            : new EventOption(this, TakeTheShovel, ShovelOptionKey);

        // 回调为 null ⇒ EventOption.IsLocked = true ⇒ 按钮灰掉，仍能看到为什么不能选。
        Func<Task>? blockHandler = HasUpgradedGoldenCharm() ? BlockWithCharm : null;
        var second = new EventOption(
            this,
            blockHandler!,
            $"{KeyPrefix}.pages.ASK.options.BLOCK",
            HoverTipFactory.FromCardWithCardHoverTips<GoldenCharm>()
        );

        SetEventState(L10NLookup($"{KeyPrefix}.pages.ASK.description"), [first, second]);
        return Task.CompletedTask;
    }

    /// <summary>随机分支之一：挨一铲子。</summary>
    private async Task TakeTheShovel()
    {
        try
        {
            var damage = (DamageVar)DynamicVars["ShovelDamage"];
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner!.Creature,
                damage,
                null!,
                null!
            );
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"GoldenPitEvent.TakeTheShovel 结算伤害失败。\n{ex}");
        }

        await GiveCardRewards();
        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.SHOVELED.description"));
    }

    /// <summary>随机分支之二：给她塞钱。金币不足时只扣到 0，避免负数，也不锁死这个选项。</summary>
    private async Task PayHer()
    {
        try
        {
            var amount = Math.Min(DynamicVars["GoldCost"].BaseValue, Owner!.Gold);
            if (amount > 0m)
                await PlayerCmd.LoseGold(amount, Owner, GoldLossType.Spent);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"GoldenPitEvent.PayHer 扣金币失败。\n{ex}");
        }

        await GiveCardRewards();
        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.PAID.description"));
    }

    /// <summary>条件分支：已升级的金光咒挡下这一下，什么也不损失。</summary>
    private async Task BlockWithCharm()
    {
        await GiveCardRewards();
        SetEventFinished(L10NLookup($"{KeyPrefix}.pages.BLOCKED.description"));
    }

    /// <summary>
    ///     一次弹出标准卡牌奖励界面，里面放 <see cref="CardRewardCount"/> 个卡牌奖励图标，每个是 3 选 1。
    ///     走 <c>RewardsCmd.OfferCustom</c>（内部 = <c>new RewardsSet(player).WithCustomRewards(rewards).Offer()</c>），
    ///     因此会经过卡牌奖励的 hook（Molten Egg / Prayer Wheel 一类）与重掷逻辑。
    ///     每个 CardReward 用独立的 CardCreationOptions，避免共享实例被 RerollOptions 改写。
    /// </summary>
    private async Task GiveCardRewards()
    {
        try
        {
            var player = Owner!;
            var rewards = new List<Reward>();
            for (var i = 0; i < CardRewardCount; i++)
            {
                var options = CardCreationOptions.ForNonCombatWithDefaultOdds(
                    new[] { player.Character.CardPool }, null!);
                // 第 4 个参数传 null 时，构造内部会回退到 RunManager.Instance.PlayerChoiceSynchronizer。
                rewards.Add(new CardReward(options, CardsPerReward, player, null!));
            }

            await RewardsCmd.OfferCustom(player, rewards);
        }
        catch (Exception ex)
        {
            // 奖励界面失败也必须让事件能结束，否则房间会一直卡在等待状态。
            MainFile.Logger.Error($"GoldenPitEvent.GiveCardRewards 发卡牌奖励失败。\n{ex}");
        }
    }

    private CardModel? FindGoldenCharm() =>
        Owner?.Deck.Cards.FirstOrDefault(c => c is GoldenCharm);

    private bool HasUpgradedGoldenCharm() =>
        Owner?.Deck.Cards.Any(c => c is GoldenCharm && c.IsUpgraded) ?? false;
}
