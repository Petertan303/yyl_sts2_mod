using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     神游: 0 费金卡, 消耗。失去<b>所有</b>炁, 获得等量能量并抽等量牌; 升级后数值 +1。
///     <para>
///         定位 = 伊林的「启动卡」(对标猎手肾上腺素 / 红裤衩祭品 / 储君大爆炸 / 骨头人精神过载)。
///         常态多配合初始遗物给的 3 炁使用 = 大号肾上腺素。
///     </para>
///     <para>
///         典故: 中医「离魂症」(dispersed soul) —— 《辨证录》「觉自己之身分而为两」、
///         《本草纲目》「身外有身」; 中医认为<b>魂附于气</b>, 故「炁散尽 = 神魂离体」。
///         道教「神游太虚」则取其飘然与感知敏锐之意。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShenYou(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShenYou() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        // 启动卡惯例: 消耗, 一次性爆发。
        WithKeywords(CardKeyword.Exhaust);
        // 不声明 QiLoss: 这张卡失去的是「全部」炁而非固定值, 写死 0 会在卡面显示「失去 0 炁」。
        // 代价完全由描述文案表达。
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 钩子绝不抛异常: 整段兜底, 失败只告警不冻结战斗。
        try
        {
            var qiPower = Owner?.Creature?.GetPower<Qi>();
            var qiAmount = (int)(qiPower?.Amount ?? 0);

            // 升级后 +1: 0 炁时也能换到 1 能量 + 1 抽, 不至于空放。
            var amount = qiAmount + (IsUpgraded ? 1 : 0);
            if (amount <= 0)
            {
                return;
            }

            // 先散尽炁 (走 ILoseQi 钩子链, 行炁等会因失炁触发), 再兑现能量与抽牌。
            if (qiAmount > 0)
            {
                await yylCmd.LoseQi(choiceContext, Owner, qiAmount, this, cardPlay.Card);
            }

            await PlayerCmd.GainEnergy(amount, Owner);
            await CardPileCmd.Draw(choiceContext, amount, Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 神游结算异常, 已安全跳过: {ex.Message}");
        }
    }
}
