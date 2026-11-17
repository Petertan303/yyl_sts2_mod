using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Interop;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Events;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "yyl_sts2_mod"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        CheckGameVersion();

        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        yylSubscriber.Subscribe();
        var assembly = Assembly.GetExecutingAssembly();

        /*  RitsuLib 自动注册: 扫描本程序集里的 [RegisterActEvent] / [RegisterCard] 等特性。
            没有这一句, 事件「“蛇花”？」之类的内容根本不会被注册进游戏。
            单独 try/catch —— RitsuLib 没装/版本不符时, 只丢事件功能, 不让整个 mod 挂掉。 */
        try
        {
            ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
            Logger.Info("RitsuLib auto-registration: OK.");
        }
        catch (Exception ex)
        {
            Logger.Error($"RitsuLib auto-registration FAILED (事件等内容将不可用)。\n{ex}");
        }

        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        Harmony harmony = new(ModId);

        /*  不用 harmony.PatchAll(): 它一旦遇到任何一个失败的补丁类 (例如游戏更新后
            某个方法从 Task 改成 void) 就会抛异常, 导致整个 mod 初始化失败、全部内容失效。
            这里改成逐类应用 + 单独 try/catch —— 单个补丁挂掉只记错误, 其余功能照常。 */
        var patchTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
            .Where(t => t != typeof(ModelDbInitIdsPatch)) // 这个下面单独应用, 避免打两遍
            .OrderBy(t => t.Name)
            .ToList();
        foreach (var patchType in patchTypes)
            ApplyPatch(harmony, patchType);

        ApplyPatch(harmony, typeof(ModelDbInitIdsPatch));

        // Keep the shop (merchant) character working when RitsuLib is installed.
        YylMerchantCharacterPatch.TryApply(harmony);

        /*  遗物图标: 原版小图/轮廓图是 .tres 图集精灵, 自定义遗物不在图集里,
            必须用 BaseLib 的 RelicImageOverridePatch 显式注册三条路径, 否则小图显示 NOPE 占位符
            (大图本来就是 png, 所以一直正常)。放在 Initialize() 末尾 —— 此时 mod 的 pck 已加载,
            ResourceLoader.Exists 才能正确判断文件是否存在。 */
        RelicIcons.Register();
    }
    
    
    private static void ApplyPatch(Harmony harmony, Type patchClass)
    {
        try
        {
            var patched = harmony.CreateClassProcessor(patchClass).Patch();
            if (patched == null || patched.Count == 0)
                Logger.Error($"{patchClass.Name}: applied but patched ZERO methods (TargetMethod returned null?).");
            else
                Logger.Info($"{patchClass.Name}: OK ({patched.Count} method(s)).");
        }
        catch (Exception ex)
        {
            Logger.Error($"{patchClass.Name}: FAILED to apply.\n{ex}");
        }
    }

    /*  ★游戏版本守卫 (2026-11-15 迁移到 v0.113.1): v0.113.0 把血条/交互 API 整组重命名
        (ToggleIsInteractable → SetIsInteractable、IsHealthBarVisible → IsInteractable),
        AfterDeath 增加 CardModel 参数, 引擎 SingletonModel 被删除、BaseLib 基类随之改为 AbstractModel;
        v0.113.1 又把 AttackCommand.Execute 的伤害结算改走
        CreatureCmd.Damage(IEnumerable<DamageTargetData>, ...) 新重载 (太极重定向补丁已同步覆盖)。
        版本不符时旧 API 解析失败会让战斗房间创建中断 (角色站位错乱、敌人不显示、进战斗卡死)。
        启动时读游戏根目录 release_info.json, 版本不符就打醒目错误。 */
    private const string SupportedGameVersion = "v0.113.1";

    private static void CheckGameVersion()
    {
        try
        {
            using var f = Godot.FileAccess.Open("res://release_info.json", Godot.FileAccess.ModeFlags.Read);
            if (f == null)
            {
                Logger.Warn("[版本守卫] 无法读取 release_info.json (跳过游戏版本检查)。");
                return;
            }

            var parsed = Json.ParseString(f.GetAsText());
            var ver = parsed.AsGodotDictionary()["version"].AsString();

            if (string.Equals(ver, SupportedGameVersion, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info($"[版本守卫] 游戏版本 {ver} = 本 mod 开发版本, OK。");
                return;
            }

            Logger.Error(
                $"\n========================================\n" +
                $"[版本守卫] ★游戏版本不匹配! 当前游戏 = {ver}, 本 mod 基于 {SupportedGameVersion} 开发。\n" +
                $"[版本守卫] 版本不符时可能出现: 战斗房间创建中断 (角色站位错乱、敌人不显示) 等恶性问题。\n" +
                $"[版本守卫] 请把游戏切换到 {SupportedGameVersion}。\n" +
                $"========================================");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[版本守卫] 检查游戏版本时出错 (忽略, 不影响加载): {ex.Message}");
        }
    }
}