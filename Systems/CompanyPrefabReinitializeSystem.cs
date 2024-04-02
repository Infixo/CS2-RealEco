using System.Runtime.CompilerServices;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Prefabs;

namespace RealEco.Systems;

[CompilerGenerated]
public partial class CompanyPrefabInitializeSystem : GameSystemBase
{
    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CommercialCompanyData> __Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<IndustrialCompanyData> __Game_Prefabs_IndustrialCompanyData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ExtractorCompanyData> __Game_Prefabs_ExtractorCompanyData_RO_ComponentTypeHandle;

        public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RW_ComponentTypeHandle;

        public ComponentTypeHandle<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
            __Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialCompanyData>(isReadOnly: true);
            __Game_Prefabs_IndustrialCompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialCompanyData>(isReadOnly: true);
            __Game_Prefabs_ExtractorCompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorCompanyData>(isReadOnly: true);
            __Game_Prefabs_IndustrialProcessData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>();
            __Game_Companies_ServiceCompanyData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceCompanyData>();
            __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkplaceData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
        }
    }

    private EntityQuery m_PrefabQuery;

    private EntityQuery m_EconomyParameterQuery;

    private PrefabSystem m_PrefabSystem;

    private TypeHandle __TypeHandle;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
        m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1]
            {
                //ComponentType.ReadWrite<Created>(), // process all prefabs
                ComponentType.ReadOnly<PrefabData>()
            },
            Any = new ComponentType[2]
            {
                ComponentType.ReadWrite<CommercialCompanyData>(),
                ComponentType.ReadWrite<IndustrialCompanyData>()
            }
        });
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        RequireForUpdate(m_PrefabQuery);
        RequireForUpdate(m_EconomyParameterQuery);
        Mod.log.Info("CompanyPrefabInitializeSystem for RealEco created.");
    }

    [Preserve]
    protected override void OnUpdate()
    {
        Mod.log.Info($"Reinitializing {m_PrefabQuery.CalculateEntityCount()} companies.");
        NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        EntityTypeHandle _Unity_Entities_Entity_TypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        _ = __TypeHandle;
        __TypeHandle.__Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<CommercialCompanyData> typeHandle = __TypeHandle.__Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_IndustrialCompanyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<IndustrialCompanyData> typeHandle2 = __TypeHandle.__Game_Prefabs_IndustrialCompanyData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ExtractorCompanyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ExtractorCompanyData> typeHandle3 = __TypeHandle.__Game_Prefabs_ExtractorCompanyData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_IndustrialProcessData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<IndustrialProcessData> typeHandle4 = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Companies_ServiceCompanyData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ServiceCompanyData> typeHandle5 = __TypeHandle.__Game_Companies_ServiceCompanyData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<WorkplaceData> typeHandle6 = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        ComponentLookup<ResourceData> _Game_Prefabs_ResourceData_RO_ComponentLookup = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        ResourcePrefabs prefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
        EconomyParameterData economyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
        Mod.LogIf($"Reinitializing: ext {economyParameters.m_ExtractorCompanyExportMultiplier} ind {economyParameters.m_IndustrialProfitFactor} com {economyParameters.m_CommercialDiscount}");
        for (int i = 0; i < nativeArray.Length; i++)
        {
            ArchetypeChunk archetypeChunk = nativeArray[i];
            NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
            NativeArray<IndustrialProcessData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle4);
            NativeArray<WorkplaceData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle6);
            BuildingData buildingData = default(BuildingData);
            buildingData.m_LotSize = new int2(100, 10);
            BuildingData buildingData2 = buildingData;
            SpawnableBuildingData spawnableBuildingData = default(SpawnableBuildingData);
            spawnableBuildingData.m_Level = 1;
            SpawnableBuildingData spawnableBuildingData2 = spawnableBuildingData;
            if (archetypeChunk.Has(ref typeHandle))
            {
                NativeArray<ServiceCompanyData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
                for (int j = 0; j < archetypeChunk.Count; j++)
                {
                    CompanyPrefab prefab = m_PrefabSystem.GetPrefab<CompanyPrefab>(nativeArray2[j]);
                    ServiceAvailable serviceAvailable = default(ServiceAvailable);
                    serviceAvailable.m_MeanPriority = 0.5f;
                    serviceAvailable.m_ServiceAvailable = nativeArray5[j].m_MaxService / 2;
                    ServiceAvailable service = serviceAvailable;
                    IndustrialProcessData industrialProcessData = nativeArray3[j];
                    float3 tradeCosts = EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData, _Game_Prefabs_ResourceData_RO_ComponentLookup, prefabs);
                    ServiceCompanyData serviceCompanyData = nativeArray5[j];
                    WorkplaceData workplaceData = nativeArray4[j];
                    int workers = Mathf.RoundToInt(serviceCompanyData.m_MaxWorkersPerCell * 1000f);
                    int num = 1;
                    int num2 = 65536;
                    serviceCompanyData.m_WorkPerUnit = 512;
                    do
                    {
                        if (0.001f * (float)ServiceCompanySystem.EstimateDailyProfit(workers, service, serviceCompanyData, buildingData2, industrialProcessData, ref economyParameters, workplaceData, spawnableBuildingData2, prefabs, _Game_Prefabs_ResourceData_RO_ComponentLookup, tradeCosts) > prefab.profitability)
                        {
                            num = serviceCompanyData.m_WorkPerUnit;
                        }
                        else
                        {
                            num2 = serviceCompanyData.m_WorkPerUnit;
                        }
                        serviceCompanyData.m_WorkPerUnit = (num + num2) / 2;
                    }
                    while (num < num2 - 1);
                    if (serviceCompanyData.m_WorkPerUnit == 0)
                    {
                        UnityEngine.Debug.Log($"Warning: calculated work per unit for service company prefab {nativeArray2[i].Index} is zero");
                    }
                    nativeArray5[j] = serviceCompanyData;
                }
            }
            else if (archetypeChunk.Has(ref typeHandle3))
            {
                for (int k = 0; k < archetypeChunk.Count; k++)
                {
                    CompanyPrefab prefab2 = m_PrefabSystem.GetPrefab<CompanyPrefab>(nativeArray2[k]);
                    IndustrialProcessData industrialProcessData2 = nativeArray3[k];
                    EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData2, _Game_Prefabs_ResourceData_RO_ComponentLookup, prefabs);
                    WorkplaceData workplaceData2 = nativeArray4[k];
                    int fittingWorkers = ExtractorAISystem.GetFittingWorkers(156.25f, 1f, industrialProcessData2);
                    int num3 = 1;
                    int num4 = 65536;
                    industrialProcessData2.m_WorkPerUnit = 512;
                    do
                    {
                        if ((float)ExtractorCompanySystem.EstimateDailyProfit(ExtractorCompanySystem.EstimateDailyProduction(1f, fittingWorkers, 1, workplaceData2, industrialProcessData2, ref economyParameters), fittingWorkers, industrialProcessData2, ref economyParameters, workplaceData2, spawnableBuildingData2, prefabs, _Game_Prefabs_ResourceData_RO_ComponentLookup) * 64f / 10000f > prefab2.profitability)
                        {
                            num3 = industrialProcessData2.m_WorkPerUnit;
                        }
                        else
                        {
                            num4 = industrialProcessData2.m_WorkPerUnit;
                        }
                        industrialProcessData2.m_WorkPerUnit = (num3 + num4) / 2;
                    }
                    while (num3 < num4 - 1);
                    industrialProcessData2.m_WorkPerUnit = num3;
                    if (industrialProcessData2.m_WorkPerUnit == 0)
                    {
                        UnityEngine.Debug.LogError($"calculated work per unit for extractor company prefab {nativeArray2[i].Index} is zero");
                        industrialProcessData2.m_WorkPerUnit = 1;
                    }
                    nativeArray3[k] = industrialProcessData2;
                }
            }
            else
            {
                if (!archetypeChunk.Has(ref typeHandle2) || !archetypeChunk.Has(ref typeHandle6))
                {
                    continue;
                }
                for (int l = 0; l < archetypeChunk.Count; l++)
                {
                    CompanyPrefab prefab3 = m_PrefabSystem.GetPrefab<CompanyPrefab>(nativeArray2[l]);
                    IndustrialProcessData industrialProcessData3 = nativeArray3[l];
                    float3 tradeCosts2 = EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData3, _Game_Prefabs_ResourceData_RO_ComponentLookup, prefabs);
                    WorkplaceData workplaceData3 = nativeArray4[l];
                    int workers2 = Mathf.RoundToInt(industrialProcessData3.m_MaxWorkersPerCell * 1000f);
                    int num5 = 1;
                    int num6 = 65536;
                    industrialProcessData3.m_WorkPerUnit = 512;
                    do
                    {
                        if (economyParameters.m_IndustrialProfitFactor * (float)ProcessingCompanySystem.EstimateDailyProfit(workers2, industrialProcessData3, buildingData2, ref economyParameters, tradeCosts2, workplaceData3, spawnableBuildingData2, prefabs, _Game_Prefabs_ResourceData_RO_ComponentLookup) > prefab3.profitability)
                        {
                            num5 = industrialProcessData3.m_WorkPerUnit;
                        }
                        else
                        {
                            num6 = industrialProcessData3.m_WorkPerUnit;
                        }
                        industrialProcessData3.m_WorkPerUnit = (num5 + num6) / 2;
                    }
                    while (num5 < num6 - 1);
                    if (industrialProcessData3.m_WorkPerUnit == 0)
                    {
                        UnityEngine.Debug.Log($"Warning: calculated work per unit for industry company prefab {nativeArray2[i].Index} is zero");
                    }
                    nativeArray3[l] = industrialProcessData3;
                }
            }
        }
        nativeArray.Dispose();
        base.Enabled = false; // run only once
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public CompanyPrefabInitializeSystem()
    {
    }
}
