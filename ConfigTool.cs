﻿using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.Entities;
using Game.Prefabs;
using HarmonyLib;

namespace RealEco.Config;

[HarmonyPatch]
public static class ConfigTool
{
    //public static bool isLatePrefabsActive = false; // will enable AddPrefab patch to process prefabs loaded AFTER mods are initialized (there are some)

    private static PrefabSystem m_PrefabSystem;
    private static EntityManager m_EntityManager;

    public static void DumpFields(PrefabBase prefab, ComponentBase component)
    {
        string className = component.GetType().Name;
        Mod.Log($"{prefab.name}.{component.name}.CLASS: {className}");

        object obj = (object)component;
        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // field components: System.Collections.Generic.List`1[Game.Prefabs.ComponentBase]
            if (field.Name != "isDirty" && field.Name != "active" && field.Name != "components")
            {
                object value = field.GetValue(obj);
                Mod.Log($"{prefab.name}.{component.name}.{field.Name}: {value}");
            }
        }
    }

    /// <summary>
    /// Configures a specific component withing a specific prefab according to config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    /// <param name="comp"></param>
    private static void ConfigureComponent(PrefabBase prefab, PrefabXml prefabConfig, ComponentBase component, Entity entity, bool skipEntity = false)
    {
        string compName = component.GetType().Name;
        bool isPatched = false;

        // Structs within components are handled as separate components
        // TODO: When more structs are implemented, use Reflection to create a flexible code for all possible cases
        if (compName == "ProcessingCompany" && prefabConfig.TryGetComponent("IndustrialProcess", out ComponentXml structConfig))
        {
            // IndustrialProcess - currently 4 fields are supported
            Mod.LogIf($"{prefab.name}.IndustrialProcess: valid");
            ProcessingCompany comp = component as ProcessingCompany;
            IndustrialProcess oldProc = comp.process;
            if (structConfig.TryGetField("m_MaxWorkersPerCell", out FieldXml mwpcField) && mwpcField.ValueFloatSpecified)
            {
                comp.process.m_MaxWorkersPerCell = mwpcField.ValueFloat ?? oldProc.m_MaxWorkersPerCell;
                Mod.LogIf($"{prefab.name}.IndustrialProcess.{mwpcField.Name}: {oldProc.m_MaxWorkersPerCell} -> {comp.process.m_MaxWorkersPerCell} ({comp.process.m_MaxWorkersPerCell.GetType()}, {mwpcField})");
            }
            if (structConfig.TryGetField("m_Output.m_Amount", out FieldXml outamtField) && outamtField.ValueIntSpecified)
            {
                comp.process.m_Output.m_Amount = outamtField.ValueInt ?? oldProc.m_Output.m_Amount;
                Mod.LogIf($"{prefab.name}.IndustrialProcess.{outamtField.Name}: {oldProc.m_Output.m_Amount} -> {comp.process.m_Output.m_Amount} ({comp.process.m_Output.m_Amount.GetType()}, {outamtField})");
            }
            // Special case: Industrial_BioRefinery, make it use less Grain
            if (structConfig.TryGetField("m_Input1.m_Amount", out FieldXml in1amtField) && in1amtField.ValueIntSpecified)
            {
                comp.process.m_Input1.m_Amount = in1amtField.ValueInt ?? oldProc.m_Input1.m_Amount;
                Mod.LogIf($"{prefab.name}.IndustrialProcess.{in1amtField.Name}: {oldProc.m_Input1.m_Amount} -> {comp.process.m_Input1.m_Amount} ({comp.process.m_Input1.m_Amount.GetType()}, {in1amtField})");
            }
            if (structConfig.TryGetField("m_Input2.m_Amount", out FieldXml in2amtField) && in2amtField.ValueIntSpecified)
            {
                comp.process.m_Input2.m_Amount = in2amtField.ValueInt ?? oldProc.m_Input2.m_Amount;
                Mod.LogIf($"{prefab.name}.IndustrialProcess.{in2amtField.Name}: {oldProc.m_Input2.m_Amount} -> {comp.process.m_Input2.m_Amount} ({comp.process.m_Input2.m_Amount.GetType()}, {in2amtField})");
            }
            if (!Mod.setting.Logging)
                Mod.Log($"{prefab.name}.IndustrialProcess: wpc {comp.process.m_MaxWorkersPerCell} out {comp.process.m_Output.m_Amount} in1 {comp.process.m_Input1.m_Amount} in2 {comp.process.m_Input2.m_Amount}");
            isPatched = true;
        }

        // SERVICE FEES
        if (compName == "ServiceFeeParameterPrefab" && prefabConfig.TryGetComponent("FeeParameters", out ComponentXml feeConfig))
        {
            ServiceFeeParameterPrefab feePrefab = component as ServiceFeeParameterPrefab;
            if (feeConfig.TryGetField("m_ElectricityFee.m_Default", out FieldXml electricityField) && electricityField.ValueFloatSpecified)
            {
                FeeParameters oldFee = feePrefab.m_ElectricityFee;
                feePrefab.m_ElectricityFee.m_Default = electricityField.ValueFloat ?? oldFee.m_Default;
                Mod.LogIf($"{prefab.name}.FeeParameters.{electricityField.Name}: {oldFee.m_Default} -> {feePrefab.m_ElectricityFee.m_Default} ({feePrefab.m_ElectricityFee.m_Default.GetType()}, {electricityField})");
            }
            if (feeConfig.TryGetField("m_GarbageFee.m_Default", out FieldXml garbageField) && garbageField.ValueFloatSpecified)
            {
                FeeParameters oldFee = feePrefab.m_GarbageFee;
                feePrefab.m_GarbageFee.m_Default = garbageField.ValueFloat ?? oldFee.m_Default;
                Mod.LogIf($"{prefab.name}.FeeParameters.{garbageField.Name}: {oldFee.m_Default} -> {feePrefab.m_GarbageFee.m_Default} ({feePrefab.m_GarbageFee.m_Default.GetType()}, {garbageField})");
            }
            if (feeConfig.TryGetField("m_WaterFee.m_Default", out FieldXml waterField) && waterField.ValueFloatSpecified)
            {
                FeeParameters oldFee = feePrefab.m_WaterFee;
                feePrefab.m_WaterFee.m_Default = waterField.ValueFloat ?? oldFee.m_Default;
                Mod.LogIf($"{prefab.name}.FeeParameters.{waterField.Name}: {oldFee.m_Default} -> {feePrefab.m_WaterFee.m_Default} ({feePrefab.m_WaterFee.m_Default.GetType()}, {waterField})");
            }
            if (!Mod.setting.Logging)
                Mod.Log($"{prefab.name}.FeeParameters: electricity {feePrefab.m_ElectricityFee.m_Default} garbage {feePrefab.m_GarbageFee.m_Default} water {feePrefab.m_WaterFee.m_Default}");
            isPatched = true;
        }


        // Default processing using Reflection
        if (prefabConfig.TryGetComponent(compName, out ComponentXml compConfig))
        {
            Mod.LogIf($"{prefab.name}.{compName}: valid");
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
                    if (Mod.setting.Logging)
                        Mod.Log($"{prefab.name}.{compName}.{field.Name}: {oldValue} -> {field.GetValue(component)} ({field.FieldType}, {fieldConfig})");
                    else
                        Mod.Log($"{prefab.name}.{compName}.{field.Name}: {field.GetValue(component)}");
                    isPatched = true;
                }
                else
                {
                    Mod.Log($"{prefab.name}.{compName}: Warning! Field {fieldConfig.Name} not found in the component.");
                }
            }
            if (Mod.setting.Logging) DumpFields(prefab, component); // debug
        }

        // 240401 We quit if there is no default processing nor special cases
        if (!isPatched)
        {
            Mod.LogIf($"{prefab.name}.{compName}: SKIP");
            return;
        }

        // 240401 Modify Entity
        MethodInfo methodInit = component.GetType().GetMethod("Initialize");
        MethodInfo methodLate = component.GetType().GetMethod("LateInitialize");
        MethodInfo methodArch = component.GetType().GetMethod("RefreshArchetype");
        bool hasInit = methodInit.DeclaringType == component.GetType();
        bool hasLate = methodLate.DeclaringType == component.GetType();
        bool hasArch = methodArch != null; // this is a bit different because it is not declared in ComponentBase, so not components may have it
        Mod.LogIf($"{prefab.name}.{compName}: INIT {hasInit} {methodInit.DeclaringType.Name} LATE {hasLate} {methodLate.DeclaringType.Name} ARCH {hasArch} {(hasArch ? methodArch.DeclaringType.Name : "none")}");

        // 240405 Fix for rare problems with DemandPrefab and prefabs in it - we cannot use LateInitialize
        if (prefab.GetType() == typeof(DemandPrefab))
        {
            Mod.LogIf($"... DemandPrefab special treatment");
            DemandParameterData componentData = m_EntityManager.GetComponentData<DemandParameterData>(entity);
            DemandPrefab dp = prefab as DemandPrefab;
            componentData.m_MinimumHappiness = dp.m_MinimumHappiness;
            componentData.m_HappinessEffect = dp.m_HappinessEffect;
            componentData.m_UnemploymentEffect = dp.m_UnemploymentEffect;
            componentData.m_HomelessEffect = dp.m_HomelessEffect;
            componentData.m_NeutralHappiness = dp.m_NeutralHappiness;
            componentData.m_NeutralUnemployment = dp.m_NeutralUnemployment;
            componentData.m_NeutralHomelessness = dp.m_NeutralHomelessness;
            componentData.m_FreeResidentialProportion = dp.m_FreeResidentialProportion;
            componentData.m_FreeCommercialProportion = dp.m_FreeCommercialProportion;
            componentData.m_FreeIndustrialProportion = dp.m_FreeIndustrialProportion;
            componentData.m_CommercialStorageMinimum = dp.m_CommercialStorageMinimum;
            componentData.m_CommercialStorageEffect = dp.m_CommercialStorageEffect;
            componentData.m_CommercialBaseDemand = dp.m_CommercialBaseDemand;
            componentData.m_IndustrialStorageMinimum = dp.m_IndustrialStorageMinimum;
            componentData.m_IndustrialStorageEffect = dp.m_IndustrialStorageEffect;
            componentData.m_IndustrialBaseDemand = dp.m_IndustrialBaseDemand;
            componentData.m_ExtractorBaseDemand = dp.m_ExtractorBaseDemand;
            componentData.m_StorageDemandMultiplier = dp.m_StorageDemandMultiplier;
            componentData.m_LowRentDefaultRent = dp.m_LowRentDefaultRent;
            componentData.m_ResidentialHighDefaultRent = dp.m_ResidentialHighDefaultRent;
            componentData.m_ResidentialLowDefaultRent = dp.m_ResidentialLowDefaultRent;
            componentData.m_CommuterWorkerRatioLimit = dp.m_CommuterWorkerRatioLimit;
            componentData.m_CommuterSlowSpawnFactor = dp.m_CommuterSlowSpawnFactor;
            componentData.m_CommuterOCSpawnParameters = dp.m_CommuterOCSpawnParameters;
            componentData.m_TouristOCSpawnParameters = dp.m_TouristOCSpawnParameters;
            componentData.m_CitizenOCSpawnParameters = dp.m_CitizenOCSpawnParameters;
            componentData.m_TeenSpawnPercentage = dp.m_TeenSpawnPercentage;
            m_EntityManager.SetComponentData(entity, componentData);
        }

        // The logic is that if there is only either Initialize or LateInitialize, we call them
        // I don't think there is a case where both are defined at the same time but if so - need to check on individual basis
        else if (hasInit && hasLate)
        {
            Mod.log.Warn($"DUALINIT: {prefab.name}.{compName} has both Init and LateInit; not supported.");
        }
        else if (hasLate)
        {
            Mod.LogIf($"... calling LateInitialize");
            component.LateInitialize(m_EntityManager, entity);
        }
        else if (hasInit)
        {
            Mod.LogIf($"... calling Initialize");
            component.Initialize(m_EntityManager, entity);
        }
        else
        {
            if (compName != "ResourcePrefab" && compName != "CompanyPrefab")
                Mod.log.Warn($"ZEROINIT: {prefab.name}.{compName} has no Init and no LateInit; not supported.");
        }

        // After that there are cases where RefreshArchetype is needed - very rare so can be handled as exception
        if (hasArch)
        {
            Mod.log.Warn($"ARCHETYPE: {prefab.name}.{compName} has RefreshArchetype; not supported.");
        }
    }

    // Method to change the value of a field in an ECS component by name
    // NOT USED
    /*
    public static void SetFieldValue<T>(ref T component, string fieldName, object newValue) where T : struct, IComponentData
    {
        Type type = typeof(T);
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field != null)
        {
            object oldValue = field.GetValue(component);
            field.SetValueDirect(__makeref(component), newValue);
            Mod.log.Info($"{type.Name}.{field.Name}: {oldValue} -> {field.GetValue(component)} ({field.FieldType})");
        }
        else
        {
            Mod.log.Info($"Field '{fieldName}' not found in struct '{type.Name}'.");
        }
    }
    */

    // NOT USED - CRASHES THE GAME ATM
    /*
    public static void ConfigureComponentData<T>(ComponentXml compXml, ref T component) where T : struct, IComponentData
    {
        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            object oldValue = field.GetValue(component);
            // change it
            if (compXml.TryGetField(field.Name, out FieldXml fieldXml))
            {
                // TODO: extend for other field types
                if (field.FieldType == typeof(float))
                {
                    field.SetValueDirect(__makeref(component), fieldXml.ValueFloat);
                }
                else
                {
                    field.SetValueDirect(__makeref(component), fieldXml.ValueInt);
                }
                Mod.log.Info($"{type.Name}.{field.Name}: {oldValue} -> {field.GetValue(component)} ({field.FieldType})");
            }
            else
                Mod.LogIf($"{type.Name}.{field.Name}: {oldValue}");
        }
    }
    */

    /// <summary>
    /// Configures a specific prefab according to the config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    private static void ConfigurePrefab(PrefabBase prefab, PrefabXml prefabConfig, Entity entity, bool skipEntity = false)
    {
        Mod.LogIf($"{prefab.name}: valid {prefab.GetType().Name} entity {entity.Index} {skipEntity}");
        // check first if the main prefab needs to be changed
        ConfigureComponent(prefab, prefabConfig, prefab, entity, skipEntity);
        // iterate through components and see which ones need to be changed
        foreach (ComponentBase component in prefab.components)
            ConfigureComponent(prefab, prefabConfig, component, entity, skipEntity);
    }

    /* NOT USED 
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool PrefabSystem_AddPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        if (isLatePrefabsActive && Mod.setting.FeaturePrefabs) // && ConfigToolXml.Config.IsPrefabValid(prefab.GetType().Name))
        {
            if (ConfigToolXml.Config.TryGetPrefab(prefab.name, out PrefabXml prefabConfig))
            {
                ConfigurePrefab(prefab, prefabConfig, default(Entity), true);
            }
            else
                Mod.LogIf($"{prefab.name}: SKIP {prefab.GetType().Name}");
        }
    */
        // 240301 extract specific components
        /*
        ConfigurationXml config = ConfigToolXml.Config;
        if (prefab.Has<Workplace>())
        {
            Workplace comp = prefab.GetComponent<Workplace>();
            PrefabXml prefabConfig = default(PrefabXml);
            if (!config.TryGetPrefab(prefab.name, out prefabConfig))
                config.Prefabs.Add(new PrefabXml { Name = prefab.name, Components = new List<ComponentXml>() });
            if (config.TryGetPrefab(prefab.name, out prefabConfig))
            {
                ComponentXml compConfig = default(ComponentXml);
                if (!prefabConfig.TryGetComponent("Workplace", out compConfig))
                    prefabConfig.Components.Add(new ComponentXml { Name = "Workplace", Fields = new List<FieldXml>() });
                if (prefabConfig.TryGetComponent("Workplace", out compConfig))
                {
                    if (!compConfig.TryGetField("m_Workplaces", out FieldXml field1Config))
                        compConfig.Fields.Add(new FieldXml { Name = "m_Workplaces", ValueInt = comp.m_Workplaces });
                    if (!compConfig.TryGetField("m_Complexity", out FieldXml field2Config))
                        compConfig.Fields.Add(new FieldXml { Name = "m_Complexity", ValueInt = (int)comp.m_Complexity });
                }
            }
        }
        */
        //return true;
    //}

	/* NOT USED
    /// <summary>
    /// Configures a specific component within a specific prefab according to config data.
    /// </summary>
    /// <param name="compXml"></param>
    /// <param name="prefab"></param>
    /// <param name="entity"></param>
    private static void ConfigureComponent(ComponentXml compXml, PrefabBase prefab, Entity entity)
    {
        // Infixo: first version which is not yet dynamic :(
        FieldXml fieldXml;
        switch (compXml.Name)
        {
            case "WorkplaceData":
                if (m_PrefabSystem.TryGetComponentData<WorkplaceData>(prefab, out WorkplaceData workplaceData))
                {
                    if (compXml.TryGetField("m_Complexity", out fieldXml))
                    {
                        workplaceData.m_Complexity = (WorkplaceComplexity)fieldXml.ValueInt;
                    }
                    if (compXml.TryGetField("m_MaxWorkers", out fieldXml))
                    {
                        workplaceData.m_MaxWorkers = (int)fieldXml.ValueInt;
                    }
                    m_PrefabSystem.AddComponentData<WorkplaceData>(prefab, workplaceData);
                    Mod.log.Info($"{prefab.name}.{compXml.Name}: {workplaceData.m_Complexity} {workplaceData.m_MaxWorkers}");
                }
                break;
            case "DeathcareFacilityData":
                if (m_PrefabSystem.TryGetComponentData<DeathcareFacilityData>(prefab, out DeathcareFacilityData deathcareFacilityData))
                {
                    if (compXml.TryGetField("m_ProcessingRate", out fieldXml))
                    {
                        deathcareFacilityData.m_ProcessingRate = (float)fieldXml.ValueFloat;
                    }
                    m_PrefabSystem.AddComponentData<DeathcareFacilityData>(prefab, deathcareFacilityData);
                    Mod.log.Info($"{prefab.name}.{compXml.Name}: {deathcareFacilityData.m_ProcessingRate}");
                }
                break;
            case "PostFacilityData":
                if (m_PrefabSystem.TryGetComponentData<PostFacilityData>(prefab, out PostFacilityData postFacilityData))
                {
                    if (compXml.TryGetField("m_SortingRate", out fieldXml))
                    {
                        postFacilityData.m_SortingRate = (int)fieldXml.ValueInt;
                    }
                    m_PrefabSystem.AddComponentData<PostFacilityData>(prefab, postFacilityData);
                    Mod.log.Info($"{prefab.name}.{compXml.Name}: {postFacilityData.m_SortingRate}");
                }
                break;
            default:
                Mod.log.Warn($"{compXml} is not supported.");
                break;
        }
    }
	*/

    /* not used
    /// <summary>
    /// Configures a specific prefab according to the config data.
    /// </summary>
    /// <param name="prefabXml"></param>
    /// <param name="prefab"></param>
    /// <param name="entity"></param>
    private static void ConfigurePrefab(PrefabXml prefabXml, PrefabBase prefab, Entity entity)
    {
        Mod.LogIf($"{prefab.name}: valid {prefab.GetType().Name}");
        // iterate through components and see which ones need to be changed
        foreach (ComponentXml componentXml in prefabXml.Components)
            ConfigureComponent(componentXml, prefab, entity);
    }
    */

    public static void ReadAndApply()
    {
        // READ CONFIG DATA
        ConfigToolXml.LoadConfig(Mod.modAsset.path);

        m_PrefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach (PrefabXml prefabXml in ConfigToolXml.Config.Prefabs)
        {
            PrefabID prefabID = new PrefabID(prefabXml.Type, prefabXml.Name);
            if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab) && m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
            {
                Mod.LogIf($"{prefabXml} successfully retrieved from the PrefabSystem.");
                ConfigurePrefab(prefab, prefabXml, entity);
                /*
                if (ConfigToolXml.Config.IsPrefabValid(prefab.GetType().Name))
                {
                    ConfigurePrefab(prefabXml, prefab, entity);
                }
                else
                    Mod.log.Info($"{prefab.name}: SKIP {prefab.GetType().Name}");
                */
            }
            else
                Mod.log.Warn($"Failed to retrieve {prefabXml} from the PrefabSystem.");
        }
    }

    // List components from entity
    internal static void ListComponents(PrefabBase prefab, Entity entity)
    {
        foreach (ComponentType componentType in m_EntityManager.GetComponentTypes(entity))
        {
            Mod.log.Info($"{prefab.GetType().Name}.{prefab.name}.{componentType.GetManagedType().Name}: {componentType}");
        }
    }
}

