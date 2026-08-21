using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

#pragma warning disable CS1591

namespace SignatureLib.SignatureLibCode.Core;

public class ModConfig
{
    public static readonly AssemblyName AssemblyName=new(typeof(ModConfig).Assembly.GetName().Name+".ModConfig");
    public static void Init()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "BaseLib");
        if (asm != null)
        {
            SignatureLibMain.Logger.Info("Find BaseLib assembly , start register config");
            try
            {
                var baseType = GetBaseType(asm);
                var configType = CreateConfigType(asm,baseType);
                Register(asm,SignatureLibMain.ModId, configType);
                SignatureLibMain.Logger.Info("Success add BaseLib config");
            }
            catch (Exception e)
            {
                SignatureLibMain.Logger.Info("Failed to register BaseLib config");
            }
        }
    }

    public static Type GetBaseType(Assembly asm) => asm.GetType("BaseLib.Config.SimpleModConfig");

    public static void Register(Assembly asm, string id, Type configType)
    {
        var type = asm.GetType("BaseLib.Config.ModConfigRegistry");
        type?.GetMethod("Register")?.Invoke(null, [id,Activator.CreateInstance(configType)]);
    }

    public static CustomAttributeBuilder GetCustomAttributeBuilder(Assembly asm, string className, Type[] paramsTypes,
        object[] args, string[] propertyNames, object[] propertyValues, string[] fieldNames, object[] fieldValues)
    {
        var type = asm.GetType(className);
        if (type == null) return null;
        var ctor = type.GetConstructor(paramsTypes);
        if (ctor == null) return null;
        var propertyInfos = propertyNames.Select(s=>type.GetProperty(s)).ToArray();
        var fieldInfos = fieldNames.Select(s=>type.GetField(s)).ToArray();
        return new CustomAttributeBuilder(ctor, args,propertyInfos,propertyValues,fieldInfos,fieldValues);
    }
    public static CustomAttributeBuilder GetCustomAttributeBuilder(Assembly asm, string className, Type[] paramsTypes,
        params object[] args)
    {
        var type = asm.GetType(className)?.GetConstructor(paramsTypes);
        return type == null ? null : new CustomAttributeBuilder(type, args);
    }

    public static CustomAttributeBuilder ConfigHoverTipsByDefault(Assembly asm)
    {
        return GetCustomAttributeBuilder(asm, "BaseLib.Config.ConfigHoverTipsByDefaultAttribute", []);
    }

    public static CustomAttributeBuilder ConfigButton(Assembly asm,string buttonName,Color? color=null)
    {
        if (color == null)
        {
            return  GetCustomAttributeBuilder(asm, "BaseLib.Config.ConfigButtonAttribute", [typeof(string)],buttonName);
        }
        return GetCustomAttributeBuilder(asm, "BaseLib.Config.ConfigButtonAttribute", [typeof(string)], [buttonName],
            ["Color"], [color.Value.ToHtml()], [], []);
    }

    public static CustomAttributeBuilder ConfigSlider(Assembly asm, double min, double max, double step,
        string format = null)
    {
        return GetCustomAttributeBuilder(asm, "BaseLib.Config.ConfigSliderAttribute",
            [typeof(double), typeof(double), typeof(double)], [min, max, step],
            ["Format"], [format], [], []);
    }

    public static MethodBuilder CreateButton(Assembly asm,TypeBuilder typeBuilder,string methodName,string buttonName,Color? color=null)
    {
        var methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static,
            typeof(void), Type.EmptyTypes);
        ILGenerator generator = methodBuilder.GetILGenerator();
        var info = AccessTools.Method(typeof(ModConfig),methodName);
        if (info!=null)
        {
            generator.Emit(OpCodes.Call,info);
        }
        generator.Emit(OpCodes.Ret);
        if (ConfigButton(asm,buttonName, color) is { } configButton)
        {
            methodBuilder.SetCustomAttribute(configButton);
        }
        return methodBuilder;
    }

    public static PropertyBuilder CreateProperty(Assembly asm, TypeBuilder typeBuilder, string propertyName,Type propertyType,MethodInfo getter=null, MethodInfo setter=null)
    {
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
        if (getter != null)
        {
            var getterMethod = typeBuilder.DefineMethod("get_" + propertyName,
                MethodAttributes.Public|MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType,
                Type.EmptyTypes);
            var generator = getterMethod.GetILGenerator();
            generator.Emit(OpCodes.Call, getter);
            generator.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getterMethod);
        }

        if (setter != null)
        {
            var setterMethod = typeBuilder.DefineMethod("set_" + propertyName,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null,
                [propertyType]);
            var generator = setterMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, setter);
            generator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setterMethod);
        }
        return propertyBuilder;
    }
    public static Type CreateConfigType(Assembly asm, Type baseType)
    {
        AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = builder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType("SignatureLib.SignatureConfig", TypeAttributes.Public, baseType);
        if (ConfigHoverTipsByDefault(asm) is { } hoverTips)
        {
            typeBuilder.SetCustomAttribute(hoverTips);
        }

        CreateProperty(asm, typeBuilder, "AlwaysShowDescription", typeof(bool),
            AccessTools.Method(typeof(ModConfig), nameof(AlwaysShowDescriptionGetter)),
            AccessTools.Method(typeof(ModConfig), nameof(AlwaysShowDescriptionSetter)));
        CreateProperty(asm, typeBuilder, "DescriptionShadowFadeDuration", typeof(float),
            AccessTools.Method(typeof(ModConfig), nameof(DescriptionShadowFadeDurationGetter)),
            AccessTools.Method(typeof(ModConfig), nameof(DescriptionShadowFadeDurationSetter)))
            .SetCustomAttribute(ConfigSlider(asm,0,2,0.01,"{0:P0}"));
        CreateProperty(asm, typeBuilder, "DescriptionShadowAlpha", typeof(float),
            AccessTools.Method(typeof(ModConfig), nameof(DescriptionShadowAlphaGetter)),
            AccessTools.Method(typeof(ModConfig), nameof(DescriptionShadowAlphaSetter)))
            .SetCustomAttribute(ConfigSlider(asm,0,1,0.01,"{0:P0}"));
        CreateButton(asm,typeBuilder,nameof(ForceEnableAllSignature),nameof(ForceEnableAllSignature)+"Button");
        CreateButton(asm,typeBuilder,nameof(ForceDisableAllSignature),nameof(ForceDisableAllSignature)+"Button",Colors.DarkRed);
        return typeBuilder.CreateType();
    }

    public static bool AlwaysShowDescriptionGetter() => SignatureManager.AlwaysShowDescription;

    public static void AlwaysShowDescriptionSetter(bool f)=>SignatureManager.AlwaysShowDescription = f;
    public static float DescriptionShadowFadeDurationGetter()=>SignatureManager.DescriptionShadowFadeDuration;
    public static void DescriptionShadowFadeDurationSetter(float f)=>SignatureManager.DescriptionShadowFadeDuration=f;
    public static float DescriptionShadowAlphaGetter()=>SignatureManager.DescriptionShadowAlpha;
    public static void DescriptionShadowAlphaSetter(float f)=>SignatureManager.DescriptionShadowAlpha=f;

    public static void ForceEnableAllSignature()
    {
        foreach (var keyValuePair in SignatureManager.SignatureInfos.ToList())
        {
            var id = keyValuePair.Key;
            SignatureManager.SignatureEnable[id] = true;
            if (SignatureManager.CurrentSignature.GetValueOrDefault(id, null) == null &&
                SignatureManager.GetDefaultSignatureInfo(id) is { } info)
            {
                SignatureManager.CurrentSignature[id] = info;
            }
        }
        SignatureManager.SaveSignatureSetting();
        SignatureLibMain.Logger.Info("Force Enable All Signature");
        try
        {
            if (Engine.GetMainLoop() is SceneTree tree)
            {
                foreach (var card in tree.Root.GetChildrenRecursive<NCard>())
                {
                    if (card.Model != null && card.Model.HasSignature()) 
                    {
                        card.ShowSignature();
                    }
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void ForceDisableAllSignature()
    {
        foreach (var keyValuePair in SignatureManager.SignatureEnable.ToList())
        {
            SignatureManager.SignatureEnable[keyValuePair.Key] = false;
        }
        SignatureManager.SaveSignatureSetting();
        SignatureLibMain.Logger.Info("Force Disable All Signature");
        try
        {
            if (Engine.GetMainLoop() is SceneTree tree)
            {
                foreach (var card in tree.Root.GetChildrenRecursive<NCard>())
                {
                    if (card.Model != null && card.Model.HasSignature()) 
                    {
                        card.HideSignature();
                    }
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}