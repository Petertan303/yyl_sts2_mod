using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Extensions;
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
    ///     ★卡面/预览用的"自己身上是否拥有某力量"判定 —— 战斗内外都必须安全。
    ///     <para>
    ///         卡面显示的是 <c>DynamicVar._previewValue</c>, 而它会在<b>战斗外</b>
    ///         (卡牌图鉴 / 牌组浏览 / 奖励界面) 也被求值; 此时 <c>card.Owner</c> 为 null。
    ///         calc lambda 里若裸写 <c>card.Owner.Creature.HasPower&lt;T&gt;()</c> 会抛
    ///         NullReferenceException, 导致该 var 的 <c>_previewValue</c> 停在初始的 0
    ///         —— 表现就是「战斗外卡面显示 0 点伤害」(而不是回落到基础值)。
    ///         统一走这里做空值保护即可。参考安全写法: BorrowForce 的 <c>creature != null &amp;&amp; ...</c>。
    ///     </para>
    /// </summary>
    public static bool SelfHasPower<T>(CardModel card) where T : PowerModel
        => card.Owner?.Creature is { } self && self.HasPower<T>();

    /// <summary>
    ///     ★卡牌金光: 委托给 <see cref="GlowRegistry" /> 的"条件发光"判定。
    ///     <para>
    ///         原版(华丽收场 GrandFinale 等)就是覆写这个 virtual 属性,
    ///         由引擎 <c>NHandCardHolder.UpdateCard</c> 读取它并开关
    ///         <c>NCard.CardHighlight</c> 金光着色 —— 引擎本身就是金光权威,
    ///         会随战斗状态(获得/失去力量、炁增减、抽牌等)自动重算刷新。
    ///         之前本 mod 没覆写此属性(默认 false), 只在 <c>GlowRegistry.Refresh()</c>
    ///         里手动切 <c>CardHighlight</c>; 但原版 UpdateCard 随后读到 false 又会
    ///         AnimHide, 两者打架导致金光根本不显示。正确做法是让这个属性返回
    ///         真实条件, 把视觉交给引擎。
    ///     </para>
    /// </summary>
    protected override bool ShouldGlowGoldInternal => GlowRegistry.ShouldGlow(this);

    // 注: 之前的 SeedCalculatedBaseValue 已删除 —— CalculatedVar 在战斗外被 BaseLib 强制返回 0,
    // 播种 BaseValue/PreviewValue 既无效又会触发未解析(花括号)。"战斗外显示准确数字"改用普通
    // DynamicVar (WithDamage / WithVar) 解决: 其 IConvertible 直接返回 BaseValue, 战斗外也正确。
}
