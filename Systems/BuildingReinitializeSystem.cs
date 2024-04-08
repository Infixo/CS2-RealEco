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
- TBD:  ZonePollution => PollutionData
It also recalculates Upkeep for buildings to use Upkeep values from Zones.
Unfortunately this info is already is passed from Zones to individual buildings so they need to be patched also.
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
                ComponentType.ReadWrite<BuildingData>(), // needed for a lot size, 2558 buildings - this is the superset that contains SpawnableBuilding and BuildingProperty
                ComponentType.ReadWrite<SpawnableBuildingData>(), // 2371 buildings
                ComponentType.ReadWrite<BuildingPropertyData>() // 2371 buildings, it is always there
            },
            //Any = new ComponentType[2]
            //{
                //ComponentType.ReadWrite<BuildingExtensionData>(),
                //ComponentType.ReadWrite<ServiceUpgradeData>(), // these are all building extensions for city services
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
        //EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
        NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        EntityTypeHandle _Unity_Entities_Entity_TypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<PrefabData> typeHandle = __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingData> typeHandle2 = __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<BuildingExtensionData> typeHandle3 = __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<BuildingTerraformData> typeHandle4 = __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ConsumptionData> typeHandle5 = __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<ObjectGeometryData> typeHandle6 = __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SpawnableBuildingData> typeHandle7 = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SignatureBuildingData> typeHandle8 = __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<PlaceableObjectData> typeHandle9 = __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<ServiceUpgradeData> typeHandle10 = __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingPropertyData> typeHandle11 = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<WaterPoweredData> typeHandle12 = __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<SewageOutletData> typeHandle13 = __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        //BufferTypeHandle<ServiceUpgradeBuilding> bufferTypeHandle = __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;
        //__TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        //ComponentTypeHandle<CollectedServiceBuildingBudgetData> typeHandle14 = __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;
        //__TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        //BufferTypeHandle<ServiceUpkeepData> bufferTypeHandle2 = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;
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
            NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
            NativeArray<ConsumptionData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
            NativeArray<SpawnableBuildingData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle7);
            NativeArray<BuildingPropertyData> nativeArray9 = archetypeChunk.GetNativeArray(ref typeHandle11);
            NativeArray<Entity> nativeArray10 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
            bool isSignature = archetypeChunk.Has(ref typeHandle8);
            
            // SPAWNABLE BUILDING DATA
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

                // Retrieve zone data
                Entity zonePrefabEntity = spawnableBuildingData.m_ZonePrefab;
                ZoneData zoneData = _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefabEntity];
                int level = spawnableBuildingData.m_Level;
                BuildingData buildingData = nativeArray3[m];
                int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;

#if DEBUG
                Mod.LogIf($"{prefab4.name}.Reinit: lv {level} lot {lotSize} zone {zoneData.m_AreaType} {(isSignature ? "Sign" : "Norm")}"); // {value3.m_ZoneFlags} hasBPD {nativeArray9.Length != 0}");
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
                if (zoneData.m_AreaType == AreaType.Residential)
                {
                    if (isSignature)
                    {
                        refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType);
                    }
                    else
                    {
                        refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType);
                    }
                }
                else
                {
                    bool isStorage = buildingPropertyData.m_AllowedStored != Resource.NoResource;
                    if (isSignature)
                    {
                        refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType);
                    }
                    else
                    {
                        refConsData.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, isStorage);
                    }
                }

                // Track upkeep changes
                if (refConsData.m_Upkeep != oldUpkeep)
                {
                    numUpkeep++;
                    Mod.LogIf($"{prefab4.name}.ConsumptionData.m_Upkeep: {oldUpkeep} -> {refConsData.m_Upkeep}");
                }

                // Other data in ZoneServiceConsumptionData
                refConsData.m_ElectricityConsumption = zoneServiceConsumptionData.m_ElectricityConsumption;
                refConsData.m_WaterConsumption = zoneServiceConsumptionData.m_WaterConsumption;
                refConsData.m_GarbageAccumulation = zoneServiceConsumptionData.m_GarbageAccumulation;
                refConsData.m_TelecomNeed = zoneServiceConsumptionData.m_TelecomNeed;

                // SpaceMultiplier in case it is inherited from ZonePrefab
                ref BuildingPropertyData refPropData = ref nativeArray9.ElementAt(m);
                if (!prefab4.Has<BuildingProperties>() || // default case - no BuildingProperties
                    refPropData.m_AllowedSold == Resource.Lodging || // hotels & motels - Commercial Zone
                    refPropData.m_AllowedSold == Resource.Petrochemicals // gas stations - Commercial Zone
                    )
                {
                    ZonePropertiesData zonePropertiesData = _Game_Prefabs_ZonePropertiesData_RO_ComponentLookup[zonePrefabEntity];
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
                        Mod.LogIf($"{prefab4.name}.m_SpaceMultiplier: {oldSpaceMult} -> {refPropData.m_SpaceMultiplier}");
                    }
                }
#if DEBUG
                else
                {
                    Mod.LogIf($"{prefab4.name}.m_SpaceMultiplier: SKIP");
                }
#endif
            } // chunk loop
        } // archetype chunk loop

        chunks.Dispose();
        //entityCommandBuffer.Playback(base.EntityManager);
        //entityCommandBuffer.Dispose();
        base.Enabled = false; // run only once
        Mod.log.Info($"BuildingReinitializeSystem: Upkeep changed for {numUpkeep} buildings.");
        Mod.log.Info($"BuildingReinitializeSystem: SpaceMultiplier changed for {numSpaceMult} buildings.");
    }
    public BuildingReinitializeSystem()
    {
    }
}
