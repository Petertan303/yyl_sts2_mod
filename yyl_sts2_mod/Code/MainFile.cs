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
        

        harmony.PatchAll();
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