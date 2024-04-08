using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Logging;
using Colossal.Mathematics;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Prefabs;

namespace RealEco.Systems;

/* 
240408 This system reinitializes spawnable BuildingPrefabs with components from their Zones.
Currently:
- ZoneServiceConsumption => ConsumptionData
- ZoneProperties =>  BuildingPropertyData
It also recalculates Upkeep for buildings to use Upkeep values from Zones.
Unfortunately this info is already is passed from Zones to individual buildings so they need to be patched also.

The query will check: 
- PrefabData
??? - BuildingPropertyData => or just here change AllowedSold BUT some have specific info (see below)
- SpawnableBuildingData => m_ZonePrefab check zone via ZoneData component and m_AreaType = Commercial or Mixed
??? - ConsumptionData

TBD:  ZonePollution => PollutionData
*/


public partial class BuildingReinitializeSystem : GameSystemBase
{
    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

        public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RW_ComponentTypeHandle;

        public ComponentTypeHandle<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;

        public ComponentTypeHandle<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;

        public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

        public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

        public ComponentTypeHandle<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;

        public BufferTypeHandle<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;

        public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentTypeHandle<ExtractorFacilityData> __Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WaterTowerData> __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<WastewaterTreatmentPlantData> __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle;

        //[ReadOnly]
        //public ComponentTypeHandle<TransformerData> __Game_Prefabs_TransformerData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PublicTransportStationData> __Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle;

        //[ReadOnly]
        //public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RO_BufferTypeHandle;

        //[ReadOnly]
        //public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RO_BufferTypeHandle;

        public BufferTypeHandle<Effect> __Game_Prefabs_Effect_RW_BufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

        //[ReadOnly]
        //public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

        //[ReadOnly]
        //public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<VFXData> __Game_Prefabs_VFXData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<AudioSpotData> __Game_Prefabs_AudioSpotData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

