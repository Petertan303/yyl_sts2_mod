using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Monsters;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     遗物「蛇花小姐」(任务卡「蛇花」的兑现奖励)。
///     <list type="bullet">
///         <item>每场战斗开始时召唤蛇花小姐 (基础 8 点生命)。</item>
///         <item>若战斗结束时蛇花仍存活, 下一场召唤的生命上限 +3 (累加, 存档保存)。</item>
///     </list>
///     <para>召唤机制照原版 <c>Byrdpip</c>: <c>AddsPet</c>/<c>SpawnsPets</c> + <c>PlayerCmd.AddPet</c>。</para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class SnakeFlowerMiss : yylRelicModel
{
    /// <summary>每存活一场战斗, 生命上限的成长。</summary>
    public const int HpGrowthPerSurvivedCombat = 3;

    private int _bonusHp;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool AddsPet => true;

    public override bool SpawnsPets => true;

    /// <summary>累加的生命上限加成 (跨战斗保存)。</summary>
    [SavedProperty]
    public int BonusHp
    {
        get => _bonusHp;
        set
        {
            AssertMutable();
            _bonusHp = value;
        }
    }

    /// <summary>本场战斗召唤出的宠物引用 (仅运行时, 不存档)。</summary>
    private Creature? _pet;

    public override async Task BeforeCombatStart()
    {
        try
        {
            _pet = await PlayerCmd.AddPet<SnakeFlowerMissPet>(Owner);
            if (_pet == null) return;

            // ★照搬原版 OstyCmd.Summon 的流程:
            //   1) 给宠物挂「为你而死」(DieForYouPower) —— 敌人的攻击会被 redirected 到它身上,
            //      这正是"怪物优先攻击召唤物"的原版实现 (亡灵契约师的奥斯提同款)。
            //   2) 再挂「蛇花护体」(SnakeFlowerGuard) —— 引擎的重定向发生在未格挡段,
            //      宠物的格挡默认永远不结算; 这个能力在 AfterOsty 阶段消费蛇花的格挡。
            //   3) SetMaxHp(基础 8 + 存活成长) + Heal 到满。
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.DieForYouPower>(
                null!, _pet, 1m, null, null);
            await PowerCmd.Apply<Powers.SnakeFlowerGuard>(
                null!, _pet, 1m, null, null);

            var maxHp = SnakeFlowerMissPet.BaseHp + BonusHp;
            await CreatureCmd.SetMaxHp(_pet, maxHp);
            await CreatureCmd.Heal(_pet, maxHp);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 召唤蛇花小姐失败: {ex.Message}");
            _pet = null;
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        try
        {
            // 战斗结束仍存活 → 下一场血上限 +3。
            if (_pet != null && _pet.IsAlive)
            {
                BonusHp += HpGrowthPerSurvivedCombat;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[yyl_sts2_mod] 蛇花小姐存活判定失败: {ex.Message}");
        }
        finally
        {
            _pet = null;
        }

        await Task.CompletedTask;
    }
}
