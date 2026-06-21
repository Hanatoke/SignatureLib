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
/// <summary>
/// The manager for all the data of Signature
/// </summary>
public static class SignatureManager
{
    /// <summary>
    /// The SaveFile Name for Setting
    /// </summary>
    public const string SaveFileName = "SignatureSettings.save";
    /// <summary>
    /// The Collection for Signature Provider by Set
    /// </summary>
    public static readonly List<Func<CardModel,IEnumerable<(string,string,Vector2,Func<LocString>,Func<LocString>)>>> SignatureSetProvider = [];
    /// <summary>
    /// The Collection for Signature Provider by SignatureInfo
    /// </summary>
    public static readonly List<Func<CardModel,IEnumerable<SignatureInfo>>> SignatureInfoProvider = [];
    /// <summary>
    /// All Signature Map
    /// </summary>
    public static readonly Dictionary<ModelId,List<SignatureInfo>> SignatureInfos = new();
    /// <summary>
    /// Current Signature Map
    /// </summary>
    public static readonly Dictionary<ModelId,SignatureInfo> CurrentSignature = new();
    /// <summary>
    /// Enabled Signature Map
    /// </summary>
    public static Dictionary<ModelId,bool> SignatureEnable { get; }= new();
    internal static SignatureSave Setting{get; set;} = new();
    /// <summary>
    /// Return the Card has any Signature
    /// </summary>
    public static bool HasSignature(this CardModel card)=> SignatureInfos.ContainsKey(card.Id)&& SignatureInfos[card.Id].Count>0;
    /// <summary>
    /// Return true if Card is enabled Signature 
    /// </summary>
    public static bool IsEnableSignature(this CardModel card)=> SignatureEnable.GetValueOrDefault(card.Id,false);
    /// <summary>
    /// Return the Card current enabled Signature
    /// </summary>
    /// <returns>null if no enabled</returns>
    public static SignatureInfo? GetCurrentSignature(this CardModel card)=>CurrentSignature.GetValueOrDefault(card.Id,null);
    /// <summary>
    /// Get the Signature if same id exist
    /// </summary>
    /// <returns>Return null if no exist</returns>
    public static SignatureInfo? GetSignatureById(ModelId modelId, string signatureId)
    {
        if (signatureId == null) return null;
        return GetSignatureInfos(modelId).FirstOrDefault(info => info.Id == signatureId, null);
    }

    /// <summary>
    /// Get the Signature if same id exist
    /// </summary>
    /// <returns>Return null if no exist</returns>
    public static SignatureInfo? GetSignatureById(this CardModel card, string signatureId)=>GetSignatureById(card.Id, signatureId);
    /// <summary>
    /// Get All Signature for the card
    /// </summary>
    /// <returns>return empty list if no any Signature</returns>
    public static IEnumerable<SignatureInfo> GetSignatureInfos(this CardModel card)
    {
        return GetSignatureInfos(card.Id);
    }
    /// <summary>
    /// Get All Signature for the id
    /// </summary>
    /// <returns>return empty list if no any Signature</returns>
    public static IEnumerable<SignatureInfo> GetSignatureInfos(ModelId id)
    {
        return SignatureInfos.GetValueOrDefault(id,[]);
    }
    /// <summary>
    /// Add a Signature to the card.
    /// <param name="signatureInfo">Must have a unique id and valid path</param>
    /// </summary>
    public static void AddSignature(this CardModel card, SignatureInfo signatureInfo)
    {
        if (signatureInfo == null || string.IsNullOrEmpty(signatureInfo.Id) || string.IsNullOrEmpty(signatureInfo.Img)) return;
        if (card.GetSignatureInfos().Any(s=>s.Id==signatureInfo.Id))
        {
            SignatureLibMain.Logger.Warn("Add signature failed: same id already exists! cardId:" + card.Id+"signatureInfo.Id:" + signatureInfo.Id);
            return;
        }
        if (!signatureInfo.VerifyImgPath(signatureInfo.Img))
        {
            SignatureLibMain.Logger.Info("Not found Signature image path, skipped:" + signatureInfo.Img);
            return;
        }
        if (!SignatureInfos.ContainsKey(card.Id)) SignatureInfos[card.Id] = [];
        SignatureInfos[card.Id].Add(signatureInfo);
        SignatureLibMain.Logger.Info("success add signature ID:" + signatureInfo.Id);
    }
    /// <summary>
    /// Add many Signatures to the card.
    /// <seealso cref="AddSignature"/>
    /// </summary>
    public static void AddSignatures(this CardModel card, IEnumerable<SignatureInfo> signatures)
    {
        foreach (var signature in signatures)
        {
            AddSignature(card, signature);
        }
    }
    /// <summary>
    /// Set whether to enable Signature for the card
    /// <remarks>Only modify usage data</remarks>
    /// <remarks><seealso cref="NCardHelper.ShowSignature"/><seealso cref="NCardHelper.HideSignature"/></remarks>
    /// </summary>
    public static void SetSignatureEnable(this CardModel card, bool enable,bool saveSetting=true)
    {
        SignatureEnable[card.Id] = enable;
        if (card.GetCurrentSignature()==null && GetDefaultSignatureInfo(card.Id) is {} info)
        {
            card.SetCurrentSignature(info);
        }

        if (!saveSetting) return;
        SaveSignatureSetting();
    }
    /// <summary>
    /// Set the currently used Signature for the card.
    /// <remarks>Only modify usage data</remarks>
    /// <remarks><seealso cref="NCardHelper.ShowSignature"/><seealso cref="NCardHelper.HideSignature"/></remarks>
    /// </summary>
    public static void SetCurrentSignature(this CardModel card, SignatureInfo info,bool saveSetting=true)
    {
        CurrentSignature[card.Id]=info;
        if (!saveSetting) return;
        SaveSignatureSetting();
    }