        //[ReadOnly]
        //public BufferLookup<SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
            __Game_Prefabs_BuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>();
            __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingExtensionData>();
            __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingTerraformData>();
            __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>();
            __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>();
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(isReadOnly: true);
            __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableObjectData>();
            __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgradeData>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPoweredData>(isReadOnly: true);
            __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SewageOutletData>(isReadOnly: true);
            __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpgradeBuilding>(isReadOnly: true);
            __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(isReadOnly: true);
            __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpkeepData>();
            __Game_Prefabs_ZoneData_RW_ComponentLookup = state.GetComponentLookup<ZoneData>();
            __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ZoneServiceConsumptionData>(isReadOnly: true);
            __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
            __Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorFacilityData>(isReadOnly: true);
            __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(isReadOnly: true);
            __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkplaceData>(isReadOnly: true);
            __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPumpingStationData>(isReadOnly: true);
            __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterTowerData>(isReadOnly: true);
            __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WastewaterTreatmentPlantData>(isReadOnly: true);
            //__Game_Prefabs_TransformerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransformerData>(isReadOnly: true);
            __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingFacilityData>(isReadOnly: true);
            __Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PublicTransportStationData>(isReadOnly: true);
            __Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CargoTransportStationData>(isReadOnly: true);
            //__Game_Prefabs_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>(isReadOnly: true);
            //__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
            __Game_Prefabs_SubMesh_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>(isReadOnly: true);
            __Game_Prefabs_Effect_RW_BufferTypeHandle = state.GetBufferTypeHandle<Effect>();
            __Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
            __Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
            //__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
            //__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
            __Game_Prefabs_VFXData_RO_ComponentLookup = state.GetComponentLookup<VFXData>(isReadOnly: true);
            __Game_Prefabs_AudioSourceData_RO_BufferLookup = state.GetBufferLookup<AudioSourceData>(isReadOnly: true);
            __Game_Prefabs_AudioSpotData_RO_ComponentLookup = state.GetComponentLookup<AudioSpotData>(isReadOnly: true);
            __Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(isReadOnly: true);
            //__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
        }
    }

    //private static ILog log;

    private EntityQuery m_PrefabQuery;

    //private EntityQuery m_ConfigurationQuery;

    private PrefabSystem m_PrefabSystem;

    private TypeHandle __TypeHandle;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
        m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[4]
            {
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadWrite<BuildingData>(), // needed for a lot size, 2558 buildings - this is a superset that contains SpawnableBuilding and BuildingProperty
                ComponentType.ReadWrite<SpawnableBuildingData>(), // 2371 buildings
                ComponentType.ReadWrite<BuildingPropertyData>() // 2371 buildings, it is always there
            },
            //Any = new ComponentType[2]
            //{
                //ComponentType.ReadWrite<BuildingExtensionData>(),
                //ComponentType.ReadWrite<ServiceUpgradeData>(), // these are all building extensions for city services
                //ComponentType.ReadWrite<SpawnableBuildingData>() // 2371 buildings
            //}
            // HAS BD && NO Spawnable - 187, main city service buildings, extractor buildings, ruins
            // NO BD && HAS Spawnable - ZERO
            // HAS BPD - 2380, so 9 diff?
            // HAS BPD && No Spawnable - 9 buildings - extractor placeholders - all have SpaceMult = 0
            // NO BD && HAS BPD - ZERO
            // HAS BD && No BPD - 178
        });
        //m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
        RequireForUpdate(m_PrefabQuery);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        Mod.log.Info("BuildingReinitializeSystem created.");
    }

    protected override void OnUpdate()
    {
        int numUpkeep = 0, numSpaceMult = 0;
        Mod.log.Info($"BuildingReinitializeSystem.OnUpdate: {m_PrefabQuery.CalculateEntityCount()}");
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
        NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        EntityTypeHandle _Unity_Entities_Entity_TypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<PrefabData> typeHandle = __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingData> typeHandle2 = __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingExtensionData> typeHandle3 = __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingTerraformData> typeHandle4 = __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ConsumptionData> typeHandle5 = __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ObjectGeometryData> typeHandle6 = __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SpawnableBuildingData> typeHandle7 = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SignatureBuildingData> typeHandle8 = __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<PlaceableObjectData> typeHandle9 = __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ServiceUpgradeData> typeHandle10 = __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingPropertyData> typeHandle11 = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<WaterPoweredData> typeHandle12 = __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SewageOutletData> typeHandle13 = __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        BufferTypeHandle<ServiceUpgradeBuilding> bufferTypeHandle = __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;
        __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<CollectedServiceBuildingBudgetData> typeHandle14 = __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        BufferTypeHandle<ServiceUpkeepData> bufferTypeHandle2 = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;
        __TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        ComponentLookup<ZoneData> _Game_Prefabs_ZoneData_RW_ComponentLookup = __TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup;
        __TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        ComponentLookup<ZoneServiceConsumptionData> _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = __TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;
        __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        ComponentLookup<ZonePropertiesData> _Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;
        CompleteDependency();
        for (int i = 0; i < chunks.Length; i++)
        {
            ArchetypeChunk archetypeChunk = chunks[i];
            NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle);
            //NativeArray<ObjectGeometryData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle6);
            NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
            //NativeArray<BuildingExtensionData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle3);
            NativeArray<ConsumptionData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
            NativeArray<SpawnableBuildingData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle7);
            //NativeArray<PlaceableObjectData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle9);
            //NativeArray<ServiceUpgradeData> nativeArray8 = archetypeChunk.GetNativeArray(ref typeHandle10);
            NativeArray<BuildingPropertyData> nativeArray9 = archetypeChunk.GetNativeArray(ref typeHandle11);
            //BufferAccessor<ServiceUpgradeBuilding> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
            //BufferAccessor<ServiceUpkeepData> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
            NativeArray<Entity> nativeArray10 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
            //bool flag = archetypeChunk.Has(ref typeHandle14);
            bool isSignature = archetypeChunk.Has(ref typeHandle8);
            //bool flag3 = archetypeChunk.Has(ref typeHandle12);
            //bool flag4 = archetypeChunk.Has(ref typeHandle13);

            // BUILDING DATA + INIT LOT SIZE
            /* not used
            if (nativeArray3.Length != 0)
            {
                NativeArray<BuildingTerraformData> nativeArray11 = archetypeChunk.GetNativeArray(ref typeHandle4);
                for (int j = 0; j < nativeArray3.Length; j++)
                {
                    BuildingPrefab prefab = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[j]);
                    BuildingTerraformOverride component = prefab.GetComponent<BuildingTerraformOverride>();
                    ObjectGeometryData objectGeometryData = nativeArray2[j];
                    BuildingTerraformData buildingTerraformData = nativeArray11[j];
                    BuildingData buildingData = nativeArray3[j];
                    InitializeLotSize(prefab, component, ref objectGeometryData, ref buildingTerraformData, ref buildingData);
                    if (nativeArray6.Length != 0 && !flag2)
                    {
                        objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.DeleteOverridden;
                    }
                    else
                    {
                        objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Overridable;
                        objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                    }
                    if (flag3)
                    {
                        objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
                    }
                    else if (flag4 && prefab.GetComponent<SewageOutlet>().m_AllowSubmerged)
                    {
                        objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
                    }
                    objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Brushable;
                    objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot;
                    nativeArray2[j] = objectGeometryData;
                    nativeArray11[j] = buildingTerraformData;
                    nativeArray3[j] = buildingData;
                }
            }
            */
            // BUILDING EXTENSION + TERRAFORM - not used
            /*
            if (nativeArray4.Length != 0)
            {
                NativeArray<BuildingTerraformData> nativeArray12 = archetypeChunk.GetNativeArray(ref typeHandle4);
                for (int k = 0; k < nativeArray4.Length; k++)
                {
                    BuildingExtensionPrefab prefab2 = m_PrefabSystem.GetPrefab<BuildingExtensionPrefab>(nativeArray[k]);
                    ObjectGeometryData value = nativeArray2[k];
                    Bounds2 flatBounds;
                    if ((value.m_Flags & Game.Objects.GeometryFlags.Standing) != 0)
                    {
                        float2 xz = value.m_Pivot.xz;
                        float2 @float = value.m_LegSize.xz * 0.5f;
                        flatBounds = new Bounds2(xz - @float, xz + @float);
                    }
                    else
                    {
                        flatBounds = value.m_Bounds.xz;
                    }
                    value.m_Bounds.min = math.min(value.m_Bounds.min, new float3(-0.5f, 0f, -0.5f));
                    value.m_Bounds.max = math.max(value.m_Bounds.max, new float3(0.5f, 5f, 0.5f));
                    value.m_Flags &= ~(Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Brushable);
                    value.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot;
                    BuildingExtensionData value2 = nativeArray4[k];
                    value2.m_Position = prefab2.m_Position;
                    value2.m_LotSize = prefab2.m_OverrideLotSize;
                    value2.m_External = prefab2.m_ExternalLot;
                    if (prefab2.m_OverrideHeight > 0f)
                    {
                        value.m_Bounds.max.y = prefab2.m_OverrideHeight;
                    }
                    Bounds2 lotBounds;
                    if (math.all(value2.m_LotSize > 0))
                    {
                        float2 float2 = value2.m_LotSize;
                        float2 *= 8f;
                        lotBounds = new Bounds2(float2 * -0.5f, float2 * 0.5f);
                        float2 -= 0.4f;
                        value.m_Bounds.min.xz = float2 * -0.5f;
                        value.m_Bounds.max.xz = float2 * 0.5f;
                        if (bufferAccessor.Length != 0)
                        {
                            value.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                        }
                    }
                    else
                    {
                        Bounds3 bounds = value.m_Bounds;
                        lotBounds = value.m_Bounds.xz;
                        if (bufferAccessor.Length != 0)
                        {
                            DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer = bufferAccessor[k];
                            for (int l = 0; l < dynamicBuffer.Length; l++)
                            {
                                ServiceUpgradeBuilding serviceUpgradeBuilding = dynamicBuffer[l];
                                BuildingPrefab prefab3 = m_PrefabSystem.GetPrefab<BuildingPrefab>(serviceUpgradeBuilding.m_Building);
                                float2 float3 = new int2(prefab3.m_LotWidth, prefab3.m_LotDepth);
                                float3 *= 8f;
                                float2 float4 = float3;
                                float3 -= 0.4f;
                                if ((value.m_Flags & Game.Objects.GeometryFlags.Standing) == 0 && prefab3.TryGet<StandingObject>(out var component2))
                                {
                                    float3 = component2.m_LegSize.xz;
                                    float4 = component2.m_LegSize.xz;
                                    if (component2.m_CircularLeg)
                                    {
                                        value.m_Flags |= Game.Objects.GeometryFlags.Circular;
                                    }
                                }
                                if (l == 0)
                                {
                                    bounds.xz = new Bounds2(float3 * -0.5f, float3 * 0.5f) - prefab2.m_Position.xz;
                                    lotBounds = new Bounds2(float4 * -0.5f, float4 * 0.5f) - prefab2.m_Position.xz;
                                }
                                else
                                {
                                    bounds.xz &= new Bounds2(float3 * -0.5f, float3 * 0.5f) - prefab2.m_Position.xz;
                                    lotBounds &= new Bounds2(float4 * -0.5f, float4 * 0.5f) - prefab2.m_Position.xz;
                                }
                            }
                            value.m_Bounds.xz = bounds.xz;
                            value.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                        }
                        float2 float5 = math.min(-bounds.min.xz, bounds.max.xz) * 0.25f - 0.01f;
                        value2.m_LotSize.x = math.max(1, Mathf.CeilToInt(float5.x));
                        value2.m_LotSize.y = math.max(1, Mathf.CeilToInt(float5.y));
                    }
                    if (value2.m_External)
                    {
                        float2 float6 = value2.m_LotSize;
                        float6 *= 8f;
                        value.m_Layers |= MeshLayer.Default;
                        value.m_MinLod = math.min(value.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(float6.x, 0f, float6.y))));
                    }
                    if (nativeArray12.Length != 0)
                    {
                        BuildingTerraformOverride component3 = prefab2.GetComponent<BuildingTerraformOverride>();
                        BuildingTerraformData buildingTerraformData2 = nativeArray12[k];
                        InitializeTerraformData(component3, ref buildingTerraformData2, lotBounds, flatBounds);
                        nativeArray12[k] = buildingTerraformData2;
                    }
                    value.m_Size = math.max(ObjectUtils.GetSize(value.m_Bounds), new float3(1f, 5f, 1f));
                    nativeArray2[k] = value;
                    nativeArray4[k] = value2;
                }
            }

            */
            
            // SPAWNABLE BUILDING DATA
            //if (nativeArray6.Length != 0)
            //{
                for (int m = 0; m < nativeArray6.Length; m++)
                {
                    Entity entity = nativeArray10[m];
                    BuildingPrefab prefab4 = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[m]);
                    BuildingPropertyData buildingPropertyData = ((nativeArray9.Length != 0) ? nativeArray9[m] : default(BuildingPropertyData)); // this seems to always have BPD in the chunk
                    SpawnableBuildingData spawnableBuildingData = nativeArray6[m];

                    // Skip the building if there is no zone for it
                    if (spawnableBuildingData.m_ZonePrefab == Entity.Null)
                    {
                        Mod.log.Warn($"{prefab4.name} has no Zone.");
                        continue;
                    }

                    Entity zonePrefabEntity = spawnableBuildingData.m_ZonePrefab;
                    ZoneData value3 = _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefabEntity];

                    // Not Signature - set spawnable group - NOT USED
                    /*
                    if (!flag2)
                    {
                        entityCommandBuffer.SetSharedComponent(e, new BuildingSpawnGroupData(value3.m_ZoneType));
                        ushort num = (ushort)math.clamp(Mathf.CeilToInt(nativeArray2[m].m_Size.y), 0, 65535);
                        if (spawnableBuildingData.m_Level == 1)
                        {
                            if (prefab4.m_LotWidth == 1 && (value3.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0)
                            {
                                value3.m_ZoneFlags |= ZoneFlags.SupportNarrow;
                                _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                            }
                            if (prefab4.m_AccessType == BuildingAccessType.LeftCorner && (value3.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == 0)
                            {
                                value3.m_ZoneFlags |= ZoneFlags.SupportLeftCorner;
                                _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                            }
                            if (prefab4.m_AccessType == BuildingAccessType.RightCorner && (value3.m_ZoneFlags & ZoneFlags.SupportRightCorner) == 0)
                            {
                                value3.m_ZoneFlags |= ZoneFlags.SupportRightCorner;
                                _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                            }
                            if (prefab4.m_AccessType == BuildingAccessType.Front && prefab4.m_LotWidth <= 3 && prefab4.m_LotDepth <= 2)
                            {
                                if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 3) && num < value3.m_MinOddHeight)
                                {
                                    value3.m_MinOddHeight = num;
                                    _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                                }
                                if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 2) && num < value3.m_MinEvenHeight)
                                {
                                    value3.m_MinEvenHeight = num;
                                    _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                                }
                            }
                        }
                        if (num > value3.m_MaxHeight)
                        {
                            value3.m_MaxHeight = num;
                            _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = value3;
                        }
                    }
                    */



                    // CORE MUST STAY
                    int level = spawnableBuildingData.m_Level;
                    BuildingData buildingData2 = nativeArray3[m];
                    int lotSize = buildingData2.m_LotSize.x * buildingData2.m_LotSize.y;

