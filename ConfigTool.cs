using System;
using System.Reflection;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
using HarmonyLib;

namespace RealEco.Config;

[HarmonyPatch]
public static class ConfigTool_Patches
{
    public static void DumpFields(PrefabBase prefab, ComponentBase component)
    {
        string className = component.GetType().Name;
        Plugin.Log($"{prefab.name}.{component.name}.CLASS: {className}");

        object obj = (object)component;
        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // field components: System.Collections.Generic.List`1[Game.Prefabs.ComponentBase]
            if (field.Name != "isDirty" && field.Name != "active" && field.Name != "components")
            {
                object value = field.GetValue(obj);
                Plugin.Log($"{prefab.name}.{component.name}.{field.Name}: {value}");
            }
        }
    }

    /// <summary>
    /// Configures a specific component withing a specific prefab according to config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    /// <param name="comp"></param>
    private static void ConfigureComponent(PrefabBase prefab, PrefabXml prefabConfig, ComponentBase component)
    {
        string compName = component.GetType().Name;

        // Structs within components are handled as separate components
        if (compName == "ProcessingCompany" && prefabConfig.TryGetComponent("IndustrialProcess", out ComponentXml structConfig))
        {
            // IndustrialProcess - currently 2 fields are supported
            ProcessingCompany comp = component as ProcessingCompany;
            IndustrialProcess oldProc = comp.process;
            if (structConfig.TryGetField("m_MaxWorkersPerCell", out FieldXml mwpcField) && mwpcField.ValueFloatSpecified)
            {
                comp.process.m_MaxWorkersPerCell = mwpcField.ValueFloat ?? oldProc.m_MaxWorkersPerCell;
                Plugin.LogIf($"{prefab.name}.IndustrialProcess.{mwpcField.Name}: {oldProc.m_MaxWorkersPerCell} -> {comp.process.m_MaxWorkersPerCell} ({comp.process.m_MaxWorkersPerCell.GetType()}, {mwpcField})");
            }
            if (structConfig.TryGetField("m_Output.m_Amount", out FieldXml outamtField) && outamtField.ValueIntSpecified)
            {
                comp.process.m_Output.m_Amount = outamtField.ValueInt ?? oldProc.m_Output.m_Amount;
                Plugin.LogIf($"{prefab.name}.IndustrialProcess.{outamtField.Name}: {oldProc.m_Output.m_Amount} -> {comp.process.m_Output.m_Amount} ({comp.process.m_Output.m_Amount.GetType()}, {outamtField})");
            }
            if (!Plugin.Logging.Value)
                Plugin.LogIf($"{prefab.name}.IndustrialProcess: wpc {comp.process.m_MaxWorkersPerCell} output {comp.process.m_Output.m_Amount}");
        }

        if (!prefabConfig.TryGetComponent(compName, out ComponentXml compConfig))
        {
            //Plugin.LogIf($"{prefab.name}.{compName}: valid");
            //ConfigureComponent(prefab, prefabConfig, component, compConfig);
            //}
            //else
            Plugin.LogIf($"{prefab.name}.{compName}: SKIP");
            return;
        }

        Plugin.LogIf($"{prefab.name}.{compName}: valid");
        foreach (FieldXml fieldConfig in compConfig.Fields)
        {
            // Get the FieldInfo object for the field with the given name
            FieldInfo field = component.GetType().GetField(fieldConfig.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                object oldValue = field.GetValue(component);

                // TODO: extend for other field types
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(component, fieldConfig.ValueFloat);
                }
                else
                {
                    field.SetValue(component, fieldConfig.ValueInt);
                }
                if (Plugin.Logging.Value)
                    Plugin.Log($"{prefab.name}.{compName}.{field.Name}: {oldValue} -> {field.GetValue(component)} ({field.FieldType}, {fieldConfig})");
                else
                    Plugin.Log($"{prefab.name}.{compName}.{field.Name}: {field.GetValue(component)}");
            }
            else
            {
                Plugin.Log($"{prefab.name}.{compName}: Warning! Field {fieldConfig.Name} not found in the component.");
            }
        }
        if (Plugin.Logging.Value) DumpFields(prefab, component); // debug
    }

    /// <summary>
    /// Configures a specific prefab according to the config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    private static void ConfigurePrefab(PrefabBase prefab, PrefabXml prefabConfig)
    {
        Plugin.LogIf($"{prefab.name}: valid {prefab.GetType().Name}");
        // check first if the main prefab needs to be changed
        ConfigureComponent(prefab, prefabConfig, prefab);
        // iterate through components and see which ones need to be changed
        foreach (ComponentBase component in prefab.components)
        {
            //string compName = component.GetType().Name;
            //if (prefabConfig.TryGetComponent(compName, out ComponentXml compConfig))
            //{
                //Plugin.LogIf($"{prefab.name}.{compName}: valid");
                ConfigureComponent(prefab, prefabConfig, component);
            //}
            //else
                //Plugin.LogIf($"{prefab.name}.{compName}: SKIP");
        }
    }

    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool PrefabSystem_AddPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        if (ConfigToolXml.Config.IsPrefabValid(prefab.GetType().Name))
        {
            if (ConfigToolXml.Config.TryGetPrefab(prefab.name, out PrefabXml prefabConfig))
            {
                ConfigurePrefab(prefab, prefabConfig);
            }
            else
                Plugin.LogIf($"{prefab.name}: SKIP {prefab.GetType().Name}");
        }
        return true;
    }

    // Part 1: This is called 1035 times
    /*
    [HarmonyPatch(typeof(Game.Prefabs.AssetCollection), "AddPrefabsTo")]
    [HarmonyPostfix]
    public static void AddPrefabsTo_Postfix()
    {
        Plugin.Log("**************************** Game.Prefabs.AssetCollection.AddPrefabsTo");
    }
    */

    // Part 2: This is called 1 time
    /*
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "LoadPrefabs")]
    [HarmonyPostfix]
    public static void LoadPrefabs_Postfix()
    {
        Plugin.Log("**************************** Game.SceneFlow.GameManager.LoadPrefabs");
    }
    */

    // Part 3: This is called 1 time
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "OnUpdate")]
    [HarmonyPostfix]
    public static void OnUpdate_Postfix()
    {
        //Plugin.Log("**************************** Game.Prefabs.PrefabInitializeSystem.OnUpdate");
        if (Plugin.ConfigDump.Value) ConfigToolXml.SaveConfig();
    }
}
