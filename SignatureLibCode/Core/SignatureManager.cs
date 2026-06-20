using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace SignatureLib.SignatureLibCode.Core;

public static class SignatureManager
{
    public const string SaveFileName = "SignatureSettings.save";
    public static readonly List<Func<CardModel,IEnumerable<(string,string,Vector2,Func<LocString>,Func<LocString>)>>> SignatureSetProvider = [];
    public static readonly List<Func<CardModel,IEnumerable<SignatureInfo>>> SignatureInfoProvider = [];
    public static readonly Dictionary<ModelId,List<SignatureInfo>> SignatureInfos = new();
    public static readonly Dictionary<ModelId,SignatureInfo> CurrentSignature = new();
    public static Dictionary<ModelId,bool> SignatureEnable { get; internal set; }= new();
    public static SignatureSave Setting{get; set;} = new();
    public static bool HasSignature(this CardModel card)=> SignatureInfos.ContainsKey(card.Id)&& SignatureInfos[card.Id].Count>0;
    public static bool IsEnableSignature(this CardModel card)=> SignatureEnable.GetValueOrDefault(card.Id,false);
    public static SignatureInfo GetCurrentSignature(this CardModel card)=>CurrentSignature.GetValueOrDefault(card.Id,null);

    public static IEnumerable<SignatureInfo> GetSignatureInfos(this CardModel card)
    {
        return GetSignatureInfos(card.Id);
    }
    public static IEnumerable<SignatureInfo> GetSignatureInfos(ModelId id)
    {
        return SignatureInfos.GetValueOrDefault(id,[]);
    }
    public static void AddSignature(this CardModel card, SignatureInfo signatureInfo)
    {
        if (card.GetSignatureInfos().Any(s=>s.Id==signatureInfo.Id))
        {
            SignatureLibMain.Logger.Warn("Add signature failed: same id already exists! cardId:" + card.Id+"signatureInfo.Id:" + signatureInfo.Id);
            return;
        }
        if (ResourceLoader.Exists(signatureInfo.Img))
        {
            if (!SignatureInfos.ContainsKey(card.Id)) SignatureInfos[card.Id] = [];
            SignatureInfos[card.Id].Add(signatureInfo);
            SignatureLibMain.Logger.Info("success add signature :" + signatureInfo.Id);
        }
    }

    public static void AddSignatures(this CardModel card, IEnumerable<SignatureInfo> signatures)
    {
        foreach (var signature in signatures)
        {
            AddSignature(card, signature);
        }
    }
    public static void SetSignatureEnable(this CardModel card, bool enable)
    {
        SignatureEnable[card.Id] = enable;
        if (card.GetCurrentSignature()==null)
        {
            card.SetCurrentSignature(GetDefaultSignatureInfo(card.Id));
        }
        SaveSignatureSetting();
    }

    public static void SetCurrentSignature(this CardModel card, SignatureInfo info)
    {
        CurrentSignature[card.Id]=info;
        SaveSignatureSetting();
    }

    internal static SignatureInfo GetDefaultSignatureInfo(ModelId id) =>
        GetSignatureInfos(id).FirstOrDefault((SignatureInfo)null);

    public static void RegisterSignatureSetsProvider(Func<CardModel, IEnumerable<(string,string,Vector2,Func<LocString>,Func<LocString>)>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        SignatureSetProvider.Add(func);
    }
    public static void RegisterSignatureInfosProvider(Func<CardModel, IEnumerable<SignatureInfo>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        SignatureInfoProvider.Add(func);
    }

    public static void LoadSignatureSetting()
    {
        try
        {
            var saveStore = AccessTools.Field(typeof(SaveManager),"_saveStore").GetValue(SaveManager.Instance) as ISaveStore;
            if (saveStore == null)return;
            if (saveStore.FileExists(SaveFileName)&&saveStore.ReadFile(SaveFileName) is {} s)
            {
                Setting=JsonSerializer.Deserialize<SignatureSave>(s);
            }
            Setting ??= new SignatureSave();
            foreach (var keyValuePair in Setting.SignatureCurrentSelect)
            {
                ModelId modelId = ModelId.Deserialize(keyValuePair.Key);
                if (GetSignatureInfos(modelId).FirstOrDefault(info => info.Id == keyValuePair.Value,null) is { } signatureInfo)
                {
                    CurrentSignature[modelId] = signatureInfo;
                }
            }
            foreach (var keyValuePair in Setting.SignatureEnableSetting)
            {
                ModelId modelId = ModelId.Deserialize(keyValuePair.Key);
                if (!(SignatureInfos.ContainsKey(modelId) && SignatureInfos[modelId].Count > 0)) continue;
                SignatureEnable[modelId] = keyValuePair.Value;
            }
            foreach (var keyValuePair in SignatureEnable)
            {
                if (CurrentSignature.ContainsKey(keyValuePair.Key))continue;
                var info = GetDefaultSignatureInfo(keyValuePair.Key);
                if (info == null) continue;
                CurrentSignature[keyValuePair.Key] = info;
                Setting.SignatureCurrentSelect[keyValuePair.Key.ToString()] = info.Id;
            }
        }
        catch (Exception e)
        {
            SignatureLibMain.Logger.Info(e.ToString());
        }
    }

    public static void SaveSignatureSetting()
    {
        try
        {
            var saveStore = AccessTools.Field(typeof(SaveManager),"_saveStore").GetValue(SaveManager.Instance) as ISaveStore;
            Setting.SignatureEnableSetting = SignatureEnable.Select(k => (k.Key.ToString(), k.Value)).ToDictionary();
            Setting.SignatureCurrentSelect =
                CurrentSignature.Select(k => (k.Key.ToString(), k.Value.Id)).ToDictionary();
            saveStore?.WriteFile(SaveFileName,JsonSerializer.Serialize(Setting));
        }
        catch (Exception e)
        {
            SignatureLibMain.Logger.Info(e.ToString());
        }
    }
    public class SignatureSave 
    {
        [JsonPropertyName("signature_enable_setting")]
        public Dictionary<string, bool> SignatureEnableSetting { get; set; } = new();
        [JsonPropertyName("signature_current_select")]
        public Dictionary<string, string> SignatureCurrentSelect { get; set; } = new();
        
    }
}