#if DEBUG
                Mod.LogIf($"BuildingReinit: {prefab4.name} level {level} lot {lotSize} zone {value3.m_AreaType} {(isSignature ? "Sign" : "Norm")}"); // {value3.m_ZoneFlags} hasBPD {nativeArray9.Length != 0}");
#endif

                // SKIP setting service consumption if Prefab has its own ServiceConsumption or Zone does not have it
                if (nativeArray5.Length == 0 || prefab4.Has<ServiceConsumption>() || !_Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefabEntity))
                    {
                        Mod.LogIf($"BuildingReinit: ... SKIP consData {nativeArray5.Length == 0} prefabHas {prefab4.Has<ServiceConsumption>()} zoneHas {_Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefabEntity)}");
                        continue;
                        // Seems to never happen. Each building has ConsumptionData, none Prefab has ServiceConsumption and each Zone has ZoneServiceConsumption
                    }


                    ZoneServiceConsumptionData zoneServiceConsumptionData = _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup[zonePrefabEntity];
                    ref ConsumptionData refConsData = ref nativeArray5.ElementAt(m);
                    int oldUpkeep = refConsData.m_Upkeep;
                    if (value3.m_AreaType == AreaType.Residential)
                    {
                        if (isSignature)
                        {
                            refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, zoneServiceConsumptionData.m_Upkeep, lotSize, value3.m_AreaType);
                        }
                        else
                        {
                            refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, value3.m_AreaType);
                        }
                    }
                    else
                    {
                        bool isStorage = buildingPropertyData.m_AllowedStored != Resource.NoResource;
                        if (isSignature)
                        {
                            refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, zoneServiceConsumptionData.m_Upkeep, lotSize, value3.m_AreaType);
                        }
                        else
                        {
                            refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, value3.m_AreaType, isStorage);
                        }
                    }

                    // Track upkeep changes
                    if (refConsData.m_Upkeep != oldUpkeep)
                    {
                        numUpkeep++;
                        Mod.LogIf($"{prefab4.name}.ConsumptionData.m_Upkeep: {oldUpkeep} -> {refConsData.m_Upkeep}");
                    }

                    // Get ZoneServiceConsumption and reinit buildinng
                    //if (m_PrefabSystem.TryGetPrefab<ZonePrefab>(zonePrefabEntity, out ZonePrefab zonePrefab))
                    //{
                        //Mod.log.Warn($"BuildingReinit: Failed to retrieve ZonePrefab from the PrefabSystem.");
                    //}
                    //else
                    //{
                        //ZoneServiceConsumption zoneServiceConsumption = zonePrefab.GetComponent<ZoneServiceConsumption>();
                        //zoneServiceConsumption.InitializeBuilding(EntityManager, entity, prefab4, spawnableBuildingData.m_Level);
                        refConsData.m_ElectricityConsumption = zoneServiceConsumptionData.m_ElectricityConsumption;
                        refConsData.m_WaterConsumption = zoneServiceConsumptionData.m_WaterConsumption;
                        refConsData.m_GarbageAccumulation = zoneServiceConsumptionData.m_GarbageAccumulation;
                        refConsData.m_TelecomNeed = zoneServiceConsumptionData.m_TelecomNeed;
                    //}


                    // SpaceMultiplier
                    ZonePropertiesData zonePropertiesData = _Game_Prefabs_ZonePropertiesData_RO_ComponentLookup[zonePrefabEntity];
                    ref BuildingPropertyData refPropData = ref nativeArray9.ElementAt(m);
                    float oldSpaceMult = refPropData.m_SpaceMultiplier;

                    float num = (zonePropertiesData.m_ScaleResidentials ? ((1f + 0.25f * (float)(level - 1)) * (float)lotSize) : 1f);
                    refPropData.m_ResidentialProperties = (int)math.round(num * zonePropertiesData.m_ResidentialProperties);
                    refPropData.m_AllowedSold = zonePropertiesData.m_AllowedSold;
                    refPropData.m_AllowedManufactured = zonePropertiesData.m_AllowedManufactured;
                    refPropData.m_AllowedStored = zonePropertiesData.m_AllowedStored;
                    refPropData.m_SpaceMultiplier = zonePropertiesData.m_SpaceMultiplier;

                    // Track space mult changes
                    if (refPropData.m_SpaceMultiplier != oldSpaceMult)
                    {
                        numSpaceMult++;
                        Mod.LogIf($"{prefab4.name}.BuildingPropertyData.m_SpaceMultiplier: {oldSpaceMult} -> {refPropData.m_SpaceMultiplier}");
                    }
                }
            //}


            // PLACEABLE OBJECT DATA + SERVICE UPGRADE - NOT USED
            /*
            if (nativeArray7.Length != 0)
            {
                if (nativeArray8.Length != 0)
                {
                    for (int n = 0; n < nativeArray7.Length; n++)
                    {
                        PlaceableObjectData value4 = nativeArray7[n];
                        ServiceUpgradeData serviceUpgradeData = nativeArray8[n];
                        if (nativeArray3.Length != 0)
                        {
                            value4.m_Flags |= Game.Objects.PlacementFlags.OwnerSide;
                        }
                        value4.m_ConstructionCost = serviceUpgradeData.m_UpgradeCost;
                        nativeArray7[n] = value4;
                    }
                }
                else
                {
                    for (int num2 = 0; num2 < nativeArray7.Length; num2++)
                    {
                        PlaceableObjectData value5 = nativeArray7[num2];
                        value5.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
                        nativeArray7[num2] = value5;
                    }
                }
            }
            */

            // COLLECTED SERVICE BUDGET - not sure if should stay or not - need testing
            // Probably for service buildings?
            /*
            if (flag)
            {
                for (int num3 = 0; num3 < nativeArray10.Length; num3++)
                {
                    if (nativeArray5.Length == 0 || nativeArray5[num3].m_Upkeep <= 0)
                    {
                        continue;
                    }
                    bool flag5 = false;
                    DynamicBuffer<ServiceUpkeepData> dynamicBuffer2 = bufferAccessor2[num3];
                    for (int num4 = 0; num4 < dynamicBuffer2.Length; num4++)
                    {
                        if (dynamicBuffer2[num4].m_Upkeep.m_Resource == Resource.Money)
                        {
                            log.WarnFormat("Warning: {0} has monetary upkeep in both ConsumptionData and CityServiceUpkeep", m_PrefabSystem.GetPrefab<PrefabBase>(nativeArray10[num3]).name);
                            ServiceUpkeepData value6 = dynamicBuffer2[num4];
                            value6.m_Upkeep.m_Amount += nativeArray5[num3].m_Upkeep;
                            dynamicBuffer2[num4] = value6;
                            flag5 = true;
                        }
                    }
                    if (!flag5)
                    {
                        dynamicBuffer2.Add(new ServiceUpkeepData
                        {
                            m_ScaleWithUsage = false,
                            m_Upkeep = new ResourceStack
                            {
                                m_Amount = nativeArray5[num3].m_Upkeep,
                                m_Resource = Resource.Money
                            }
                        });
                    }
                }
            }
            */

            // This sets ServiceUpkeepData - and uses upkeep from ConsumptionData - I am not sure if it should be updated
            // Comment out but do not remove - check if it is needed
            /*
            if (bufferAccessor.Length == 0)
            {
                continue;
            }
            for (int num5 = 0; num5 < bufferAccessor.Length; num5++)
            {
                Entity upgrade = nativeArray10[num5];
                DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer3 = bufferAccessor[num5];
                for (int num6 = 0; num6 < dynamicBuffer3.Length; num6++)
                {
                    ServiceUpgradeBuilding serviceUpgradeBuilding2 = dynamicBuffer3[num6];
                    base.EntityManager.GetBuffer<BuildingUpgradeElement>(serviceUpgradeBuilding2.m_Building).Add(new BuildingUpgradeElement(upgrade));
                }
                if (nativeArray5.Length != 0 && nativeArray5[num5].m_Upkeep > 0)
                {
                    bufferAccessor2[num5].Add(new ServiceUpkeepData
                    {
                        m_ScaleWithUsage = false,
                        m_Upkeep = new ResourceStack
                        {
                            m_Amount = nativeArray5[num5].m_Upkeep,
                            m_Resource = Resource.Money
                        }
                    });
                }
            }
            */
        }
        chunks.Dispose();
        entityCommandBuffer.Playback(base.EntityManager);
        entityCommandBuffer.Dispose();
        base.Enabled = false; // run only once
        Mod.log.Info($"BuildingReinitializeSystem: Upkeep changed for {numUpkeep} buildings.");
        Mod.log.Info($"BuildingReinitializeSystem: SpaceMultiplier changed for {numSpaceMult} buildings.");
    }

    /* not used
    private void InitializeLotSize(BuildingPrefab buildingPrefab, BuildingTerraformOverride terraformOverride, ref ObjectGeometryData objectGeometryData, ref BuildingTerraformData buildingTerraformData, ref BuildingData buildingData)
    {
        float2 @float = new float2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
        @float *= 8f;
        Bounds2 flatBounds;
        if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0)
        {
            buildingData.m_LotSize.x = Mathf.RoundToInt(objectGeometryData.m_LegSize.x / 8f);
            buildingData.m_LotSize.y = Mathf.RoundToInt(objectGeometryData.m_LegSize.z / 8f);
            float2 xz = objectGeometryData.m_Pivot.xz;
            float2 float2 = objectGeometryData.m_LegSize.xz * 0.5f;
            flatBounds = new Bounds2(xz - float2, xz + float2);
        }
        else
        {
            buildingData.m_LotSize = new int2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
            flatBounds = objectGeometryData.m_Bounds.xz;
        }
        Bounds2 lotBounds = default(Bounds2);
        lotBounds.max = (float2)buildingData.m_LotSize * 4f;
        lotBounds.min = -lotBounds.max;
        InitializeTerraformData(terraformOverride, ref buildingTerraformData, lotBounds, flatBounds);
        objectGeometryData.m_Layers |= MeshLayer.Default;
        objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(@float.x, 0f, @float.y))));
        switch (buildingPrefab.m_AccessType)
        {
        case BuildingAccessType.LeftCorner:
            buildingData.m_Flags |= BuildingFlags.LeftAccess;
            break;
        case BuildingAccessType.RightCorner:
            buildingData.m_Flags |= BuildingFlags.RightAccess;
            break;
        case BuildingAccessType.LeftAndRightCorner:
            buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess;
            break;
        case BuildingAccessType.LeftAndBackCorner:
            buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.BackAccess;
            break;
        case BuildingAccessType.RightAndBackCorner:
            buildingData.m_Flags |= BuildingFlags.RightAccess | BuildingFlags.BackAccess;
            break;
        case BuildingAccessType.FrontAndBack:
            buildingData.m_Flags |= BuildingFlags.BackAccess;
            break;
        case BuildingAccessType.All:
            buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess;
            break;
        }
        if (math.any(objectGeometryData.m_Size.xz > @float + 0.5f))
        {
            log.WarnFormat("Building geometry doesn't fit inside the lot ({0}): {1}m x {2}m ({3}x{4})", buildingPrefab.name, objectGeometryData.m_Size.x, objectGeometryData.m_Size.z, buildingData.m_LotSize.x, buildingData.m_LotSize.y);
        }
        @float -= 0.4f;
        objectGeometryData.m_Size.xz = @float;
        objectGeometryData.m_Size.y = math.max(objectGeometryData.m_Size.y, 5f);
        objectGeometryData.m_Bounds.min.xz = @float * -0.5f;
        objectGeometryData.m_Bounds.min.y = math.min(objectGeometryData.m_Bounds.min.y, 0f);
        objectGeometryData.m_Bounds.max.xz = @float * 0.5f;
        objectGeometryData.m_Bounds.max.y = math.max(objectGeometryData.m_Bounds.max.y, 5f);
    }

    public static void InitializeTerraformData(BuildingTerraformOverride terraformOverride, ref BuildingTerraformData buildingTerraformData, Bounds2 lotBounds, Bounds2 flatBounds)
    {
        float3 @float = new float3(1f, 0f, 1f);
        float3 float2 = new float3(1f, 0f, 1f);
        float3 float3 = new float3(1f, 0f, 1f);
        float3 float4 = new float3(1f, 0f, 1f);
        buildingTerraformData.m_Smooth.xy = lotBounds.min;
        buildingTerraformData.m_Smooth.zw = lotBounds.max;
        if (terraformOverride != null)
        {
            flatBounds.min += terraformOverride.m_LevelMinOffset;
            flatBounds.max += terraformOverride.m_LevelMaxOffset;
            @float.x = terraformOverride.m_LevelBackRight.x;
            @float.z = terraformOverride.m_LevelFrontRight.x;
            float2.x = terraformOverride.m_LevelBackRight.y;
            float2.z = terraformOverride.m_LevelBackLeft.y;
            float3.x = terraformOverride.m_LevelBackLeft.x;
            float3.z = terraformOverride.m_LevelFrontLeft.x;
            float4.x = terraformOverride.m_LevelFrontRight.y;
            float4.z = terraformOverride.m_LevelFrontLeft.y;
            buildingTerraformData.m_Smooth.xy += terraformOverride.m_SmoothMinOffset;
            buildingTerraformData.m_Smooth.zw += terraformOverride.m_SmoothMaxOffset;
            buildingTerraformData.m_HeightOffset = terraformOverride.m_HeightOffset;
            buildingTerraformData.m_DontRaise = terraformOverride.m_DontRaise;
            buildingTerraformData.m_DontLower = terraformOverride.m_DontLower;
        }
        float3 float5 = flatBounds.min.x + @float;
        float3 float6 = flatBounds.min.y + float2;
        float3 float7 = flatBounds.max.x - float3;
        float3 float8 = flatBounds.max.y - float4;
        float3 x = (float5 + float7) * 0.5f;
        float3 x2 = (float6 + float8) * 0.5f;
        buildingTerraformData.m_FlatX0 = math.min(float5, math.max(x, float7));
        buildingTerraformData.m_FlatZ0 = math.min(float6, math.max(x2, float8));
        buildingTerraformData.m_FlatX1 = math.max(float7, math.min(x, float5));
        buildingTerraformData.m_FlatZ1 = math.max(float8, math.min(x2, float6));
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
    */

    public BuildingReinitializeSystem()
    {
    }
}

