using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     祖炁形态（形态卡）: <b>3 费</b>，<b>升级不降费</b>（原版形态卡共同特征），
///     升级只把炁的线性系数从 9% 提到 12.5%。
///     <para>
///         效果: 炁的增伤<b>不再分段衰减</b> —— 每 1 点炁使造成的伤害提高
///         9%→12.5%（全程线性），取代默认的两段曲线（8 炁前 12.5%/点、之后 4%/点）。
///         对照: 8 炁时 默认 ×2.000 / 形态 ×1.720(9%) ~ ×2.000(12.5%)；
///         33 炁时 默认 ×3.000 / 形态 ×3.970 ~ ×5.125。
///     </para>
///     <para>
///         ★2026-11-15 用户定调: 最初的 8→12% 曾因「与卡面 × 炁量型卡二次增长」被削到
///         8→10%；新环境下认为原强度必要, 改为 <b>9% → 12.5%</b>。
///         ⚠ BaseLib 的 WithPower 升级增量只接受 int, 12.5 带半点写不出来:
///         用 (9, 0) 注册变量, 再在 OnUpgrade 里 UpgradeValueBy(3.5m)。
///         因此 gen_card_list.py 读到的升级增量是 0, 清单数值需单独维护
///         （用户本轮明确: 暂不修改 CARD_NUMBER_BASELINE 与统计脚本）。
///     </para>
///     <para>
///         设计备注: 原名「先天一炁」改作祖炁形态 —— 形态卡需符合「xx形态」命名与
///         3 费不降费的原版惯例；「先天一炁」这个名字留给遗物使用。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZuQiForm(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>每 1 点炁的伤害加成百分比（基础 / 升级后）。</summary>
    private const decimal BasePercent = 9m;
    private const decimal UpgradedPercent = 12.5m;

    public ZuQiForm() : this(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // 升级只增强系数 9% → 12.5%，**不降费**（对标原版形态卡）。
        // 升级增量写 0: 半点由 OnUpgrade 用 decimal 补, 见类注释。
        WithPower<Powers.ZuQiForm>((int)BasePercent, 0);
    }

    /// <summary>升级时把系数从 9% 提到 12.5%（WithPower 的 int 签名写不出 3.5 半点）。</summary>
    protected override void OnUpgrade() =>
        DynamicVars["ZuQiForm"].UpgradeValueBy(UpgradedPercent - BasePercent);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 直接读 IsUpgraded 而非 DynamicVars: 半点是小数, IntValue 会取整掉到 12,
        // 且不依赖 BaseLib 升级时是否刷新了动态变量。
        var percent = IsUpgraded ? UpgradedPercent : BasePercent;
        await PowerCmd.Apply<Powers.ZuQiForm>(
            choiceContext,
            new[] { Owner.Creature },
            percent,
            Owner.Creature,
            cardPlay.Card);
    }
}
