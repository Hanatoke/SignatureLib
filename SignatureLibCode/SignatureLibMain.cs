using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode;

[ModInitializer(nameof(Initialize))]
public partial class SignatureLibMain : Godot.Node
{
    public const string ModId = "SignatureLib"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(SignatureLibMain).Assembly);
    }

    public static void AfterGameInit(Harmony harmony)
    {
        SignatureManager.Init();
    }
}