// FOR THE FUTURE
/* This code reads a dictionary and puts it into the config xml


ConfigurationXml config = ConfigToolXml.Config;

foreach (var item in PrefabSystem_AddPrefab_Patches.MaxWorkersPerCellDict)
{
    Mod.Log($"DICT {item.Key} {item.Value}");
    PrefabXml prefabConfig = default(PrefabXml);
    if (!config.TryGetPrefab(item.Key, out prefabConfig))
        config.Prefabs.Add( new PrefabXml { Name = item.Key, Components = new List<ComponentXml>() });
    if (config.TryGetPrefab(item.Key, out prefabConfig))
    {
        ComponentXml compConfig = default(ComponentXml);
        if (!prefabConfig.TryGetComponent("IndustrialProcess", out compConfig))
            prefabConfig.Components.Add( new ComponentXml { Name = "IndustrialProcess", Fields = new List<FieldXml>() });
        if (prefabConfig.TryGetComponent("IndustrialProcess", out compConfig))
        {
            if (!compConfig.TryGetField("m_MaxWorkersPerCell", out FieldXml fieldConfig))
                compConfig.Fields.Add(new FieldXml { Name = "m_MaxWorkersPerCell", ValueFloat = item.Value });
        }
    }
}

ConfigurationXml config = ConfigToolXml.Config;

foreach (var item in PrefabSystem_AddPrefab_Patches.ProfitabilityDict)
{
    Mod.Log($"DICT {item.Key} {item.Value}");
    PrefabXml prefabConfig = default(PrefabXml);
    if (!config.TryGetPrefab(item.Key, out prefabConfig))
        config.Prefabs.Add( new PrefabXml { Name = item.Key, Components = new List<ComponentXml>() });
    if (config.TryGetPrefab(item.Key, out prefabConfig))
    {
        ComponentXml compConfig = default(ComponentXml);
        if (!prefabConfig.TryGetComponent("CompanyPrefab", out compConfig))
            prefabConfig.Components.Add(new ComponentXml { Name = "CompanyPrefab", Fields = new List<FieldXml>() });
        if (prefabConfig.TryGetComponent("CompanyPrefab", out compConfig))
        {
            if (!compConfig.TryGetField("profitability", out FieldXml fieldConfig))
                compConfig.Fields.Add(new FieldXml { Name = "profitability", ValueFloat = item.Value });
        }
    }
}
*/
