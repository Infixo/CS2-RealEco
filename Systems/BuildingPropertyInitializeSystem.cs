using Unity.Collections;
using Unity.Entities;
using Game;
using Game.Prefabs;
using Game.Economy;

namespace RealEco.Systems;

/* 
240405 This system adds Immaterial resources to AllowedSold for commercial and mixed buildings
Unfortunately this info is already is passed from Zones to individual buildings so they need to be patched also.

0111111111000000001101000000011100101100 - zone resources
0110001111000000000101000000011100101100 - building resources

The query will check: 
- PrefabData
- BuildingPropertyData => or just here change AllowedSold BUT some have specific info (see below)
- SpawnableBuildingData => m_ZonePrefab check zone via ZoneData component and m_AreaType = Commercial or Mixed
Exclude: SignatureBuildingData
Exclude: OfficeBuilding

Exceptions:
- petrochemicals (gas stations)
- lodging (hotels & motels)
*/

public partial class BuildingPropertyInitializeSystem : GameSystemBase
{
    private EntityQuery m_PrefabQuery;

    private PrefabSystem m_PrefabSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
        m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[3]
            {
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadWrite<BuildingPropertyData>(),
            },
            None = new ComponentType[2]
            {
                ComponentType.ReadOnly<SignatureBuildingData>(),
                ComponentType.ReadOnly<OfficeBuilding>(),
            }
        });
        RequireForUpdate(m_PrefabQuery);
        Mod.log.Info("BuildingPropertyInitializeSystem created.");
    }

    protected override void OnUpdate()
    {
        int num = 0;
        Mod.log.Info($"BuildingPropertyInitializeSystem.OnUpdate: {m_PrefabQuery.CalculateEntityCount()}");
        foreach (Entity entity in m_PrefabQuery.ToEntityArray(Allocator.Temp))
        {
            if (m_PrefabSystem.TryGetPrefab<BuildingPrefab>(entity, out BuildingPrefab prefab))
            {
                //Mod.log.Info($"BuildingPropertyInitializeSystem: {prefab.name}");

                BuildingPropertyData buildingPropertyData = EntityManager.GetComponentData<BuildingPropertyData>(entity);

                if (buildingPropertyData.m_AllowedSold == Resource.NoResource || // non-commercial
                    buildingPropertyData.m_AllowedSold == Resource.Petrochemicals || // gas stations
                    buildingPropertyData.m_AllowedSold == Resource.Lodging) // hotels & motels
                    continue;

                buildingPropertyData.m_AllowedSold |= (Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media);

                // Update entity's buffer with a brand and an affiliated brand
                EntityManager.SetComponentData<BuildingPropertyData>(entity, buildingPropertyData);
                Mod.LogIf($"BuildingPropertyInitializeSystem: {prefab.name} patched");
                num++;
            }
            else
            {
                Mod.log.Warn($"BuildingPropertyInitializeSystem: Failed to retrieve CompanyPrefab from the PrefabSystem.");
            }
        }
        base.Enabled = false;
        Mod.Log($"BuildingPropertyInitializeSystem: Patched {num} buildings.");
    }

    public BuildingPropertyInitializeSystem()
    {
    }
}
