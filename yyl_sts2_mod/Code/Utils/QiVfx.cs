using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     炁 / 祖炁形态的程序化特效 (2026-11-16)。照金光 RefreshVfx 的模式:
///     <para>
///         · 不预置进 yylCharacter.tscn, 战斗时动态把两个 Sprite2D 挂到角色 Visuals 下
///           (与水波纹等动态特效同路), 场景销毁时自动回收。
///         · <c>QiVortex</c> (mode=0): 脚下丹田气旋 — 层数驱动亮度/转速/第三圈, 8 炁拐点青→金。
///         · <c>ZuQiPillar</c> (mode=1): 祖炁光柱 — 形态激活时贯穿身体, 升级后多一圈外环。
///         · 形态切换: 对两者 reveal 做反向 tween (气旋淡出 → 光柱淡入), 即"气旋提成光柱"。
///     </para>
///     <para>
///         ★2026-11-17 光柱重做后重新启用: 玩家反馈初版 ① 上下被矩形硬切 ② 金色与金光冲突。
///         改法: shader 里外环也吃 yfade、柔化区 0.12→0.22 (消硬边), 光带高斯加宽,
///         颜色金→蓝 (pillar_color); 本类把光柱节点画布加宽加高 (见 <see cref="EnablePillar" />)。
///     </para>
///     纯视觉层, 各端各自刷新, 不参与联机数据同步; 全程 try/catch, 异常只记日志不打断钩链。
/// </summary>
public static class QiVfx
{
    /// <summary>★2026-11-17: 祖炁光柱重做后重新启用 (加宽/加高画布/改蓝/消硬边)。</summary>
    private const bool EnablePillar = true;

    private const string VortexNode = "QiVortex";
    private const string PillarNode = "ZuQiPillar";
    private const string ShaderPath = "res://yyl_sts2_mod/shaders/qi_vortex.gdshader";
    // 仅作 UV 矩形载体 (256x256), 形状完全由 shader 程序化生成。
    private const string CarrierPath = "res://yyl_sts2_mod/images/vfx/yyl_water_ripple_mask.png";
    private const string FormOnMeta = "yyl_form_on";

    // 按引用追踪已订阅的 power 实例 (金光同款): 实例重建后能重新订阅。
    private static readonly HashSet<PowerModel> Subscribed = new(new RefComparer());

