using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     大啖食粮: 2 费, 对每只非队友的奶龙各造成一次 4 → 6 点单体伤害,
///     并回复等同<b>对奶龙造成伤害</b>的生命。
///     <para>
///         [2026-09-22 重做] 段数 = 奶龙敌人总数 (每只各挨一下), 且只从奶龙身上吸血 ——
///         原先走 CommonActions.CardAttack 会退化成"打全体 × 循环次数"。详见 OnPlay 注释。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Devour(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Devour() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 只打非队友的奶龙: 联机时队友也可能被起始遗物标记为奶龙, 误伤队友不可接受。
        var teammates = combatState.GetTeammatesOf(Owner.Creature).ToHashSet();
        var nailongs = combatState.HittableEnemies
            .Where(e => yylNailong.IsNailong(e) && !teammates.Contains(e))
            .ToList();
        if (nailongs.Count == 0) return;

        var damage = DynamicVars.Damage.IntValue;
        var totalHealed = 0m;

        /*  ★2026-09-22 重做: 改为「对每只奶龙各打一次**单体**伤害」, 且只从奶龙身上吸血。
            之前用的是 CommonActions.CardAttack —— 它对 TargetType.AllEnemies 的卡会
            **完全忽略传入的 target 参数**、改成 TargetingAllOpponents 打全体
            (反编译 BaseLib 确认)。于是 N 只奶龙 → 每个敌人挨 N 次、还会误伤非奶龙,
            回血又按全体 UnblockedDamage 求和, 一并虚高。
            这里绕开它、自己建 AttackCommand 并显式 Targeting 到当前这一只奶龙。
            写法与 CommonActions 内部一致 (先建命令 → Targeting → WithHitFx → Execute,
            这几个方法都是原地修改接收者)。 */
        foreach (var nailong in nailongs)
        {
            var cmd = DamageCmd.Attack(damage).WithValueProp(ValueProp.Move);
            cmd.Targeting(nailong);
            cmd.WithHitFx("vfx/vfx_bite"); // 打出特效: 原版撕咬 (照搬)
            var attack = await cmd.Execute(choiceContext);
            totalHealed += attack.Results.SelectMany(result => result).Sum(result => result.UnblockedDamage);
        }

        if (totalHealed > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, totalHealed);
            yylVfx.OnCreature(Owner.Creature, "vfx/vfx_cross_heal"); // 吸食回复特效
        }
    }
}
