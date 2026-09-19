using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Events;

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
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        yylSubscriber.Subscribe();
        var assembly = Assembly.GetExecutingAssembly();
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
}