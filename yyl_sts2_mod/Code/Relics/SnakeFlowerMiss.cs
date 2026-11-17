using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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
///         <item>遗物图标显示计数 = 蛇花下次召唤的血量 (8 + BonusHp);
///             触发 +3 时照原版 <c>PaelsWing</c> 的模式: <c>IsActivating = true</c>
///             → <c>Flash</c> 闪光 → 等 1 秒 → 复位, 期间 <c>DisplayAmount</c> 显示 "+3"。</item>
///     </list>
///     <para>召唤机制照原版 <c>Byrdpip</c>: <c>AddsPet</c>/<c>SpawnsPets</c> + <c>PlayerCmd.AddPet</c>。</para>
///     <para>商店房间里在身边显示跟随的蛇花小姐 (照原版 Byrdpip 进商店跟随, 纯视觉)。</para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class SnakeFlowerMiss : yylRelicModel
{
    /// <summary>每存活一场战斗, 生命上限的成长。</summary>
    public const int HpGrowthPerSurvivedCombat = 3;

    /// <summary>商店立绘场景 (yylMerchant.tscn) 的分组名, 用于场景树查找。</summary>
    public const string MerchantVisualGroup = "yyl_merchant_visual";

    /// <summary>战斗/过场立绘场景 (yylCharacter.tscn) 的分组名。</summary>
    public const string CharacterVisualGroup = "yyl_character_visual";

    private int _bonusHp;

    public override RelicRarity Rarity => RelicRarity.Event;

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

    // ==== 计数显示与触发特效 (照原版 PaelsWing 的三件套) ====

    /// <summary>触发 +3 时为 true —— DisplayAmount 显示本次增量, 遗物闪光。</summary>
    private bool _isActivating;

    /// <summary>激活中标记的 change 通知 (照 PaelsWing: setter 内 InvokeDisplayAmountChanged)。</summary>
    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            _isActivating = value;
            InvokeDisplayAmountChanged();
        }
    }

    /// <summary>遗物图标上的计数 = 蛇花下次召唤的血量; 触发瞬间显示本次 +3。</summary>
    public override bool ShowCounter => true;

    public override int DisplayAmount => IsActivating
        ? HpGrowthPerSurvivedCombat
        : SnakeFlowerMissPet.BaseHp + BonusHp;

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

            // ★定位 + 体型成长 (公式照原版 Osty, 但不用引擎的 GetOstyOffsetFromPlayer 定位):
            //   引擎公式 = 主人 + (150~250, -75) —— Y=-75 是给 Spine 场景内部补偿用的,
            //   帧动画场景没有那层补偿, 直接用会悬空 75px (实测"靠上")。
            //   改为: 脚底对齐主人脚底 (creatureNode.GlobalPosition 就是脚底中心),
            //   x = 主人右侧 0.35 身位; 缩放从 1 倍基础 tween 到目标 (位置不动, 只长个)。
            if (_pet.GetCreatureNode() is NCreature n && n.Visuals != null)
            {
                var ownerNode = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
                if (ownerNode != null)
                {
                    var w = ownerNode.Visuals.Bounds.Size.X;
                    var h = ownerNode.Visuals.Bounds.Size.Y;
                    n.GlobalPosition = ownerNode.GlobalPosition + new Godot.Vector2(w * 0.95f, h * -0.03f);
                }

                var growth = Godot.Mathf.Lerp(0.5f, 2.5f, Godot.Mathf.Clamp(maxHp / 100f, 0f, 1f));
                var target = new Godot.Vector2(0.9f * growth, 0.9f * growth);
                n.Visuals.Scale = new Godot.Vector2(0.9f, 0.9f);
                var tw = n.Visuals.CreateTween();
                tw.TweenProperty(n.Visuals, "scale", target, 1.5f)
                  .SetTrans(Tween.TransitionType.Back)
                  .SetEase(Tween.EaseType.Out);
            }
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
        var survived = false;
        try
        {
            // 战斗结束仍存活 → 下一场血上限 +3, 并播放佩尔之翼同款触发特效。
            if (_pet != null && _pet.IsAlive)
            {
                survived = true;
                BonusHp += HpGrowthPerSurvivedCombat;
                MainFile.Logger.Info($"[yyl_sts2_mod] 蛇花小姐存活, 血上限成长至 {SnakeFlowerMissPet.BaseHp + BonusHp}");
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

        // ★照原版 PaelsWing.DoActivateVisuals 的时序:
        //   IsActivating=true (计数切到 "+3") → Flash 闪光 → 等 1 秒 → 复位。
        if (survived)
        {
            IsActivating = true;
            Flash();
            await Cmd.Wait(1f, false);
            IsActivating = false;
        }
    }

    /// <summary>
    ///     进商店房间时, 显示蛇花小姐的跟随精灵 (照原版 Byrdpip 进商店跟随)。
    ///     <para>
    ///         ★实现方式 (2026-10-21 定稿): 精灵**预置**在商店立绘场景
    ///         (<c>yylMerchant.tscn</c>, 分组 <c>yyl_merchant_visual</c>) 的
    ///         <c>PetSprite</c> (位置/可见性由场景与用户编辑器管理) —— 渲染上下文随场景
    ///         完全一致 (暂停变暗/被 UI 覆盖时同步, 不会悬浮在 UI 之上)。
    ///         本方法只把它的 <c>Visible</c> 打开, 不动态创建任何节点。
    ///     </para>
    ///     <para>
    ///         商店 UI 异步构建, 立绘晚于本钩子出现 —— 逐帧等待 (最多约 1.5 秒)。
    ///         离开商店随场景销毁, 天然重置为隐藏。联机: 各端各自显示各自的立绘精灵。
    ///     </para>
    /// </summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // 分组选择: 商店房 → 商店立绘; 建筑师终局过场 (事件房 + TheArchitect) → 角色立绘。
        var visualGroup = room switch
        {
            MerchantRoom => MerchantVisualGroup,
            EventRoom { CanonicalEvent: MegaCrit.Sts2.Core.Models.Events.TheArchitect } => CharacterVisualGroup,
            _ => (string?)null
        };
        if (visualGroup == null) { await Task.CompletedTask; return; }
        try
        {
            if (Engine.GetMainLoop() is not SceneTree tree) return;

            var shown = 0;
            for (var i = 0; i < 90; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                foreach (var node in tree.GetNodesInGroup(visualGroup))
                {
                    // 角色场景: group 在 PetSprite 自身 (NodeFactory 转换会替换场景根, group 随子节点走);
                    // 商店场景: group 在场景根, PetSprite 是其子节点。
                    var pet = visualGroup == CharacterVisualGroup
                        ? node as Sprite2D
                        : node.GetNodeOrNull("PetSprite") as Sprite2D;
                    if (pet == null) continue;
                    pet.Visible = true;


                    // 体型随生命上限成长 (与战斗内同款公式): 场景里的 scale 是基础值,
                    // 乘成长系数后 tween 过渡 —— PetSprite 已脚底原点化 (offset=-半高),
                    // 缩放围绕脚底, 位置与脚的位置不变。
                    var maxHp = SnakeFlowerMissPet.BaseHp + BonusHp;
                    var growth = Godot.Mathf.Lerp(0.5f, 2.5f, Godot.Mathf.Clamp(maxHp / 100f, 0f, 1f));
                    var baseScale = pet.Scale;
                    // 角色投影反向补偿: CharShadow 挂在 PetSprite 下 (为被蛇花轮廓裁剪),
                    // 会继承蛇花的成长缩放 —— 但角色本身没变大, 影子应恒定。
                    // 场景里存的是 1 倍基准, 运行时除以 growth 反向抵消。
                    var shadowPath = visualGroup == CharacterVisualGroup
                        ? "CharShadow"
                        : "PetSprite/CharShadow";
                    if (node.GetNodeOrNull(shadowPath) is Sprite2D shadow && growth > 0.01f)
                    {
                        shadow.Scale /= growth;
                        shadow.Position /= growth;
                    }
                    var tw = pet.CreateTween();
                    tw.TweenProperty(pet, "scale", baseScale * growth, 0.35f)
                      .SetTrans(Tween.TransitionType.Back)
                      .SetEase(Tween.EaseType.Out);
                    shown++;
                }
                if (shown > 0) break;
            }

            if (shown == 0)
                MainFile.Logger.Warn("[yyl_sts2_mod] 蛇花小姐: 未找到商店立绘的 PetSprite, 跟随未显示");
            else
                MainFile.Logger.Info($"[yyl_sts2_mod] 蛇花小姐: 商店跟随已显示 x{shown}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 商店跟随显示失败: {ex.Message}");
        }
        await Task.CompletedTask;
    }
}
