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
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ★铁律: OnPlay 绝不抛异常 —— 任何异常直冲引擎都会冻结整场战斗 (本卡历史卡死故障)。
        try
        {
            // Owner / Creature / CombatState 任一为空时静默退出 (预览、联机切换场景会出现)。
            if (Owner?.Creature is not { } self) return;
            var combatState = self.CombatState;
            if (combatState == null) return;

            // 只打非队友的奶龙: 联机时队友也可能被起始遗物标记为奶龙, 误伤队友不可接受。
            var teammates = combatState.GetTeammatesOf(self).ToHashSet();
            var nailongs = combatState.HittableEnemies
                .Where(e => e.IsAlive && yylNailong.IsNailong(e) && !teammates.Contains(e))
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
                // 目标可能在本循环的前几次结算中已经死亡, 及时跳过避免对尸体结算。
                if (!nailong.IsAlive) continue;

                // ★必须先用 FromCard 设置攻击者（反编译 AttackCommand 确认）：
                //   Execute() 开头就是
                //     if (Attacker == null) throw new InvalidOperationException("No attacker set.");
                //   本卡为绕开 CommonActions.CardAttack 而自行构建命令，此前漏掉这一步，
                //   于是每次 Execute 都抛异常 —— 异常直冲引擎 = 历史上的"打出后卡死"；
                //   被本方法的 try/catch 兜住后 = "能打出但不造成伤害、不回血"。
                var cmd = DamageCmd.Attack(damage).FromCard(this, cardPlay);
                cmd.WithValueProp(ValueProp.Move);
                cmd.Targeting(nailong);
                cmd.WithHitFx("vfx/vfx_bite"); // 打出特效: 原版撕咬 (照搬)
                var attack = await cmd.Execute(choiceContext);

                // attack / attack.Results 可能为空, 空安全后再累加。
                if (attack?.Results == null) continue;
                totalHealed += attack.Results
                    .Where(result => result != null)
                    .SelectMany(result => result)
                    .Where(result => result != null)
                    .Sum(result => result.UnblockedDamage);
            }

            if (totalHealed > 0)
            {
                await CreatureCmd.Heal(self, totalHealed);
                yylVfx.OnCreature(self, "vfx/vfx_cross_heal"); // 吸食回复特效
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 大啖食粮结算异常, 已安全跳过该次结算: {e.Message}");
        }
    }
}
