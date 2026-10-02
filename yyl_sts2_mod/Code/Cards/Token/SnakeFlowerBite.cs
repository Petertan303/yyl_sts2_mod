using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Monsters;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     「蛇花咬」: 1 费, 蛇花小姐对一名敌人造成 7(→8) 点伤害 3 次, 并给予 2 层虚弱。
///     <para>
///         ★伤害来源是<b>蛇花小姐</b>而不是玩家 —— 走 <c>AttackCommand.FromMonster</c>,
///         因此这段伤害<b>不经过炁增伤</b>(炁只放大玩家自己的输出)。
///         场上没有存活的蛇花小姐时此牌打不出伤害 (只记日志)。
///     </para>
///     <para>只由任务卡「蛇花」兑现时给予, 注册进 <see cref="TokenCardPool" />(不进奖励池)。</para>
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SnakeFlowerBite(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public SnakeFlowerBite() : this(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        // ★专属伤害变量 (照原版 POKE 的 OstyDamageVar):
        //   卡面预览以 dealer=null 走钩子, 不吃玩家自己的增幅器 ——
        //   之前用普通 Damage 变量时, 卡面显示的是炁增幅后的数字 (实测 2026-09-29)。
        //   升级 +1 用 OnUpgrade 里的 UpgradeValueBy, 不用 WithDamage。
        WithVar(new PetDamageVar(BaseDamage, ValueProp.Move));
    }

    /// <summary>每段伤害 (升级前)。</summary>
    public const int BaseDamage = 7;

    /// <summary>升级后每段伤害的加成。</summary>
    public const int UpgradeDamageBonus = 1;

    /// <summary>命中段数。</summary>
    public const int HitCount = 3;

    /// <summary>虚弱层数 (升级不变)。</summary>
    public const decimal WeakAmount = 2m;

    protected override void OnUpgrade() =>
        DynamicVars["PetDamage"].UpgradeValueBy(UpgradeDamageBonus);

    /// <summary>
    ///     ★场上没有存活的蛇花小姐时<b>不可打出</b> (与原版"需要奥斯提存活"的卡同款表现)。
    ///     实测教训: 之前只靠 OnPlay 里 return, 卡面仍可点、打出后什么都不发生。
    /// </summary>
    protected override bool IsPlayable =>
        FindPet() != null;

    /// <summary>查找本场召唤且存活的蛇花小姐 (与原版 OstyCmd 同款查找)。</summary>
    private Creature? FindPet() =>
        Owner.Creature.CombatState?.Allies?.FirstOrDefault(
            c => c.Monster is SnakeFlowerMissPet && c.PetOwner == Owner && c.IsAlive);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        try
        {
            var target = cardPlay.Target;
            if (target == null) return;

            var pet = FindPet();
            if (pet?.Monster == null)
            {
                MainFile.Logger.Warn("[yyl_sts2_mod] 蛇花咬: 场上没有存活的蛇花小姐, 无法出手。");
                return;
            }

            var damage = DynamicVars["PetDamage"].BaseValue;

            for (var i = 0; i < HitCount; i++)
            {
                // 先定单体外攻 (必须在 From* 之前 —— FromMonster 会强制 TargetingAllOpponents),
                // 再用 FromCard 挂上 ModelSource/CardPlay, 最后把攻击者替换成蛇花小姐:
                // 伤害归属宠物 → 不经过炁增伤, 也不触发"玩家造成伤害"类效果。
                var cmd = DamageCmd.Attack(damage)
                    .Targeting(target)
                    .WithValueProp(ValueProp.Move)
                    .FromCard(this, cardPlay);

                if (!TrySwapAttacker(cmd, pet))
                {
                    MainFile.Logger.Warn("[yyl_sts2_mod] 蛇花咬: 攻击者替换失败, 本次按玩家出手。");
                }

                cmd.WithHitFx("vfx/vfx_bite");
                await cmd.Execute(choiceContext);
            }

            await PowerCmd.Apply<WeakPower>(choiceContext, target, WeakAmount, pet, this);
        }
        catch (Exception ex)
        {
            // ★实测出过 NRE (19:14 局), 带上完整堆栈才能定位。
            MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花咬结算失败: {ex}");
        }
    }

    /// <summary>
    ///     把 <see cref="AttackCommand.Attacker" /> (private set) 替换成蛇花小姐。
    ///     引擎没有"指定宠物单体攻击"的公开入口:
    ///     FromMonster 会强制 TargetingAllOpponents, FromOsty 只接受原版 Osty 类型。
    /// </summary>
    private static bool TrySwapAttacker(AttackCommand cmd, Creature pet)
    {
        try
        {
            var setter = AccessTools.PropertySetter(typeof(AttackCommand), nameof(AttackCommand.Attacker));
            if (setter == null) return false;
            // ★PropertySetter 的 Invoke: 第一个参数 = 目标实例, 参数表 = [新值]。
            //   之前把实例混进参数表 → "Parameter count mismatch." (实测 2026-09-29)。
            setter.Invoke(cmd, new object[] { pet });
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 替换攻击者失败: {ex.Message}");
            return false;
        }
    }
}
