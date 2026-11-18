using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     食炁: 2 费, 造成 18 → 26 点伤害。<b>消耗 3 点炁</b>; 若此击<b>击杀</b>敌人, <b>获得 6 点炁</b>。
///     <para>
///         机制照搬储君「决胜一击」(<c>KnockoutBlow</c>: 3 费 30 伤, 击杀后获 5 星),
///         把"星"换成伊林的「炁」, 并把费用压到 1 费 + 改为**消耗炁**——
///         形成"高伤爆发 → 斩杀 → 回炁"的循环, 与「大啖食粮」(击杀回血) 同一思路,
///         但走的是**资源回收**而非治疗。
///     </para>
///     <para>
///         ★耗炁用<b>「不足则少给效果」</b>的既定约定(与天火/散炁同款), 不做"改失血"。
///         炁不足 3 点时: 跳过耗炁, 伤害打折为"不耗炁时的基础值"。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShiQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShiQi() : this(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // ★2026-11-18 削弱(用户定调): 伤害 26→32 改为 18→26; 击杀返还 6→9 改为恒定 6
        //   (升级不再增加返还)。理由: 26 吃炁乘区(12炁时 44 伤) + 击杀净赚 3~6 炁,
        //   对一张 Uncommon 常规攻击过强。降基础伤压 boss 场景, 砍升级返还压小怪循环。
        WithDamage(18, 8);
        WithVar("QiCost", 3, 0);
        WithVar("QiRefund", 6, 0); // 升级不增加返还
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        var qi = Owner.Creature.GetPower<Powers.Qi>()?.Amount ?? 0;
        var cost = DynamicVars["QiCost"].IntValue;

        // ★不足则少给效果(既定约定): 炁不够就不耗炁, 但伤害打折(下限基础值的 60%)。
        var hasQi = qi >= cost;
        var damage = hasQi
            ? DynamicVars.Damage.BaseValue
            : DynamicVars.Damage.BaseValue * 6 / 10;

        if (hasQi)
        {
            var killed = (await DamageCmd.Attack(damage)
                    .FromCard(this, cardPlay)
                    .Targeting(target)
                    .WithHitFx("vfx/vfx_attack_blunt", null, "heavy_attack.mp3")
                    .Execute(choiceContext))
                .Results.SelectMany(r => r)
                .Any(r => r.WasTargetKilled);
            await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
            if (!killed) return;
            // 击杀 ⇒ 返还炁 (照 KnockoutBlow 的"击杀才给资源"结构)。
            await yylCmd.GainQi(choiceContext, Owner, DynamicVars["QiRefund"].IntValue, this, cardPlay.Card);

        }
    }
}