/*

// Game.Prefabs.ZoneServiceConsumption

public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
{
    if (!buildingPrefab.Has<ServiceConsumption>())
    {
        entityManager.SetComponentData(entity, GetBuildingConsumptionData());
    }
}

private ConsumptionData GetBuildingConsumptionData()
{
    ConsumptionData result = default(ConsumptionData);
    result.m_Upkeep = 0;
    result.m_ElectricityConsumption = m_ElectricityConsumption;
    result.m_WaterConsumption = m_WaterConsumption;
    result.m_GarbageAccumulation = m_GarbageAccumulation;
    result.m_TelecomNeed = m_TelecomNeed;
    return result;
}

// Game.Prefabs.ZoneProperties

public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
{
    if (!buildingPrefab.Has<BuildingProperties>())
    {
        BuildingPropertyData buildingPropertyData = GetBuildingPropertyData(buildingPrefab, level);
        entityManager.SetComponentData(entity, buildingPropertyData);
    }
}

private BuildingPropertyData GetBuildingPropertyData(BuildingPrefab buildingPrefab, byte level)
{
    float num = (m_ScaleResidentials ? ((1f + 0.25f * (float)(level - 1)) * (float)buildingPrefab.lotSize) : 1f);
    BuildingPropertyData result = default(BuildingPropertyData);
    result.m_ResidentialProperties = (int)math.round(num * m_ResidentialProperties);
    result.m_AllowedSold = EconomyUtils.GetResources(m_AllowedSold);
    result.m_AllowedManufactured = EconomyUtils.GetResources(m_AllowedManufactured);
    result.m_AllowedStored = EconomyUtils.GetResources(m_AllowedStored);
    result.m_SpaceMultiplier = m_SpaceMultiplier;
    return result;
}

*/