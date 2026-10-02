using MegaCrit.Sts2.Core.Models;
using BaseLib.Patches.UI;
using yyl_sts2_mod.Code.Extensions;
using yyl_sts2_mod.Code.Relics;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     遗物图标注册（BaseLib 官方接管接口）。
///     <para>
///         为什么需要显式注册：引擎 <c>RelicModel</c> 里
///         <c>PackedIconPath</c> / <c>PackedIconOutlinePath</c> 原版指向的是
///         <b>atlases/relic_atlas.sprites/{name}.tres 图集精灵</b>，只有 <c>BigIconPath</c> 才是 png。
///         自定义遗物没有图集条目，所以必须用
///         <see cref="RelicImageOverridePatch.AddOverride{TRelicType}(RelicIconData, Func{RelicModel, bool})" />
///         把三条路径一并接管，否则小图取不到可用纹理 → UI 回退成 NOPE 占位符（大图不需要轮廓图，所以一直正常）。
///     </para>
///     <para>
///         BaseLib 的补丁是 <b>HarmonyPrefix</b>：注册过才会接管（返回 false 跳过原版），
///         未注册则返回 true 继续执行原版 —— 因此 <c>yyl_sts2_modRelic</c> 里的同名覆写仍然保留，
///         作为注册失败时的兜底。
///     </para>
/// </summary>
public static class RelicIcons
{
    public static void Register()
    {
        Register<PeachCan>("peach_can");
        Register<NlCan2>("nl_can2");
        Register<DongNiZhiBa>("dong_ni_zhi_ba");
        Register<NailongRearing>("nailong_rearing");
        Register<SnakeFlowerMiss>("snake_flower_miss");
    }

    /// <summary>
    ///     为单个遗物注册三张图：大图 / 小图（packed）/ 轮廓图。
    ///     目录约定见 <c>StringExtensions</c>：relics/big、relics、relics/outline。
    /// </summary>
    private static void Register<TRelic>(string file) where TRelic : RelicModel
    {
        try
        {
            var data = new RelicIconData(
                BigIconPath: (file + ".png").BigRelicImagePath(),
                PackedIconPath: (file + ".png").RelicImagePath(),
                PackedIconOutlinePath: (file + "_outline.png").RelicOutlineImagePath());

            RelicImageOverridePatch.AddOverride<TRelic>(data);
            MainFile.Logger.Info(
                $"RelicIcons: <{typeof(TRelic).Name}> -> big/big-relic-path registered ({file}.png)");
        }
        catch (Exception e)
        {
            // 单个遗物注册失败不应拖垮整个 mod 初始化。
            MainFile.Logger.Error($"RelicIcons: <{typeof(TRelic).Name}> 注册失败。\n{e}");
        }
    }
}
