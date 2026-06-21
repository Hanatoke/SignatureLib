using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using SignatureLib.SignatureLibCode.Core;
using SignatureLib.SignatureLibCode.Interface;

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
        Logger.Info("start add signature");
        foreach (var card in ModelDb.AllCards)
        {
            if (card is ISignatureCard { ShouldAutoAddSignature: true } signatureCard)
            {
                card.AddSignatures(signatureCard.SignatureInfos);
                
                if (signatureCard.AutoEnabledSignature != null && card.GetCurrentSignature() == null &&
                    card.GetSignatureById(signatureCard.AutoEnabledSignature) is { } info)
                {
                    card.SetCurrentSignature(info,false);
                    card.SetSignatureEnable(true,false);
                }
            }
            foreach (var func in SignatureManager.SignatureInfoProvider)
            {
                var infos = func?.Invoke(card);
                if (infos == null) continue;
                card.AddSignatures(infos);
            }
            foreach (var func in SignatureManager.SignatureSetProvider)
            {
                var e = func?.Invoke(card);
                if (e==null)continue;
                foreach (var s in e)
                {
                    if (s is { Item1: not null })
                    {
                        card.AddSignature(new SignatureInfo()
                        {
                            Id = s.Item1,
                            Img =  s.Item2,
                            Scale = s.Item3,
                            Name = s.Item4,
                            Description = s.Item5,
                        });
                    }
                }
            }
            if (card.HasSignature())
            {
                Logger.Info($"Added signature {card.Id} Finish");
            }
        }
        SignatureManager.LoadSignatureSetting();
    }
}
