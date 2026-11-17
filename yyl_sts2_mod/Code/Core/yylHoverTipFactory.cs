using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.Core;

public class yylHoverTipFactory
{
    public static IHoverTip FromStance<T>() where T : yylStanceModel
    {
        return FromStance(yylModelDb.yylStance<T>());
    }

    public static IHoverTip FromStance(yylStanceModel model)
    {
        return model.DumbHoverTip;
    }

    /// <summary>
    ///     「姿态」通用解释窗 (★2026-11-16 用户定调, 照观者 mod 卡牌预览的悬浮解释做法):
    ///     文案在 powers 表 <c>YYL_STS2_MOD-STANCE_TIP.title/.description</c>。
    ///     无图标 (HoverTip 的 icon 参数传 null)。
    /// </summary>
    public static IHoverTip StanceIntro()
    {
        var title = new LocString("powers", $"{MainFile.ModId.ToUpperInvariant()}-STANCE_TIP.title");
        var description = new LocString("powers", $"{MainFile.ModId.ToUpperInvariant()}-STANCE_TIP.description");
        return new HoverTip(title, description.GetFormattedText(), null);
    }

    /// <summary>
    ///     逆生姿态链解释窗：<b>姿态通用窗 + 自身及之后各重的姿态解释</b>。
    ///     <c>minLevel</c> = 打出这张卡进入的姿态等级 (一重=1 / 二重=2 / 三重=3):
    ///     一重卡显示 姿态、逆生一重、逆生二重、逆生三重；
    ///     二重卡显示 姿态、逆生二重、逆生三重；三重卡显示 姿态、逆生三重。
    ///     各重解释 = <see cref="yylStanceModel.DumbHoverTip" /> (规范实例可直接取,
    ///     <c>AddDumbVariablesToDescription</c> 对非 mutable 有回退分支)。
    ///     ⚠ 与 <see cref="FromCardChain{T}" /> 的卡面预览窗并存 —— 用独立的
    ///     <c>WithTips</c> 调用追加 (BaseLib 内部是列表 Add, 多次调用叠加)。
    /// </summary>
    public static IEnumerable<IHoverTip> RebirthStanceChain(int minLevel)
    {
        yield return StanceIntro();
        if (minLevel <= 1) yield return FromStance<RebirthStanceOne>();
        if (minLevel <= 2) yield return FromStance<RebirthStanceTwo>();
        if (minLevel <= 3) yield return FromStance<RebirthStanceThree>();
    }

    /// <summary>
    ///     卡牌预览 + 该卡自身的全部 hovertip，<b>整体都按同一个升级状态渲染</b>。
    ///     <para>
    ///         ★ 为什么不直接用引擎的 <c>HoverTipFactory.FromCardWithCardHoverTips&lt;T&gt;(upgrade)</c>：
    ///         反编译 sts2.dll 的真实实现（2026-10-07 核实）——
    ///         <code>
    ///         return new IHoverTip[1] { FromCard&lt;T&gt;(upgrade) }
    ///                .Concat(ModelDb.Card&lt;T&gt;().HoverTips);
    ///         //            ↑ 克隆+UpgradeInternal，正确
    ///         //                      ↑ ★规范单例（canonical），永远未升级
    ///         </code>
    ///         它的<b>第一项</b>（卡片预览）会克隆并升级，所以「逆生一重」悬浮里的「逆生二重」看着是对的；
    ///         但<b>尾部接的是规范单例的 HoverTips</b>，而我们的 <c>ExtraHoverTips</c> 是<b>递归</b>的
    ///         （每张牌用 <c>WithTips</c> 声明下一张牌的预览），规范单例的 <c>IsUpgraded</c>恒为 false
    ///         ⇒ <b>嵌套的那一层必然退回未升级版</b>。这就是「二重预览正常、三重仍是未升级」的根因。
    ///         <para/>
    ///         本方法把 <b>克隆升级后的那份实例</b> 同时用于预览和 HoverTips ——
    ///         <c>CardModel.ExtraHoverTips</c> 是 <c>_multiHoverTips.SelectMany(mt =&gt; mt(this))</c>，
    ///         <c>this</c> 换成升级克隆后，递归传下去的 <c>card.IsUpgraded</c> 就是 true，
    ///         整条链（逆生一重 → 二重 → 三重）自上而下全部一致。
    ///     </para>
    /// </summary>
    public static IEnumerable<IHoverTip> FromCardChain<T>(bool upgraded) where T : CardModel
    {
        var canonical = ModelDb.Card<T>();

        // 与引擎 FromCard(card, upgrade) 同款流程: 先克隆再升级。
        // ⚠ 不能写成 `ModelDb.Card<T>().ToMutable(1)` 之类带初值的重载 (同ToMutable 坑，见记忆)。
        // ★ 显式声明为 CardModel: 若用 `var shown = canonical` 会把类型推成 T,
        //   后面赋克隆体 (CardModel) 就需要强转, 而 T 是泛型参数不能直接强转。
        CardModel shown = canonical;
        if (upgraded)
        {
            shown = (CardModel)canonical.MutableClone();
            shown.UpgradeInternal();
            shown.FinalizeUpgradeInternal();
        }

        // CardHoverTip 构造时若发现不可变会自行 ToMutable()，这里传升级克隆即可原样渲染。
        return new[] { new CardHoverTip(shown) }.Concat(shown.HoverTips);
    }
}