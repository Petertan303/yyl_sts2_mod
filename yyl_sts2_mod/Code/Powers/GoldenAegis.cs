using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

public class GoldenAegis : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ── 敌方回合末按「单次命中的最大用层数」衰退 ──────────────────────────────
    // 2026-11-08 定调 1:1 衰减；2026-11-09 改为按「本次命中实际动用的金光层数」
    //   = ceil(本次实际抵消的掉血 / perStack)，守势(perStack 2 / 3)下挡 3 点只动用 1 层，
    //   于是守势不再「多挡也多付账」，同样层数更耐用。
    // ★ 2026-11-09 二修（用户定调）：
    //   ① 结算从玩家回合末改到**敌方回合末**（side != Owner.Side）。敌方攻击发生在
    //      玩家回合结束之后，旧挂点结的是上一轮账、要等下一回合末才掉层；新挂点打完即结算。
    //   ② 多段攻击取**最大值**而非累加：敌人 A 打 3×3、敌人 B 打 4 且都被金光挡下时，
    //      只掉 4 层 = max(3,3,3,4)，不是 13 层也不是 7 层。账本因此只需存一个标量。
    // ⚠ 掉血修正钩子会被伤害预览调用（引擎 HpLossHookPhase.All 文档点名 damage
    //   previews），所以不能在这里直接入账。用 BeforeDamageReceived 开一笔草稿、
    //   AfterDamageReceived 确认入账；预览不派发这两个事件，草稿会被下一次真实
    //   伤害的 Before 清掉，不会污染账本。草稿与账期都取 max，多段命中无论引擎
    //   怎么分组都不会累加。
    private decimal _maxStacksUsedThisPeriod;
    private decimal _draftStacksUsed;

    public override Task BeforeDamageReceived(
        PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner) _draftStacksUsed = 0m;
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner)
        {
            // 多段命中只记「单次用层数」的最大值，不累加。
            _maxStacksUsedThisPeriod = Math.Max(_maxStacksUsedThisPeriod, _draftStacksUsed);
            _draftStacksUsed = 0m;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    ///     敌方回合结束时，按本账期「单次命中的最大用层数」扣层，走 yylAegis.Consume 统一入口。
    ///     2026-11-09 用户定调：旧的玩家回合末挂点结的是上一轮账（敌方攻击在其后发生），
    ///     会晚一整个回合才掉层；改挂敌方回合末（side != Owner.Side）后打完即结算。
    ///     衰减因此会被金光化炁 / 铜镜等 IAegisConsumed 监听者捕获 —— 用户定调这是
    ///     玩家可主动构筑的决策选项，不是漏洞。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // 只处理敌方回合结束；这里判的是「谁的回合结束」，联机时全员同属一个 CombatSide，
        // 与 AfterPlayerTurnStart 里必须判 player == Owner 的情形不同。
        if (Owner == null || side == Owner.Side) return;
        try
        {
            var used = _maxStacksUsedThisPeriod;
            _maxStacksUsedThisPeriod = 0m;
            // used 已是单次命中的最大用层数；仍 clamp 到当前 Amount，
            // yylAegis.Consume 内部还会再 clamp 一次。
            var decay = (int)Math.Min(Math.Floor(used), Amount);
            if (decay <= 0) return;

            var player = Owner.CombatState?.Players.FirstOrDefault(p => p.Creature == Owner);
            if (player == null) return;

            MainFile.Logger.Info($"[yyl] 金光衰退(敌方回合末): 单次最大用层={used} 扣层={decay} 结算前={Amount}");
            await yylAegis.Consume(ctx, player, decay, null);
        }
        catch (Exception ex)
        {
            // 回合末钩子抛异常会冻结整场战斗。
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光衰退结算失败: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ⚠ 减伤挂点 (2026-09-20 重做): 此前实现为 IModifyDamageAdditive (伤害管线加法阶段),
    //   但伤害管线在格挡后有"至少掉 1 血"的下限钳制 —— 层数再高也剩 1 点, 减不到 0。
    //   现改为 override 原版掉血修正钩子 ModifyHpLostAfterOstyLate (原版遗物「钨合金棍」
    //   TungstenRod / BufferPower 同款阶段): 伤害 → 格挡 → 表观掉血(下限1) → 本钩子,
    //   在这里可以减到 0。
    //   代价: 敌方意图预览不再显示金光减免后的数字 (钨合金棍同款行为)。
    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || props.HasFlag(ValueProp.Unpowered))
            return amount;

        // 只减"敌方来源"的掉血: dealer 为空 (中毒/状态牌/自伤类 LoseHp) 或来自己方
        // (联机误伤) 时不减 —— 否则燃炁/拉伤这类固定代价也会被金光吃掉。
        if (dealer == null || dealer.Side == Owner.Side)
            return amount;

        // 基础每层减伤 1 点 (2026-09-21 用户定调: 2 太强); 「守势」会再往上加 (每层 +1)。
        var perStack = 1m + (Owner.GetPower<ShouShi>()?.Amount ?? 0m);
        // 减到 0 为止 (掉血阶段没有下限钳制, 这是与伤害阶段实现的本质区别)。
        var reduced = Math.Max(0m, amount - Amount * perStack);
        // 本次命中实际动用的层数 = ceil(实际抵消的掉血 / 每层减伤)，供敌方回合末结算。
        // 守势下 perStack 为 2 / 3：挡 3 点只动用 1 层，结算时也只掉 1 层。
        // perStack 恒 >= 1（基础 1 + 守势层数），不会除零；prevented 为 0 时 ceil 也是 0。
        // 草稿取 max：若引擎把多段伤害并进同一笔 Before/After，仍只记最大的那一段。
        var prevented = amount - reduced;
        _draftStacksUsed = Math.Max(_draftStacksUsed, Math.Ceiling(prevented / perStack));
        return reduced;
    }

    /// <summary>
    ///     金光层数变化时刷新周身金光特效。纯视觉层: 各端各自刷新, 不影响联机数据同步。
    ///     ⚠ DevConsole "移除 power 后重新添加" 等路径会把 target 传成 null —— 必须用 Owner 兜底。
    ///       此前在 AfterApplied 里直接解引用 target 抛 NRE, 打断了 DisplayAmountChanged 订阅,
    ///       之后该新实例的所有层数变化都不再刷新特效 (2026-11-04 实测)。
    /// </summary>
    public override Task AfterApplied(Creature target, CardModel source)
    {
        try
        {
            var creature = ResolveCreature(target);
            if (creature != null) RefreshVfx(creature);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光 AfterApplied 失败: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature target)
    {
        try
        {
            // this 就是要移除的 power 实例: 刷新后注销订阅, 使残留委托变成 no-op,
            // 也让"重新获得金光"产生的新实例能被重新订阅 (引用去重不会误判为已订阅)。
            // ⚠ 注销必须放在 RefreshVfx 之后 —— RefreshVfx 会补订当前实例, 提前注销会被它加回去。
            var creature = ResolveCreature(target);
            if (creature != null) RefreshVfx(creature);
            Subscribed.Remove(this);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光 AfterRemoved 失败: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private Creature? ResolveCreature(Creature? target)
    {
        if (target != null) return target;
        if (Owner != null) return Owner;
        MainFile.Logger.Info("[yyl] 金光vfx: target/Owner 均为空, 跳过刷新");
        return null;
    }

    // 按引用追踪已订阅实例: 不调用 PowerModel 可能重写的 Equals/GetHashCode
    // (刚重建/移除态的对象未必初始化完整), 且实例重建后能重新订阅。
    private static readonly HashSet<PowerModel> Subscribed = new(new RefComparer());

    private sealed class RefComparer : IEqualityComparer<PowerModel>
    {
        public bool Equals(PowerModel? x, PowerModel? y) => ReferenceEquals(x, y);
        public int GetHashCode(PowerModel obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    ///     幂等确保当前金光实例已订阅层数变化事件。放在 RefreshVfx 里, 任何刷新路径
    ///     (卡牌 Apply / 移除 / DevConsole SetAmount) 都会补订, 不依赖 AfterApplied 单点。
    /// </summary>
    private static void EnsureSubscribed(Creature creature)
    {
        var power = creature.GetPower<GoldenAegis>();
        if (power == null || !Subscribed.Add(power)) return;
        var weak = new WeakReference<Creature>(creature);
        power.DisplayAmountChanged += () =>
        {
            if (!Subscribed.Contains(power)) return; // 该实例已注销 (power 被移除)
            if (weak.TryGetTarget(out var c)) RefreshVfx(c);
        };
    }

    // ── 逐帧 SDF ──────────────────────────────────────────────────────────────
    // 三层金光用一张静态 golden_sdf.png 贴 idle 轮廓, 动画换帧后会错位。这里按动画当前帧
    // 切换到预生成的 golden_sdf_<帧名>.png (同一公式: 241x190 帧居中放入 385x334 画布,
    // edt(外)-edt(内), d=clip(0.5+sd/144,0,1)), 形状与 idle 版精度一致。
    // 佛光 BuddhaAura 是程序化圆/水滴形状、不采样贴图值, 无需切换。
    private const string FrameSdfDir = "res://yyl_sts2_mod/images/ui/golden_sdf_";
    private const string IdleSdfPath = FrameSdfDir + "yyl.png";
    private static readonly Dictionary<string, Texture2D?> FrameSdfCache = new();

    private static Texture2D? GetFrameSdf(string? frameTexPath)
    {
        var name = string.IsNullOrEmpty(frameTexPath)
            ? "yyl"
            : System.IO.Path.GetFileNameWithoutExtension(frameTexPath);
        if (FrameSdfCache.TryGetValue(name, out var cached)) return cached;
        // 直接 Load (工程内动态贴图均如此): Exists 对导入型资源的行为未验证,
        // 若误判 false 会静默全回退 idle 形状, 表现为"逐帧没生效"。
        var tex = ResourceLoader.Load<Texture2D>(FrameSdfDir + name + ".png");
        if (tex == null) tex = ResourceLoader.Load<Texture2D>(IdleSdfPath); // 未预生成的帧回退 idle
        FrameSdfCache[name] = tex;
        return tex;
    }

    /// <summary>
    ///     把三层金光的 SDF 贴图切到当前动画帧对应的轮廓 (FrameChanged/AnimationChanged 驱动)。
    ///     纯视觉层, 各端各自同步, 不影响联机数据。钩子绝不抛异常。
    /// </summary>
    private static void SyncFrameSdf(AnimatedSprite2D anim, Sprite2D[] layers)
    {
        try
        {
            if (anim.SpriteFrames == null) return;
            var frameTex = anim.SpriteFrames.GetFrameTexture(anim.Animation, anim.Frame);
            var sdf = GetFrameSdf(frameTex?.ResourcePath);
            if (sdf == null) return;
            foreach (var layer in layers)
                if (layer.Texture != sdf) layer.Texture = sdf;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光逐帧SDF同步失败: {ex.Message}");
        }
    }

    private static void EnsureFrameSdfHook(AnimatedSprite2D anim, Sprite2D[] layers)
    {
        // Meta 标记挂在节点上, 随节点销毁自动回收 (不用静态集合强引用 Visuals)
        if (anim.HasMeta("yyl_golden_sdf_hook")) return;
        anim.SetMeta("yyl_golden_sdf_hook", true);
        anim.FrameChanged += () => SyncFrameSdf(anim, layers);
        anim.AnimationChanged += () => SyncFrameSdf(anim, layers);
        SyncFrameSdf(anim, layers); // 立即同步当前帧 (金光出现时角色可能正在播动画)
    }

    /// <summary>
    ///     按当前金光层数更新周身金光特效 (全部预置在 yylCharacter.tscn):
    ///     <list type="number">
    ///         <item>GoldenInner —— 硬边: 外扩 2 → 20px 随层数线性 (1 → 50 层), 恒定全亮;</item>
    ///         <item>GoldenOuter —— 光晕: 外扩 2 → 10px, 18px 宽渐隐, 明度 0.2 → 0.95;</item>
    ///         <item>GoldenBody —— 贴身: 固定内收 4px, 明度 0.2 → 0.35 随层数;</item>
    ///         <item>BuddhaAura —— 佛光: 30 层起点亮, 30 → 50 层强度渐入 (0.25 → 0.80);
    ///             独立于立绘的解析水滴/正圆光轮, 画在最底层, 详见 buddha_aura.gdshader。</item>
    ///     </list>
    ///     金光三层共用一张有符号距离场贴图 golden_sdf.png (1 单位 = 144px), 层数封顶 50,
    ///     归零隐藏; 外扩与明度经 shader 参数实时驱动。佛光形状程序化、尺寸固定不随层数。
    /// </summary>
    public static void RefreshVfx(Creature creature)
    {
        try
        {
            if (creature == null)
            {
                MainFile.Logger.Info("[yyl] 金光vfx: creature 为空, 跳过");
                return;
            }
            EnsureSubscribed(creature); // 任何刷新路径都补订层数变化事件 (幂等)
            var amount = creature.GetPower<GoldenAegis>()?.Amount ?? 0m;
            if (creature.GetCreatureNode() is not NCreature n || n.Visuals == null)
            {
                MainFile.Logger.Info($"[yyl] 金光vfx: creatureNode/Visuals 获取失败 amount={amount}");
                return;
            }

            var body = n.Visuals.GetNodeOrNull("GoldenBody") as Sprite2D;
            var inner = n.Visuals.GetNodeOrNull("GoldenInner") as Sprite2D;
            var outer = n.Visuals.GetNodeOrNull("GoldenOuter") as Sprite2D;
            var aura = n.Visuals.GetNodeOrNull("BuddhaAura") as Sprite2D;
            if (body == null || inner == null || outer == null || aura == null)
            {
                var children = string.Join(",", n.Visuals.GetChildren().Select(c => c.Name));
                MainFile.Logger.Info($"[yyl] 金光vfx: 节点缺失 body={body != null} inner={inner != null} outer={outer != null} aura={aura != null} | Visuals 子节点: {children}");
                return;
            }

            var bodyMat = body.Material as ShaderMaterial;
            var innerMat = inner.Material as ShaderMaterial;
            var outerMat = outer.Material as ShaderMaterial;
            var auraMat = aura.Material as ShaderMaterial;
            if (bodyMat?.Shader == null || innerMat?.Shader == null || outerMat?.Shader == null || auraMat?.Shader == null)
            {
                MainFile.Logger.Info($"[yyl] 金光vfx: material/shader 缺失 body={bodyMat?.Shader != null} inner={innerMat?.Shader != null} outer={outerMat?.Shader != null} aura={auraMat?.Shader != null}");
                return;
            }

            // 逐帧 SDF: 让贴轮廓的三层金光跟随当前动画帧形状 (佛光为程序化形状, 不参与)
            if (n.Visuals.GetNodeOrNull("Visuals") is AnimatedSprite2D anim && anim.SpriteFrames != null)
                EnsureFrameSdfHook(anim, new[] { inner, outer, body });

            var stacks = (int)Math.Min(amount, 50m);
            var visible = stacks > 0;
            inner.Visible = visible;
            outer.Visible = visible;
            body.Visible = visible && stacks >= 4; // 贴身光 4 层起才出现
            var wasAuraOn = aura.Visible;
            aura.Visible = visible && stacks >= 20; // 佛光 20 层起 (2026-11-08 由 12 层上调)
            // 佛光点亮时刻 (层数从 <20 跨到 >=20, 含归零后重堆): 自研水波纹空间扭曲
            // (ScreenRefraction 因缺 BackBufferCopy 读空屏无效; BeckonRipple 是淡入淡出贴图无扭曲)。
            if (aura.Visible && !wasAuraOn)
            {
                yylVfx.WaterRipple(creature, raiseFraction: 0, sizePx: 800f, duration: 1.3f ,peakIntensity: 0.005f);
                // 佛光延迟淡入: 水波先起, 时延 0.25s 后从 0 强度缓入当前层数对应强度 (0.6s)。
                FadeAuraTo(aura, auraMat, stacks, delay: 0.35f, fade: 1.7f, resetToZero: true);
                MainFile.Logger.Info($"[yyl] 金光vfx: 佛光点亮 (延迟淡入) stacks={stacks} 目标强度={AuraTargetIntensity(stacks):0.00}");
            }
            else if (aura.Visible)
            {
                // 已点亮状态层数变化: 淡入进行中则平滑重定向到新目标, 否则直接设值。
                if (aura.HasMeta(AuraFadeTweenMeta))
                    FadeAuraTo(aura, auraMat, stacks, delay: 0.15f, fade: 0.85f, resetToZero: false);
                else
                    auraMat.SetShaderParameter("intensity", AuraTargetIntensity(stacks));
            }
            else if (wasAuraOn)
            {
                StopAuraFade(aura); // 熄灭: 取消未完成的淡入
            }
            if (!visible)
            {
                MainFile.Logger.Info("[yyl] 金光vfx: 层数为 0, 隐藏");
                return;
            }

            // 外扩: 硬边 2 → 4px、光晕 2 → 10px (1 → 50 层线性; SDF 1 单位 = 144px)。
            var tInner = 0.5f + (2f + (stacks - 1) / 49f * 2f) / 144f;
            // var tOuter = 0.5f + (2f + (stacks - 1) / 49f * 8f) / 144f;
            var tOuter = 0.5f + (3f + (stacks - 1) / 49f * 10f) / 144f;

            innerMat.SetShaderParameter("threshold", tInner);
            outerMat.SetShaderParameter("threshold", tOuter);
            bodyMat.SetShaderParameter("threshold", Math.Max(0.5f - 6f / 144f, 0.5f - (stacks * 0.01f + 2f) / 144f)); 
            // 明度: 硬边半透明 0.4 → 0.9; 贴身 0.2 → 0.35 (50 层线性); 光晕 0.2 → 0.95。
            innerMat.SetShaderParameter("intensity", Math.Min(0.60f, 0.40f + stacks * 0.01f));
            bodyMat.SetShaderParameter("intensity", Math.Min(0.25f, 0.10f + stacks * 0.003f));
            outerMat.SetShaderParameter("intensity", Math.Min(0.95f, 0.20f + stacks * 0.015f));

            // 佛光强度不在此处直接设置 —— 由上方点亮淡入 / 层数变化逻辑统一驱动
            // (FadeAuraTo / AuraTargetIntensity), 避免 tween 被直设值覆盖导致淡入失效。

            // ★2026-11-17 用户定调: 金光≥20层 且 处于祖炁形态 → 佛光切"全彩虹",
            //   同时隐藏祖炁锥形(脚底气旋保留); 条件失效自动恢复。描边三层不受影响。
            var inZuQi = creature.GetPower<ZuQiForm>() != null;
            var rainbowLinked = stacks >= 20 && inZuQi;
            // ★2026-11-17 修订: 0.4s 渐变切换 (原为瞬间设值)。SetRainbowLink 幂等,
            //   开祖炁形态那条路径由 QiVfx.Refresh 也会调用它, 这里重复调用无副作用。
            SetRainbowLink(creature, rainbowLinked);
            // 同步 QiVfx: 让它按同一联动条件隐藏/恢复祖炁锥形 (幂等, 不会递归)。
            QiVfx.Refresh(creature);
            MainFile.Logger.Info($"[yyl] 金光vfx: 刷新 stacks={stacks} 硬边={(tInner - 0.5f) * 144:0.#}px 贴身={body.Visible} 佛光={aura.Visible} 全彩虹联动={rainbowLinked}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 金光特效刷新失败: {ex.Message}");
        }
    }

    // ── 佛光全彩虹联动 (2026-11-17): 0.4s 渐变 ──

    private const string RainbowTweenMeta = "yyl_rainbow_tween";
    private const string RainbowTargetMeta = "yyl_rainbow_target";

    /// <summary>
    ///     佛光全彩虹开关, 0.4s 渐变。金光层数变化 (RefreshVfx) 与祖炁形态变化 (QiVfx.Refresh)
    ///     两条路径都会调用; 目标值未变时幂等跳过, 本方法不回调两者, 无递归风险。
    ///     只改佛光的 rainbow_full uniform —— 描边三层与脚下炁盘不受影响。
    /// </summary>
    public static void SetRainbowLink(Creature creature, bool linked)
    {
        try
        {
            if (creature == null) return;
            var node = creature.GetCreatureNode();
            if (node is not NCreature n || n.Visuals == null) return;
            var aura = n.Visuals.GetNodeOrNull("BuddhaAura") as Sprite2D;
            if (aura == null || !GodotObject.IsInstanceValid(aura)) return;
            if (aura.Material is not ShaderMaterial mat || mat.Shader == null) return;

            // 幂等: 目标未变且没有进行中的渐变 → 跳过 (两条驱动路径会重复触发)。
            if (aura.HasMeta(RainbowTargetMeta) && aura.GetMeta(RainbowTargetMeta).AsBool() == linked
                && !aura.HasMeta(RainbowTweenMeta))
                return;
            aura.SetMeta(RainbowTargetMeta, linked);

            // ★取消旧 tween: 必须先校验实例有效性 —— Godot 在 tween 完成后会释放它,
            //   对已释放的 Tween 调 Kill() 会抛 NRE (11-17 实机两次 NRE 的根因)。
            if (aura.HasMeta(RainbowTweenMeta))
            {
                var oldObj = aura.GetMeta(RainbowTweenMeta).AsGodotObject();
                if (oldObj is Tween oldTween && GodotObject.IsInstanceValid(oldTween))
                    oldTween.Kill();
                aura.RemoveMeta(RainbowTweenMeta);
            }

            var target = linked ? 1f : 0f;

            // 节点不在场景树中时无法 CreateTween (会返回 null) → 降级直接设值。
            if (!aura.IsInsideTree())
            {
                mat.SetShaderParameter("rainbow_full", target);
                MainFile.Logger.Info($"[yyl] 佛光彩虹渐变: 节点不在树, 直接设值 {target}");
                return;
            }

            var tw = aura.CreateTween();
            if (tw == null)
            {
                mat.SetShaderParameter("rainbow_full", target);
                MainFile.Logger.Warn("[yyl] 佛光彩虹渐变: CreateTween 返回 null, 降级直接设值");
                return;
            }

            // ★2026-11-17 三次实测修正: TweenProperty(mat,"shader_parameter/rainbow_full") 在此
            //   material 上解析属性路径抛 NRE (而同 shader 的 intensity 却正常) —— 改用 TweenMethod
            //   直接驱动 setter 闭包, 不走属性路径解析 (照 yylAudio.cs 范式)。先从 material 读当前值
            //   作为起点, 保证中途反转从当前值平滑过渡。
            var from = linked ? 0f : 1f;
            try
            {
                var cur = mat.GetShaderParameter("rainbow_full");
                if (cur.VariantType == Variant.Type.Float) from = cur.AsSingle();
            }
            catch { /* 读不到就用端点值 */ }
            mat.SetShaderParameter("rainbow_full", from);

            var weakMat = new WeakReference<ShaderMaterial>(mat);
            tw.TweenMethod(
                Callable.From<float>(v =>
                {
                    if (weakMat.TryGetTarget(out var m) && GodotObject.IsInstanceValid(m))
                        m.SetShaderParameter("rainbow_full", v);
                }),
                from, target, 0.4f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            aura.SetMeta(RainbowTweenMeta, tw);
            MainFile.Logger.Info($"[yyl] 佛光彩虹渐变: TweenMethod {from:0.00}→{target:0.00} (linked={linked})");
        }
        catch (Exception ex)
        {
            // ★打完整堆栈(含出错行), 便于定位残余 NRE 精确来源
            MainFile.Logger.Warn($"[yyl] 佛光彩虹渐变失败: {ex}");
        }
    }

    // ── 佛光时延淡入 (2026-11-04): 点亮时强度归零, 经时延后渐入当前层数目标值 ──

    private const string AuraFadeTweenMeta = "yyl_aura_fade_tween";

    /// <summary>当前层数对应的佛光目标强度 (30 → 50 层: 0.25 → 0.80)。</summary>
    private static float AuraTargetIntensity(int stacks)
        => 0.65f + Math.Min(Math.Max(stacks - 30, 0), 20) / 20f * 0.55f;

    private static void StopAuraFade(Sprite2D aura)
    {
        if (!aura.HasMeta(AuraFadeTweenMeta)) return;
        if (aura.GetMeta(AuraFadeTweenMeta).AsGodotObject() is Tween t)
            t.Kill();
        aura.RemoveMeta(AuraFadeTweenMeta);
    }

    /// <summary>
    ///     佛光强度补间到当前层数的目标值。<paramref name="resetToZero" /> = 点亮淡入
    ///     (强度先归零, 经 <paramref name="delay" /> 时延后渐亮, Sine Out);
    ///     否则从当前强度平滑重定向 (淡入进行中层数又变化时)。
    ///     Tween 引用挂在节点 Meta 上, 供下次层数变化 / 熄灭时 kill。
    /// </summary>
    private static void FadeAuraTo(Sprite2D aura, ShaderMaterial mat, int stacks, float delay, float fade, bool resetToZero)
    {
        StopAuraFade(aura);
        if (resetToZero)
            mat.SetShaderParameter("intensity", 0f);
        var tw = aura.CreateTween();
        if (delay > 0f) tw.TweenInterval(delay);
        tw.TweenProperty(mat, "shader_parameter/intensity", AuraTargetIntensity(stacks), fade)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        aura.SetMeta(AuraFadeTweenMeta, tw);
        tw.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(aura) && aura.HasMeta(AuraFadeTweenMeta))
                aura.RemoveMeta(AuraFadeTweenMeta);
        };
    }
}