    private sealed class RefComparer : IEqualityComparer<PowerModel>
    {
        public bool Equals(PowerModel? x, PowerModel? y) => ReferenceEquals(x, y);
        public int GetHashCode(PowerModel obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>该 power 实例被移除后注销订阅, 让残留委托变成 no-op。</summary>
    public static void Unsubscribe(PowerModel power) => Subscribed.Remove(power);

    /// <summary>
    ///     幂等订阅指定 power 的层数变化。放在 Refresh 里, 任何刷新路径都补订,
    ///     不依赖单点钩子 (DevConsole SetAmount 也能触发)。
    /// </summary>
    private static void EnsureSubscribed(Creature creature, PowerModel power)
    {
        if (!Subscribed.Add(power)) return;
        var weak = new WeakReference<Creature>(creature);
        power.DisplayAmountChanged += () =>
        {
            if (!Subscribed.Contains(power)) return; // 实例已移除
            if (weak.TryGetTarget(out var c)) Refresh(c);
        };
    }

    /// <summary>炁层数 → 亮度: 0.25 起步, 每点 +0.02, 33 炁封顶 0.91。</summary>
    private static float IntensityFor(decimal stacks) =>
        Math.Clamp(0.25f + (float)Math.Min(stacks, 33m) * 0.02f, 0f, 0.91f);

    /// <summary>8 炁拐点换色: 7 炁起过渡, 9 炁全金。</summary>
    private static float WarmFor(decimal stacks) =>
        Math.Clamp((float)(stacks - 7m) / 2f, 0f, 1f);

    /// <summary>
    ///     由炁层数变化 / 形态施加或移除时调用。找不到角色节点时静默跳过
    ///     (战斗外、角色尚未入场属正常情况), 不抛异常。
    /// </summary>
    public static void Refresh(Creature creature)
    {
        try
        {
            if (creature == null) return;
            if (creature.GetCreatureNode() is not NCreature n || n.Visuals == null) return;

            // 幂等补订: 炁与形态两者的层数变化都驱动特效。
            var qiPower = creature.GetPower<Qi>();
            if (qiPower != null) EnsureSubscribed(creature, qiPower);
            var formPower = creature.GetPower<ZuQiForm>();
            if (formPower != null) EnsureSubscribed(creature, formPower);

            var stacks = qiPower?.Amount ?? 0m;
            var form = formPower;
            // ★2026-11-17 用户定调: 金光≥20层 且 祖炁形态 → 佛光切全彩虹, **祖炁锥形隐藏**
            //   (脚底气旋保留), 条件失效自动恢复。
            var aegis = creature.GetPower<GoldenAegis>();
            var rainbowLinked = aegis != null && aegis.Amount >= 20m && form != null;
            // ★2026-11-17 修订: 本方法也被"形态变化"触发, 而佛光的 rainbow_full 原本只在
            //   GoldenAegis.RefreshVfx(金光层数变化)里设 → "先叠金光后开祖炁"时佛光不变色。
            //   这里主动驱动佛光 0.4s 渐变 (SetRainbowLink 幂等且不回call本方法, 无递归)。
            GoldenAegis.SetRainbowLink(creature, rainbowLinked);
            // formOn 为锥形显隐开关: 联动时强制关(锥形淡出); 气旋显隐另按炁量, 不受影响。
            var formOn = EnablePillar && form != null && !rainbowLinked;
            var formLevel = form != null && form.Amount >= 12m ? 1f : 0f;

            var shader = ResourceLoader.Load<Shader>(ShaderPath);
            var carrier = ResourceLoader.Load<Texture2D>(CarrierPath);
            if (shader == null || carrier == null)
            {
                MainFile.Logger.Warn("[yyl] 炁vfx: shader/载体贴图加载失败, 跳过");
                return;
            }

            // ★层级 (2026-11-16): 用同级树顺序插到 Shadow 之后、Visuals 之前 —— 阴影之上、立绘之下。
            //   ⚠ 绝不用负 z_index: 地面是 YylCharacter 之外的战斗场景节点, 负 z 会让这些光效
            //   沉到地面下被不透明草地吞没 (11-16 实测: 阴影/气旋/光柱全消失, z=0 的金光仍在)。
            var vortex = EnsureSprite(n.Visuals, VortexNode, shader, carrier, mode: 0f,
                position: new Vector2(0f, -12f), scale: new Vector2(0.95f, 0.30f), orderAfterShadow: 1);
            if (vortex == null) return;

            // ★光柱暂时关闭: 不创建/不刷新光柱节点。若场上有旧实例(热重载残留), 直接隐藏。
            if (!EnablePillar)
            {
                var stale = n.Visuals.GetNodeOrNull<Sprite2D>(PillarNode);
                if (stale != null) stale.Visible = false;
            }

            var vMat = (ShaderMaterial)vortex.Material!;
            var             pMat = EnablePillar
                ? (ShaderMaterial)EnsureSprite(n.Visuals, PillarNode, shader, carrier, mode: 1f,
                    // ★2026-11-17 加宽加高画布: scale 0.78×1.30 → 1.15×1.75 (256px 载体 → 约 294×448px),
                    //   中心上移到 -150 让加高后的光柱仍覆盖角色全身; shader 内 yfade 同步加大柔化区。
                    position: new Vector2(0f, -150f), scale: new Vector2(1.15f, 1.75f), orderAfterShadow: 2).Material!
                : null;
            var intensity = IntensityFor(stacks);
            var warm = WarmFor(stacks);

            // 参数始终同步 (层数变化即时生效)
            vMat.SetShaderParameter("intensity", intensity);
            vMat.SetShaderParameter("stacks", (float)Math.Min(stacks, 33m));
            vMat.SetShaderParameter("warm", warm);

            // ★光柱关闭: 气旋只按炁量显隐, 完全无视形态, 不做任何过渡。
            if (!EnablePillar)
            {
                vMat.SetShaderParameter("reveal", stacks > 0 ? 1f : 0f);
                vortex.Visible = stacks > 0;
                vortex.SetMeta(FormOnMeta, false);
                return;
            }

            // ↓↓↓ 以下为光柱启用时的原逻辑 (改回 EnablePillar=true 即恢复) ↓↓↓
            var pillar = n.Visuals.GetNodeOrNull<Sprite2D>(PillarNode)!;
            // ★2026-11-17 用户定调: 锥形强度拉高 (原 max(intensity,0.42) 偏暗)。
            pMat!.SetShaderParameter("intensity", Math.Max(intensity * 1.35f, 0.58f));
            pMat.SetShaderParameter("form_level", formLevel);

            // ★2026-11-17 用户定调: 气旋常驻 (保留"炁"的特效), 祖炁锥形**叠加**其上,
            //   不再"气旋提成锥形"互相顶替。形态只控制锥形的显隐。
            // 形态开关翻转时才做 reveal 过渡; 战斗开始首次刷新也走这里。
            // ★未初始化时 GetMeta 会让 Godot 刷 "does not have any meta with key" ERROR, 先判 HasMeta。
            var wasOn = vortex.HasMeta(FormOnMeta) && vortex.GetMeta(FormOnMeta).AsBool();
            // 气旋始终按炁量显示, 与形态无关。
            vMat.SetShaderParameter("reveal", stacks > 0 ? 1f : 0f);
            vortex.Visible = stacks > 0;

            if (wasOn == formOn)
            {
                // 无翻转: 直接同步锥形 reveal, 避免残留过渡把刚设的参数覆盖。
                pMat.SetShaderParameter("reveal", formOn ? 1f : 0f);
                pillar.Visible = formOn;
                return;
            }

            vortex.SetMeta(FormOnMeta, formOn);
            var pt = pillar.CreateTween();
            pillar.Visible = true;
            if (formOn)
            {
                // 祖炁激活: 锥形渐入 (气旋不动, 保持底部圆盘)。
                pt.TweenProperty(pMat, "shader_parameter/reveal", 1f, 0.85f)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            }
            else
            {
                // 形态移除: 锥形淡出。
                pt.TweenProperty(pMat, "shader_parameter/reveal", 0f, 0.60f)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            }

            // 过渡结束后收起锥形(仅在移除时), 气旋永不隐藏。
            if (!formOn) HideAfter(pillar, pMat, 0.65f);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 炁vfx 刷新失败: {ex.Message}");
        }
    }

    private static Sprite2D? EnsureSprite(Node parent, string name, Shader shader, Texture2D carrier,
        float mode, Vector2 position, Vector2 scale, int orderAfterShadow)
    {
        var node = parent.GetNodeOrNull<Sprite2D>(name);
        if (node == null)
        {
            var mat = new ShaderMaterial { Shader = shader };
            mat.SetShaderParameter("mode", mode);
            mat.SetShaderParameter("reveal", 0f);
            node = new Sprite2D
            {
                Name = name,
                Texture = carrier,
                Material = mat,
                Position = position,
                Scale = scale,
                Visible = false,
            };
            parent.AddChild(node);
        }
        else if (node.Material is ShaderMaterial existing && existing.Shader != shader)
        {
            existing.Shader = shader;
        }

        // ★每次刷新都同步 transform: 改了尺寸/位置参数后, 热重载残留的旧实例也能立刻跟上,
        //   不必等节点重建。Position/Scale 不被 tween 动画修改 (tween 只动 shader reveal), 安全。
        node.Position = position;
        node.Scale = scale;

        // 每次刷新都把节点排到 Shadow 之后 (即阴影之上、立绘之下)。用阴影索引 + 偏移而非固定序号,
        // 兼容 tscn 节点顺序变化; 气旋在前、光柱在后, 两者顺序稳定不会反复互换打断 tween。
        var shadow = parent.GetNodeOrNull<Node2D>("Shadow");
        if (shadow != null)
        {
            var wanted = shadow.GetIndex() + orderAfterShadow;
            if (node.GetIndex() != wanted) parent.MoveChild(node, wanted);
        }
        return node;
    }

    private static async void HideAfter(Sprite2D node, ShaderMaterial mat, float seconds)
    {
        await node.ToSignal(node.GetTree().CreateTimer(seconds), Godot.Timer.SignalName.Timeout);
        try
        {
            var reveal = mat.GetShaderParameter("reveal").AsSingle();
            if (reveal <= 0.01f) node.Visible = false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 炁vfx HideAfter 失败: {ex.Message}");
        }
    }
}
