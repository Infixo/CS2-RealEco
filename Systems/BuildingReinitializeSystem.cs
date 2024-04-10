using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Collections;
using Game;
using Game.Prefabs;
using Game.Economy;
using Game.Simulation;
using Game.Zones;

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

        [ReadOnly]
        public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

        public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

        //[ReadOnly]
        public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RW_ComponentTypeHandle;

        public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

        //[ReadOnly]
        //public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;

        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
            __Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
            __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>();
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>();
            __Game_Prefabs_ZoneData_RW_ComponentLookup = state.GetComponentLookup<ZoneData>();
            __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ZoneServiceConsumptionData>(isReadOnly: true);
            __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
            //__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(isReadOnly: true);
        }
    }

    private EntityQuery m_PrefabQuery;

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
        RequireForUpdate(m_PrefabQuery);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        Mod.log.Info("BuildingReinitializeSystem created.");
    }

    protected override void OnUpdate()
    {
        Mod.log.Info($"BuildingReinitializeSystem.OnUpdate: {m_PrefabQuery.CalculateEntityCount()}");
        int numUpkeep = 0, numSpaceMult = 0;
        NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        EntityTypeHandle _Unity_Entities_Entity_TypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<PrefabData> typeHandle = __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingData> typeHandle2 = __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ConsumptionData> typeHandle5 = __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SpawnableBuildingData> typeHandle7 = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<SignatureBuildingData> typeHandle8 = __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<BuildingPropertyData> typeHandle11 = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RW_ComponentTypeHandle;
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
                BuildingPrefab prefab = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[m]);
                BuildingPropertyData buildingPropertyData = ((nativeArray9.Length != 0) ? nativeArray9[m] : default(BuildingPropertyData)); // this seems to always have BPD in the chunk
                SpawnableBuildingData spawnableBuildingData = nativeArray6[m];

                // Skip the building if there is no zone for it
                if (spawnableBuildingData.m_ZonePrefab == Entity.Null)
                {
                    Mod.log.Warn($"{prefab.name} has no Zone.");
                    continue;
                }

                // Retrieve zone data
                Entity zonePrefabEntity = spawnableBuildingData.m_ZonePrefab;
                ZoneData zoneData = _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefabEntity];
                int level = spawnableBuildingData.m_Level;
                BuildingData buildingData = nativeArray3[m];
                int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;

#if DEBUG
                Mod.LogIf($"{prefab.name}.Reinit: lv {level} lot {lotSize} zone {zoneData.m_AreaType} {(isSignature ? "Sign" : "Norm")}"); // {value3.m_ZoneFlags} hasBPD {nativeArray9.Length != 0}");
#endif

                // SKIP setting service consumption if Prefab has its own ServiceConsumption or Zone does not have it
                if (nativeArray5.Length == 0 || prefab.Has<ServiceConsumption>() || !_Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefabEntity))
                {
                    Mod.LogIf($"BuildingReinit: ... SKIP consData {nativeArray5.Length == 0} prefabHas {prefab.Has<ServiceConsumption>()} zoneHas {_Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefabEntity)}");
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
                    Mod.LogIf($"{prefab.name}.m_Upkeep: {oldUpkeep} -> {refConsData.m_Upkeep}");
                }
#if DEBUG
                else
                    Mod.LogIf($"{prefab.name}.m_Upkeep: {oldUpkeep} -> SAME");
#endif

                // Other data in ZoneServiceConsumptionData
                refConsData.m_ElectricityConsumption = zoneServiceConsumptionData.m_ElectricityConsumption;
                refConsData.m_WaterConsumption = zoneServiceConsumptionData.m_WaterConsumption;
                refConsData.m_GarbageAccumulation = zoneServiceConsumptionData.m_GarbageAccumulation;
                refConsData.m_TelecomNeed = zoneServiceConsumptionData.m_TelecomNeed;

                // SpaceMultiplier in case it is inherited from ZonePrefab
                ref BuildingPropertyData refPropData = ref nativeArray9.ElementAt(m);
                if (!prefab.Has<BuildingProperties>() || // default case - no BuildingProperties
                    refPropData.m_AllowedSold == Resource.Lodging || // hotels & motels - Commercial Zone
                    refPropData.m_AllowedSold == Resource.Petrochemicals // gas stations - Commercial Zone
                    )
                {
                    ZonePropertiesData zonePropertiesData = _Game_Prefabs_ZonePropertiesData_RO_ComponentLookup[zonePrefabEntity];
                    float oldSpaceMult = refPropData.m_SpaceMultiplier;

                    /* 240410 Fix for motels and gas stations seling everything. Cannot copy all data from zones here, need more logic.
                    float num = (zonePropertiesData.m_ScaleResidentials ? ((1f + 0.25f * (float)(level - 1)) * (float)lotSize) : 1f);
                    refPropData.m_ResidentialProperties = (int)math.round(num * zonePropertiesData.m_ResidentialProperties);
                    refPropData.m_AllowedSold = zonePropertiesData.m_AllowedSold;
                    refPropData.m_AllowedManufactured = zonePropertiesData.m_AllowedManufactured;
                    refPropData.m_AllowedStored = zonePropertiesData.m_AllowedStored;
                    */
                    refPropData.m_SpaceMultiplier = zonePropertiesData.m_SpaceMultiplier;

                    // Track space mult changes
                    if (refPropData.m_SpaceMultiplier != oldSpaceMult)
                    {
                        numSpaceMult++;
                        Mod.LogIf($"{prefab.name}.m_SpaceMultiplier: {oldSpaceMult} -> {refPropData.m_SpaceMultiplier}");
                    }
#if DEBUG
                    else
                        Mod.LogIf($"{prefab.name}.m_SpaceMultiplier: {oldSpaceMult} -> SAME");
#endif
                }
#if DEBUG
                else
                {
                    Mod.LogIf($"{prefab.name}.m_SpaceMultiplier: SKIP");
                }
#endif
            } // chunk loop
        } // archetype chunk loop

        chunks.Dispose();
        base.Enabled = false; // run only once
        Mod.log.Info($"BuildingReinitializeSystem: Upkeep changed for {numUpkeep} buildings.");
        Mod.log.Info($"BuildingReinitializeSystem: SpaceMultiplier changed for {numSpaceMult} buildings.");
    }

    public BuildingReinitializeSystem()
    {
    }
}
