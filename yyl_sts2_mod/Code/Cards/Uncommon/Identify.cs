using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     指认: **0 费技能**, 选择一个目标 ——
///     <list type="bullet">
///         <item>目标<b>已带奶龙标记</b>(普通奶龙或心魔) → 获得 1 → 2 点能量。</item>
///         <item>目标<b>未带标记</b> → 按你持有的初始遗物, 为其施加对应的奶龙标记
///             (黄桃罐头 → 奶龙; 大瓶黄桃罐头 → 心魔)。</item>
///     </list>
///     <para>
///         [2026-10-02 overhaul] 原为 1 费攻击白卡「造成 6→9 伤害, 若目标是奶龙获得 1 能量」。
///         降费为 0 并升为蓝卡, 效果从「纯输出」彻底转向「奶龙标记的补充来源」。
///     </para>
///     <para>
///         ★<b>设计动机: 补上奶龙标记唯一的「后续施加」缺口</b>。
///         黄桃罐头与大瓶黄桃罐头的挂标记逻辑都挂在 <c>BeforeHandDraw</c> 且带
///         <c>TurnNumber: 1</c> 守卫 —— <b>只在战斗第一回合给当时在场的实体挂标记</b>。
///         因此: 第二波入场敌人、联机中各玩家战斗起始回合数错开的情形下,
///         场上会有<b>永远拿不到标记</b>的敌人, 而奶龙体系(驭龙/养龙/投喂/大啖食粮等 8 张卡)
///         对它们全部失效。本卡是那张「补标记」的牌, 与两个遗物<b>同源</b>
///         (统一走 <c>yylNailong.ApplyMark</c>, 联机去重 + 心魔优先覆盖均自动继承)。
///     </para>
///     <para>
///         ⚠ <b>刻意不主动给全场上标记</b>: 若对所有敌人无条件补标记, 奶龙将从
///         「需要经营的优势」退化为「默认全场状态」, 养龙的击杀叠层(上限 20 层)会被白送。
///         所以判定<b>只看被选中的那一个目标</b>: 它带标记就产能量, 不带才补——
///         每一次补标记都是玩家主动且有代价(0 费 + 消耗一个行动)的选择。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Identify(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Identify() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithEnergy(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 已带任意奶龙标记(普通奶龙 or 心魔) → 转化为能量。
        // 用 IsNailongMarked 而非 IsNailong: 后者要求 IsAlive, 而判标记不需要存活条件,
        // 且它同时认 NailongMark 与 InnerDemon, 与两个初始遗物的判定完全一致。
        if (yylNailong.IsNailongMarked(target))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            return;
        }

        // 未带标记 → 按当前持有的初始遗物施加对应标记:
        //   持有大瓶黄桃罐头 → 心魔(InnerDemon; ApplyMark 内部心魔优先, 会先移除普通奶龙);
        //   否则(黄桃罐头) → 普通奶龙(NailongMark)。
        // ApplyMark 已内部处理联机去重(同一目标只挂一次)与已带同类标记则跳过。
        var demon = Owner.Relics.Any(r => r is Relics.NlCan2);
        await yylNailong.ApplyMark(choiceContext, new[] { target }, Owner.Creature, demon);
    }
}