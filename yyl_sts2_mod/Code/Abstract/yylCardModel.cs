using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Extensions;
using yyl_sts2_mod.Code.Powers;
// using yyl_sts2_mod.Code.Core;
// using yyl_sts2_mod.Code.DynamicVars;
// using yyl_sts2_mod.Code.Extensions;
// using yyl_sts2_mod.Code.Keywords;
// using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Abstract;

public abstract class yylCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public sealed override string CustomPortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    ///     ★耗炁限制的统一结算 (2026-09-21): 优先失去 <c>QiLoss</c> 点炁;
    ///     <b>炁不足时改为失去 <c>HpLoss</c> 点生命</b> (代价换个形式, 卡的效果不变)。
    ///     想给强力卡加耗炁门槛时, 构造函数里声明
    ///     <c>WithPower&lt;Qi&gt;("QiLoss", n, up)</c> + <c>WithCalculatedDamage("HpLoss", m, ...)</c>
    ///     再在 OnPlay 开头调本方法即可。
    /// </summary>
    protected async Task PayQiOrHp(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var qiLoss = DynamicVars["QiLoss"].IntValue;
        if (qiLoss > 0 && Owner.Creature.GetPower<Qi>()?.Amount >= qiLoss)
        {
            await yylCmd.LoseQi(choiceContext, Owner, qiLoss, this, cardPlay.Card);
            return;
        }
        var hpLoss = DynamicVars["HpLoss"].IntValue;
        if (hpLoss > 0)
            // 走 LoseHp 而非伤害管线: 自伤不吃格挡、也不吃炁/姿态/遗物的增减伤。
            yylCmd.LoseHp(Owner.Creature, hpLoss);
    }
}
