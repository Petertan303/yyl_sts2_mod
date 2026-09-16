using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using yyl_sts2_mod.Code.Enchantments;

namespace yyl_sts2_mod.Code.Rewards;

/// <summary>
///     开脉的战后奖励: 点击后从牌组里选 N 张牌永久附魔「炁脉」。
///     <para>
///         完全照原版死灵法师「禁忌魔典」(<c>CardRemovalReward</c>) 的模式实现 ——
///         战后要弹选牌界面, 不能在内联 <c>await</c>, 否则战斗结算序列和选牌界面互相等待会死锁;
///         正确做法是把选择本身做成一个 Reward, 由战利品界面驱动。
///     </para>
/// </summary>
public class QiMeridianEnchantReward : Reward
{
    private readonly int _count;

    public QiMeridianEnchantReward(Player player, int count) : base(player)
    {
        _count = count;
    }

    /// <summary>与 CardRemovalReward 一致 (该类内部就是返回枚举值 4)。</summary>
    protected override RewardType RewardType => (RewardType)4;

    public override LocString Description =>
        new LocString("cards", "YYL_STS2_MOD-MERIDIAN_OPENING.rewardDescription");

    protected override string IconPath => "res://yyl_sts2_mod/images/powers/qi.png";

    public override int RewardsSetIndex => 0;

    public override bool IsPopulated => true;

    public override void Populate()
    {
    }

    public override void MarkContentAsSeen()
    {
    }

    protected override async Task<bool> OnSelect()
    {
        var enchantment = ModelDb.Enchantment<QiMeridian>();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, _count);
        var selected = await CardSelectCmd.FromDeckForEnchantment(Player, enchantment, _count, prefs);
        foreach (var card in selected)
        {
            // CardCmd.Enchant 是同步的: 施加附魔并返回附魔实例本身。
            CardCmd.Enchant(enchantment, card, 1m);
        }

        return true;
    }
}