    internal static SignatureInfo? GetDefaultSignatureInfo(ModelId id) =>
        GetSignatureInfos(id).FirstOrDefault((SignatureInfo)null);
    /// <summary>
    /// Register Signature Provider.
    /// <code>
    ///RegisterSignatureSetsProvider(card =>
    ///{
    ///    if (card is YourAbstractClass)
    ///    {
    ///        return [(ID1,ImgPath1,Scale1,Name1?,Desc1?),(ID2,ImgPath2,Scale2,Name2?,Desc2?)......]
    ///    }
    ///    return [];
    ///});
    /// </code>
    /// </summary>
    public static void RegisterSignatureSetsProvider(Func<CardModel, IEnumerable<(string,string,Vector2,Func<LocString>,Func<LocString>)>> func)
    {
        
        ArgumentNullException.ThrowIfNull(func);
        SignatureSetProvider.Add(func);
    }
    /// <summary>
    /// Register Signature Provider.
    /// <seealso cref="SignatureInfo"/>
    /// <code>
    /// RegisterSignatureInfosProvider(card =>
    ///{
    ///    if (card is YourAbstractClass)
    ///    {
    ///        return [SignatureInfo1,SignatureInfo2......]
    ///    }
    ///    return [];
    ///});
    /// </code>
    /// </summary>
    public static void RegisterSignatureInfosProvider(Func<CardModel, IEnumerable<SignatureInfo>> func)
    {
        
        ArgumentNullException.ThrowIfNull(func);
        SignatureInfoProvider.Add(func);
    }
    /// <summary>
    /// Load the Signature settings immediately
    /// <remarks>You may not need this</remarks>
    /// </summary>

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
                if (GetSignatureById(modelId,keyValuePair.Value) is { } signatureInfo)
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
            //为已启用但还没有选择的卡牌,选择默认异画
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
    /// <summary>
    /// Save the Signature settings immediately
    /// <remarks>You may not need this</remarks>
    /// </summary>

    public static void SaveSignatureSetting()
    {
        try
        {
            var saveStore = AccessTools.Field(typeof(SaveManager),"_saveStore").GetValue(SaveManager.Instance) as ISaveStore;
            Setting.SignatureEnableSetting = SignatureEnable.Select(k => (k.Key.ToString(), k.Value)).ToDictionary();
            Setting.SignatureCurrentSelect =
                CurrentSignature.Where(k=>k.Value!=null)
                    .Select(k => (k.Key.ToString(), k.Value.Id)).ToDictionary();
            saveStore?.WriteFile(SaveFileName,JsonSerializer.Serialize(Setting));
        }
        catch (Exception e)
        {
            SignatureLibMain.Logger.Info(e.ToString());
        }
    }
    internal class SignatureSave 
    {
        [JsonPropertyName("signature_enable_setting")]
        public Dictionary<string, bool> SignatureEnableSetting { get; set; } = new();
        [JsonPropertyName("signature_current_select")]
        public Dictionary<string, string> SignatureCurrentSelect { get; set; } = new();
        
    }
}