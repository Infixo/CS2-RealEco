using System.Runtime.CompilerServices;
using Game.Common;
using Game.Economy;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ResourceSystem : GameSystemBase
{
    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

        public ComponentTypeHandle<ResourceData> __Game_Prefabs_ResourceData_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ResourceInfo> __Game_Economy_ResourceInfo_RO_ComponentTypeHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceData>();
            __Game_Economy_ResourceInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceInfo>(isReadOnly: true);
        }
    }

    private EntityQuery m_PrefabGroup;

    private EntityQuery m_InfoGroup;

    private PrefabSystem m_PrefabSystem;

    private NativeArray<Entity> m_ResourcePrefabs;

    private NativeArray<Entity> m_ResourceInfos;

    private JobHandle m_PrefabsReaders;

    private int m_BaseConsumptionSum;

    private TypeHandle __TypeHandle;

    public int BaseConsumptionSum => m_BaseConsumptionSum;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
        m_PrefabGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[3]
            {
                ComponentType.ReadOnly<Created>(),
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<ResourceData>()
            }
        });
        m_InfoGroup = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<ResourceInfo>());
        m_ResourcePrefabs = new NativeArray<Entity>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_ResourceInfos = new NativeArray<Entity>(EconomyUtils.ResourceCount, Allocator.Persistent);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_PrefabsReaders.Complete();
        m_ResourcePrefabs.Dispose();
        m_ResourceInfos.Dispose();
        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        m_PrefabsReaders.Complete();
        m_PrefabsReaders = default(JobHandle);
        if (!m_PrefabGroup.IsEmptyIgnoreFilter)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            NativeArray<ArchetypeChunk> nativeArray = m_PrefabGroup.ToArchetypeChunkArray(Allocator.TempJob);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            EntityTypeHandle _Unity_Entities_Entity_TypeHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<PrefabData> typeHandle = __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
            __TypeHandle.__Game_Prefabs_ResourceData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<ResourceData> typeHandle2 = __TypeHandle.__Game_Prefabs_ResourceData_RW_ComponentTypeHandle;
            float num = 0f;
            for (int i = 0; i < nativeArray.Length; i++)
            {
                ArchetypeChunk archetypeChunk = nativeArray[i];
                NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
                NativeArray<PrefabData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
                NativeArray<ResourceData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
                for (int j = 0; j < nativeArray4.Length; j++)
                {
                    Entity value = nativeArray2[j];
                    ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(nativeArray3[j]);
                    ResourceData resourceData = nativeArray4[j];
                    resourceData.m_IsMaterial = prefab.m_IsMaterial;
                    resourceData.m_IsProduceable = prefab.m_IsProduceable;
                    resourceData.m_IsTradable = prefab.m_IsTradable;
                    resourceData.m_IsLeisure = prefab.m_IsLeisure;
                    resourceData.m_Weight = prefab.m_Weight;
                    resourceData.m_Price = prefab.m_InitialPrice;
                    resourceData.m_WealthModifier = prefab.m_WealthModifier;
                    resourceData.m_BaseConsumption = prefab.m_BaseConsumption;
                    resourceData.m_ChildWeight = prefab.m_ChildWeight;
                    resourceData.m_TeenWeight = prefab.m_TeenWeight;
                    resourceData.m_AdultWeight = prefab.m_AdultWeight;
                    resourceData.m_ElderlyWeight = prefab.m_ElderlyWeight;
                    resourceData.m_CarConsumption = prefab.m_CarConsumption;
                    resourceData.m_RequireTemperature = prefab.m_RequireTemperature;
                    resourceData.m_RequiredTemperature = prefab.m_RequiredTemperature;
                    resourceData.m_RequireNaturalResource = prefab.m_RequireNaturalResource;
                    nativeArray4[j] = resourceData;
                    num += math.lerp(HouseholdBehaviorSystem.GetWeight(200, resourceData, 1, leisureIncluded: false), HouseholdBehaviorSystem.GetWeight(200, resourceData, 0, leisureIncluded: false), 0.2f);
                    int index = (int)(prefab.m_Resource - 1);
                    if (m_ResourcePrefabs[index] == Entity.Null)
                    {
                        m_ResourcePrefabs[index] = value;
                        m_ResourceInfos[index] = entityCommandBuffer.CreateEntity();
                        entityCommandBuffer.AddComponent(m_ResourceInfos[index], new ResourceInfo
                        {
                            m_Resource = EconomyUtils.GetResource(prefab.m_Resource)
                        });
                        entityCommandBuffer.AddComponent(m_ResourceInfos[index], default(Created));
                    }
                }
            }
            entityCommandBuffer.Playback(base.EntityManager);
            entityCommandBuffer.Dispose();
            m_BaseConsumptionSum = Mathf.RoundToInt(num);
            nativeArray.Dispose();
        }
        if (m_InfoGroup.IsEmptyIgnoreFilter)
        {
            return;
        }
        NativeArray<ArchetypeChunk> nativeArray5 = m_InfoGroup.ToArchetypeChunkArray(Allocator.TempJob);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        EntityTypeHandle _Unity_Entities_Entity_TypeHandle2 = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        __TypeHandle.__Game_Economy_ResourceInfo_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        ComponentTypeHandle<ResourceInfo> typeHandle3 = __TypeHandle.__Game_Economy_ResourceInfo_RO_ComponentTypeHandle;
        for (int k = 0; k < nativeArray5.Length; k++)
        {
            ArchetypeChunk archetypeChunk2 = nativeArray5[k];
            NativeArray<Entity> nativeArray6 = archetypeChunk2.GetNativeArray(_Unity_Entities_Entity_TypeHandle2);
            NativeArray<ResourceInfo> nativeArray7 = archetypeChunk2.GetNativeArray(ref typeHandle3);
            for (int l = 0; l < nativeArray6.Length; l++)
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(nativeArray7[l].m_Resource);
                if (resourceIndex >= 0 && m_ResourceInfos[resourceIndex] != nativeArray6[l])
                {
                    m_ResourceInfos[resourceIndex] = nativeArray6[l];
                }
            }
        }
        nativeArray5.Dispose();
    }

    public ResourcePrefabs GetPrefabs()
    {
        return new ResourcePrefabs(m_ResourcePrefabs);
    }

    public void AddPrefabsReader(JobHandle handle)
    {
        m_PrefabsReaders = JobHandle.CombineDependencies(m_PrefabsReaders, handle);
    }

    public Entity GetPrefab(Resource resource)
    {
        return m_ResourcePrefabs[EconomyUtils.GetResourceIndex(resource)];
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
    public ResourceSystem()
    {
    }
}
