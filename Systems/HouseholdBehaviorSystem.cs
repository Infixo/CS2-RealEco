using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace RealEco.Systems;

[CompilerGenerated]
public partial class HouseholdBehaviorSystem : GameSystemBase
{
    private struct ProcessConsumptionJob : IJob
    {
        public NativeQueue<ResourceStack> m_Queue;

        public NativeArray<int> m_ConsumptionAccumulator;

        public NativeQueue<int> m_DebugResourceQueue;

        public NativeArray<int> m_DebugResourceCounter;

        public NativeArray<int> m_DebugResourceCounter2;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        public void Execute()
        {
            //Plugin.Log($"ProcessConsumptionJob: {m_Queue.Count}");
            int2 @int = default(int2);
            ResourceStack item;
            while (m_Queue.TryDequeue(out item))
            {
                m_ConsumptionAccumulator[EconomyUtils.GetResourceIndex(item.m_Resource)] += item.m_Amount;
                float price = m_ResourceDatas[m_ResourcePrefabs[item.m_Resource]].m_Price;
                @int.x += Mathf.RoundToInt((float)item.m_Amount * price);
            }
            int item2;
            while (m_DebugResourceQueue.TryDequeue(out item2))
            {
                @int.y += item2;
            }
            m_DebugResourceCounter[0] += @int.x;
            m_DebugResourceCounter2[0] += @int.y;
        }
    }


    //[BurstCompile]
    private struct HouseholdTickJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        public ComponentTypeHandle<Household> m_HouseholdType;

        public ComponentTypeHandle<HouseholdNeed> m_HouseholdNeedType;

        [ReadOnly]
        public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

        public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        public ComponentTypeHandle<TouristHousehold> m_TouristHouseholdType;

        [ReadOnly]
        public ComponentTypeHandle<CommuterHousehold> m_CommuterHouseholdType;

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

        [ReadOnly]
        public ComponentTypeHandle<LodgingSeeker> m_LodgingSeekerType;

        [ReadOnly]
        public ComponentTypeHandle<HomelessHousehold> m_HomelessHouseholdType;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;

        [ReadOnly]
        public ComponentLookup<PropertySeeker> m_PropertySeekers;

        [ReadOnly]
        public ComponentLookup<Worker> m_Workers;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<LodgingProvider> m_LodgingProviders;

        [ReadOnly]
        public ComponentLookup<HealthProblem> m_HealthProblems;

        [ReadOnly]
        public ComponentLookup<Population> m_Populations;

        [ReadOnly]
        public ComponentLookup<Citizen> m_CitizenDatas;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        public RandomSeed m_RandomSeed;

        public EconomyParameterData m_EconomyParameters;

        public NativeQueue<ResourceStack>.ParallelWriter m_ConsumptionQueue;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public uint m_UpdateFrameIndex;

        public int m_BaseConsumptionSum;

        public Entity m_City;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int>.ParallelWriter m_DebugWealthQueue;

        public bool m_DebugWealthQueueIsCreated;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int>.ParallelWriter m_DebugResourcesQueue;

        public bool m_DebugResourcesQueueIsCreated;

        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int>.ParallelWriter m_DebugConsumptionQueue;

        public bool m_DebugConsumptionQueueIsCreated;

        public NativeQueue<int>.ParallelWriter m_DebugResourceQueue;

        private bool NeedsCar(Entity household, int wealth, float commute, int cars)
        {
            if (cars > 0 || wealth < 0)
            {
                return false;
            }
            int num = new Unity.Mathematics.Random((uint)(household.Index + 1)).NextInt(100);
            return (float)math.min(100, wealth / 100) + 0.02f * commute > (float)num;
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            //Plugin.Log($"HouseholdTickJob, frame {m_UpdateFrameIndex}: {chunk.Count}");
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Household> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdType);
            NativeArray<HouseholdNeed> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdNeedType);
            BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
            BufferAccessor<Game.Economy.Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceType);
            NativeArray<TouristHousehold> nativeArray4 = chunk.GetNativeArray(ref m_TouristHouseholdType);
            NativeArray<PropertyRenter> nativeArray5 = chunk.GetNativeArray(ref m_PropertyRenterType);
            Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            int population = m_Populations[m_City].m_Population;
            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = nativeArray[i];
                Household household = nativeArray2[i];
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
                if (dynamicBuffer.Length == 0)
                {
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
                    continue;
                }
                DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor2[i];
                HouseholdNeed value = nativeArray3[i];
                PropertyRenter propertyRenter = ((nativeArray5.Length > 0) ? nativeArray5[i] : default(PropertyRenter));
                int householdWealth = EconomyUtils.GetHouseholdWealth(entity, household, resources, bufferAccessor[i], ref m_Workers, ref m_CitizenDatas, ref m_HealthProblems, propertyRenter, ref m_EconomyParameters, m_ResourcePrefabs, ref m_ResourceDatas, m_BaseConsumptionSum, m_TaxRates);
                if (m_DebugWealthQueueIsCreated)
                {
                    m_DebugWealthQueue.Enqueue(math.clamp(householdWealth, -20000, 20000));
                }
                if (m_DebugResourcesQueueIsCreated)
                {
                    m_DebugResourcesQueue.Enqueue(math.clamp(household.m_Resources, -20000, 20000));
                }
                float num = GetConsumptionMultiplier(Mathf.RoundToInt((float)householdWealth / math.max(1f, bufferAccessor[i].Length))) * m_EconomyParameters.m_ResourceConsumption * (float)dynamicBuffer.Length;
                //Plugin.Log($"HouseholdTickJob1 {entity.Index}: had {household.m_Resources} needs {value.m_Amount} of {value.m_Resource} wealth {householdWealth} cons_mult {num}");
                if (chunk.Has(ref m_TouristHouseholdType))
                {
                    num *= m_EconomyParameters.m_TouristConsumptionMultiplier;
                    if (!chunk.Has(ref m_LodgingSeekerType))
                    {
                        TouristHousehold value2 = nativeArray4[i];
                        if (value2.m_Hotel.Equals(Entity.Null))
                        {
                            m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
                        }
                        else if (!m_LodgingProviders.HasComponent(value2.m_Hotel))
                        {
                            value2.m_Hotel = Entity.Null;
                            nativeArray4[i] = value2;
                            m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
                        }
                    }
                }
                household.m_LastConsumption = (short)Mathf.RoundToInt(256f * num);
                if (m_DebugConsumptionQueueIsCreated)
                {
                    m_DebugConsumptionQueue.Enqueue(household.m_LastConsumption / dynamicBuffer.Length);
                }
                int num2 = Mathf.FloorToInt(num);
                num -= (float)num2;
                m_DebugResourceQueue.Enqueue(num2);
                if (random.NextFloat() < num)
                {
                    num2++;
                }
                household.m_Resources -= num2;
                //Plugin.Log($"HouseholdTickJob2 {entity.Index}: used {num2} has {household.m_Resources} cons_mult {num}");
                if (value.m_Resource == Resource.NoResource)
                {
                    int carCount = 0;
                    if (m_OwnedVehicles.HasBuffer(entity))
                    {
                        carCount = m_OwnedVehicles[entity].Length;
                    }
                    if (household.m_Resources < 0)
                    {
                        //Plugin.Log($"HouseholdTickJob entity {entity.Index}: no resources, create need");
                        float lastCommutePerCitizen = GetLastCommutePerCitizen(dynamicBuffer, m_Workers);
                        int cars = 0;
                        if (m_OwnedVehicles.HasBuffer(entity))
                        {
                            cars = m_OwnedVehicles[entity].Length;
                        }
                        if (NeedsCar(entity, householdWealth, lastCommutePerCitizen, cars))
                        {
                            value.m_Resource = Resource.Vehicles;
                            value.m_Amount = kCarAmount;
                            nativeArray3[i] = value;
                            nativeArray2[i] = household;
                            // Infixo: why break? it needs a car, but there is no consumption
                            // it registers as HouseholdNeed but when is it fulfilled?
                            break;
                        }
                        // Infixo: this is probably for reducing traffic?
                        // but it cannot work, eventually every household that lacks resources WILL send someone for shopping
                        //int num3 = math.min(80, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(m_EconomyParameters.m_TrafficReduction * (float)population))));
                        //if (random.NextInt(100) < num3)
                        //{
                            ResourceIterator iterator = ResourceIterator.GetIterator();
                            int num4 = 0;
                            while (iterator.Next())
                            {
                                num4 += GetWeight(householdWealth, iterator.resource, m_ResourcePrefabs, ref m_ResourceDatas, carCount, leisureIncluded: false, dynamicBuffer, ref m_CitizenDatas);
                            }
                            int num5 = random.NextInt(num4);
                            iterator = ResourceIterator.GetIterator();
                            while (iterator.Next())
                            {
                                int weight = GetWeight(householdWealth, iterator.resource, m_ResourcePrefabs, ref m_ResourceDatas, carCount, leisureIncluded: false, dynamicBuffer, ref m_CitizenDatas);
                                num4 -= weight;
                                if (weight > 0 && num4 <= num5)
                                {
                                    // 240306 money
                                    int moneyPerCim = EconomyUtils.GetResources(Resource.Money, resources) / dynamicBuffer.Length;
                                    // Infixo: the core part where actual consumption need is created and inserted into the queue
                                    // then it gets accumulated and processed by other systems, mainly CommercialDemandSystem
                                    value.m_Resource = iterator.resource;
                                    float price = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]].m_Price;
                                    value.m_Amount = Mathf.CeilToInt((float)(math.clamp(moneyPerCim, 1000, 3000) * dynamicBuffer.Length) / price); // 240306 buy more resources, default is 2000
                                    nativeArray3[i] = value;
                                    nativeArray2[i] = household;
                                    m_ConsumptionQueue.Enqueue(new ResourceStack
                                    {
                                        m_Resource = value.m_Resource,
                                        // Infixo: actual fix is to keep consumption at the same level as needs
                                        //m_Amount = Mathf.RoundToInt((float)value.m_Amount / (0.01f * (float)num3))
                                        m_Amount = value.m_Amount
                                    });
                                    //int amount = Mathf.RoundToInt((float)value.m_Amount / (0.01f * (float)num3));
                                    //Plugin.Log($"HouseholdTickJob3 {entity.Index}: cims {dynamicBuffer.Length} needs {value.m_Amount} of {value.m_Resource} at price {price} money {moneyPerCim} wealth {householdWealth}");//, consumption {amount}, num3 {num3}, traf {m_EconomyParameters.m_TrafficReduction}, pop {population}");
                                    return;
                                }
                            }
                        // } // Infixo: removed virtual traffic reduction
                    }
                }
                int max = math.clamp(Mathf.RoundToInt(0.06f * (float)population), 64, 1024);
                if (!chunk.Has(ref m_TouristHouseholdType) && !chunk.Has(ref m_CommuterHouseholdType) && !m_PropertySeekers.HasComponent(nativeArray[i]) && (!chunk.Has(ref m_PropertyRenterType) || (chunk.Has(ref m_HomelessHouseholdType) && random.NextInt(128) == 0) || random.NextInt(max) == 0))
                {
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(PropertySeeker));
                }
                nativeArray2[i] = household;
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

        public ComponentTypeHandle<HouseholdNeed> __Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle;

        public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

        public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<LodgingSeeker> __Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
            __Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdNeed>();
            __Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
            __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
            __Game_Citizens_TouristHousehold_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>();
            __Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommuterHousehold>(isReadOnly: true);
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingSeeker>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
            __Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HomelessHousehold>(isReadOnly: true);
            __Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
            __Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
            __Game_Agents_PropertySeeker_RO_ComponentLookup = state.GetComponentLookup<PropertySeeker>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Companies_LodgingProvider_RO_ComponentLookup = state.GetComponentLookup<LodgingProvider>(isReadOnly: true);
            __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            __Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
        }
    }

    public static readonly int kCarAmount = 50;

    private EntityQuery m_HouseholdGroup;

    private EntityQuery m_EconomyParameterGroup;

    private SimulationSystem m_SimulationSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private ResourceSystem m_ResourceSystem;

    private TaxSystem m_TaxSystem;

    private CountConsumptionSystem m_CountConsumptionSystem;

    private CitySystem m_CitySystem;

    private NativeQueue<ResourceStack> m_ConsumptionQueue;

    [DebugWatchDeps]
    private JobHandle m_ConsumptionQueueDeps;

    [DebugWatchValue]
    private DebugWatchDistribution m_DebugWealth;

    [DebugWatchValue]
    private DebugWatchDistribution m_DebugConsumption;

    [DebugWatchValue]
    private DebugWatchDistribution m_DebugResources;

    [DebugWatchValue]
    private NativeArray<int> m_DebugResourceCounter;

    [DebugWatchValue]
    private NativeArray<int> m_DebugResourceCounter2;

    private NativeQueue<int> m_DebugResourceQueue;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 64;
    }

    public static float GetLastCommutePerCitizen(DynamicBuffer<HouseholdCitizen> householdCitizens, ComponentLookup<Worker> workers)
    {
        float num = 0f;
        float num2 = 0f;
        for (int i = 0; i < householdCitizens.Length; i++)
        {
            Entity citizen = householdCitizens[i].m_Citizen;
            if (workers.HasComponent(citizen))
            {
                num2 += workers[citizen].m_LastCommuteTime;
            }
            num += 1f;
        }
        return num2 / num;
    }

    public static float GetConsumptionMultiplier(int wealth)
    {
        return 0.3f + 2.2f * math.smoothstep(0f, 1f, (float)(wealth + 1000) / 6000f);
    }

    public static int GetHouseholdExpectedIncome(DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Game.Citizens.Student> students, ref ComponentLookup<HealthProblem> healthProblems, ref ComponentLookup<Citizen> citizenDatas, ref EconomyParameterData economyParameters, NativeArray<int> taxRates, NativeArray<int> unemployment)
    {
        float num = 0f;
        for (int i = 0; i < citizens.Length; i++)
        {
            Entity citizen = citizens[i].m_Citizen;
            if (CitizenUtils.IsDead(citizen, ref healthProblems))
            {
                continue;
            }
            Citizen citizen2 = citizenDatas[citizen];
            switch (citizen2.GetAge())
            {
            case CitizenAge.Child:
                num += (float)economyParameters.m_FamilyAllowance;
                continue;
            case CitizenAge.Elderly:
                num += (float)economyParameters.m_Pension;
                continue;
            }
            int num2 = citizen2.GetEducationLevel();
            if (students.HasComponent(citizen))
            {
                num2++;
            }
            float num3 = economyParameters.GetWage(num2);
            float num4 = num3 - (float)economyParameters.m_ResidentialMinimumEarnings;
            if (num4 > 0f)
            {
                num3 -= (float)Mathf.RoundToInt(num4 * ((float)TaxSystem.GetResidentialTaxRate(num2, taxRates) / 100f));
            }
            num3 = math.lerp(num3, economyParameters.m_UnemploymentBenefit, 0.01f * (float)unemployment[num2]);
            num += num3;
        }
        return Mathf.RoundToInt(num);
    }

    public static int GetHouseholdExpectedIncomeDefault(DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Game.Citizens.Student> students, ref ComponentLookup<HealthProblem> healthProblems, ref ComponentLookup<Citizen> citizenDatas, ref EconomyParameterData economyParameters)
    {
        float num = 0f;
        for (int i = 0; i < citizens.Length; i++)
        {
            Entity citizen = citizens[i].m_Citizen;
            if (CitizenUtils.IsDead(citizen, ref healthProblems))
            {
                continue;
            }
            Citizen citizen2 = citizenDatas[citizen];
            switch (citizen2.GetAge())
            {
            case CitizenAge.Child:
                num += (float)economyParameters.m_FamilyAllowance;
                continue;
            case CitizenAge.Elderly:
                num += (float)economyParameters.m_Pension;
                continue;
            }
            int num2 = citizen2.GetEducationLevel();
            if (students.HasComponent(citizen))
            {
                num2++;
            }
            float num3 = economyParameters.GetWage(num2);
            float num4 = num3 - (float)economyParameters.m_ResidentialMinimumEarnings;
            if (num4 > 0f)
            {
                num3 -= (float)Mathf.RoundToInt(num4 * 0.1f);
            }
            num3 = math.lerp(num3, economyParameters.m_UnemploymentBenefit, 0.1f);
            num += num3;
        }
        return Mathf.RoundToInt(num);
    }

    public static int GetHouseholdIncome(DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Worker> workers, ref ComponentLookup<Citizen> citizenDatas, ref ComponentLookup<HealthProblem> healthProblems, ref EconomyParameterData economyParameters, NativeArray<int> taxRates)
    {
        int num = 0;
        for (int i = 0; i < citizens.Length; i++)
        {
            Entity citizen = citizens[i].m_Citizen;
            if (CitizenUtils.IsDead(citizen, ref healthProblems))
            {
                continue;
            }
            CitizenAge age = citizenDatas[citizen].GetAge();
            if (workers.HasComponent(citizen))
            {
                int level = workers[citizen].m_Level;
                int wage = economyParameters.GetWage(level);
                num += wage;
                int num2 = wage - economyParameters.m_ResidentialMinimumEarnings;
                if (num2 > 0)
                {
                    num -= Mathf.RoundToInt((float)num2 * ((float)TaxSystem.GetResidentialTaxRate(level, taxRates) / 100f));
                }
                continue;
            }
            switch (age)
            {
            case CitizenAge.Child:
            case CitizenAge.Teen:
                num += economyParameters.m_FamilyAllowance;
                break;
            case CitizenAge.Elderly:
                num += economyParameters.m_Pension;
                break;
            default:
                num += economyParameters.m_UnemploymentBenefit;
                break;
            }
        }
        return Mathf.RoundToInt(num);
    }

    public static int GetHouseholdIncomeDefaultTax(DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Worker> workers, ref ComponentLookup<HealthProblem> healthProblems, ref ComponentLookup<Citizen> citizenDatas, ref EconomyParameterData economyParameters)
    {
        int num = 0;
        for (int i = 0; i < citizens.Length; i++)
        {
            Entity citizen = citizens[i].m_Citizen;
            if (CitizenUtils.IsDead(citizen, ref healthProblems))
            {
                continue;
            }
            CitizenAge age = citizenDatas[citizen].GetAge();
            if (workers.HasComponent(citizen))
            {
                int level = workers[citizen].m_Level;
                int wage = economyParameters.GetWage(level);
                num += wage;
                int num2 = wage - economyParameters.m_ResidentialMinimumEarnings;
                if (num2 > 0)
                {
                    num -= Mathf.RoundToInt((float)num2 * 0.1f);
                }
                continue;
            }
            switch (age)
            {
            case CitizenAge.Child:
            case CitizenAge.Teen:
                num += economyParameters.m_FamilyAllowance;
                break;
            case CitizenAge.Elderly:
                num += economyParameters.m_Pension;
                break;
            default:
                num += economyParameters.m_UnemploymentBenefit;
                break;
            }
        }
        return Mathf.RoundToInt(num);
    }

    public static int EstimateHouseholdDailyExpenses(int familySize, ref EconomyParameterData economyParameters, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, float baseConsumptionSum)
    {
        float num = 0f;
        ResourceIterator iterator = ResourceIterator.GetIterator();
        while (iterator.Next())
        {
            float num2 = (float)familySize * GetCitizenDailyConsumption(iterator.resource, ref economyParameters, resourcePrefabs, ref resourceDatas, baseConsumptionSum);
            if (num2 > 0f)
            {
                num += EconomyUtils.GetMarketPrice(iterator.resource, resourcePrefabs, ref resourceDatas) * num2;
            }
        }
        return Mathf.RoundToInt(num);
    }

    public static float GetCitizenDailyConsumption(Resource resource, ref EconomyParameterData parameters, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, float baseConsumptionSum)
    {
        int num = Mathf.RoundToInt(128f * (parameters.m_ResourceConsumption / EconomyUtils.GetMarketPrice(resource, resourcePrefabs, ref resourceDatas)) * GetConsumptionMultiplier(900));
        float num2 = math.lerp(GetWeight(900, resource, resourcePrefabs, ref resourceDatas, 1, leisureIncluded: true), GetWeight(900, resource, resourcePrefabs, ref resourceDatas, 0, leisureIncluded: true), 0.2f);
        return (float)num * num2 / baseConsumptionSum;
    }

    public static bool GetFreeCar(Entity household, BufferLookup<OwnedVehicle> ownedVehicles, ComponentLookup<Game.Vehicles.PersonalCar> personalCars, ref Entity car)
    {
        if (ownedVehicles.HasBuffer(household))
        {
            DynamicBuffer<OwnedVehicle> dynamicBuffer = ownedVehicles[household];
            for (int i = 0; i < dynamicBuffer.Length; i++)
            {
                car = dynamicBuffer[i].m_Vehicle;
                if (personalCars.HasComponent(car) && personalCars[car].m_Keeper.Equals(Entity.Null))
                {
                    return true;
                }
            }
        }
        car = Entity.Null;
        return false;
    }

    public static int GetTouristDailyConsumption(Resource resource, ref EconomyParameterData parameters, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, float baseConsumptionSum)
    {
        if (resource == Resource.Lodging)
        {
            return 1;
        }
        int num = Mathf.RoundToInt(1024f * parameters.m_ResourceConsumption / EconomyUtils.GetMarketPrice(resource, resourcePrefabs, ref resourceDatas) * GetConsumptionMultiplier(900));
        float num2 = math.lerp(GetWeight(200, resource, resourcePrefabs, ref resourceDatas, 1, leisureIncluded: true), GetWeight(200, resource, resourcePrefabs, ref resourceDatas, 0, leisureIncluded: true), 0.2f);
        return Mathf.RoundToInt(parameters.m_TouristConsumptionMultiplier * (float)num * num2 / baseConsumptionSum);
    }

    public static int GetTotalDailyConsumption(Resource resource, ref EconomyParameterData parameters, int population, int averageTourists, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas, float baseConsumptionSum)
    {
        float num = averageTourists * GetTouristDailyConsumption(resource, ref parameters, resourcePrefabs, ref resourceDatas, baseConsumptionSum);
        float num2 = (float)(1 + population) * GetCitizenDailyConsumption(resource, ref parameters, resourcePrefabs, ref resourceDatas, baseConsumptionSum);
        return Mathf.RoundToInt(num + num2);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_DebugResourceCounter = new NativeArray<int>(1, Allocator.Persistent);
        m_DebugResourceCounter2 = new NativeArray<int>(1, Allocator.Persistent);
        m_DebugResourceQueue = new NativeQueue<int>(Allocator.Persistent);
        m_DebugWealth = new DebugWatchDistribution(persistent: true);
        m_DebugConsumption = new DebugWatchDistribution(persistent: true);
        m_DebugResources = new DebugWatchDistribution(persistent: true);
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountConsumptionSystem = base.World.GetOrCreateSystemManaged<CountConsumptionSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_EconomyParameterGroup = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_HouseholdGroup = GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<HouseholdNeed>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_ConsumptionQueue = new NativeQueue<ResourceStack>(Allocator.Persistent);
        RequireForUpdate(m_HouseholdGroup);
        RequireForUpdate(m_EconomyParameterGroup);
        Mod.log.Info("Modded HouseholdBehaviorSystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_DebugResourceCounter.Dispose();
        m_DebugResourceCounter2.Dispose();
        m_DebugResourceQueue.Dispose();
        m_DebugWealth.Dispose();
        m_DebugResources.Dispose();
        m_DebugConsumption.Dispose();
        m_ConsumptionQueue.Dispose();
        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
        __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Agents_PropertySeeker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        HouseholdTickJob householdTickJob = default(HouseholdTickJob);
        householdTickJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        householdTickJob.m_HouseholdType = __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle;
        householdTickJob.m_HouseholdNeedType = __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle;
        householdTickJob.m_ResourceType = __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle;
        householdTickJob.m_HouseholdCitizenType = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;
        householdTickJob.m_TouristHouseholdType = __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle;
        householdTickJob.m_CommuterHouseholdType = __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle;
        householdTickJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        householdTickJob.m_LodgingSeekerType = __TypeHandle.__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle;
        householdTickJob.m_PropertyRenterType = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
        householdTickJob.m_HomelessHouseholdType = __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle;
        householdTickJob.m_Workers = __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup;
        householdTickJob.m_OwnedVehicles = __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        householdTickJob.m_EconomyParameters = m_EconomyParameterGroup.GetSingleton<EconomyParameterData>();
        householdTickJob.m_PropertySeekers = __TypeHandle.__Game_Agents_PropertySeeker_RO_ComponentLookup;
        householdTickJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        householdTickJob.m_LodgingProviders = __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup;
        householdTickJob.m_CitizenDatas = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
        householdTickJob.m_HealthProblems = __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup;
        householdTickJob.m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
        householdTickJob.m_TaxRates = m_TaxSystem.GetTaxRates();
        householdTickJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
        householdTickJob.m_RandomSeed = RandomSeed.Next();
        householdTickJob.m_BaseConsumptionSum = m_ResourceSystem.BaseConsumptionSum;
        householdTickJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        householdTickJob.m_UpdateFrameIndex = updateFrameWithInterval;
        householdTickJob.m_ConsumptionQueue = m_ConsumptionQueue.AsParallelWriter();
        householdTickJob.m_City = m_CitySystem.City;
        householdTickJob.m_DebugResourceQueue = m_DebugResourceQueue.AsParallelWriter();
        HouseholdTickJob jobData = householdTickJob;
        if (m_DebugWealth.IsEnabled)
        {
            jobData.m_DebugWealthQueue = m_DebugWealth.GetQueue(clear: false, out var deps).AsParallelWriter();
            jobData.m_DebugWealthQueueIsCreated = true;
            deps.Complete();
        }
        if (m_DebugConsumption.IsEnabled)
        {
            jobData.m_DebugConsumptionQueue = m_DebugConsumption.GetQueue(clear: false, out var deps2).AsParallelWriter();
            jobData.m_DebugConsumptionQueueIsCreated = true;
            deps2.Complete();
        }
        if (m_DebugResources.IsEnabled)
        {
            jobData.m_DebugResourcesQueue = m_DebugResources.GetQueue(clear: false, out var deps3).AsParallelWriter();
            jobData.m_DebugResourcesQueueIsCreated = true;
            deps3.Complete();
        }
        JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdGroup, JobHandle.CombineDependencies(m_ConsumptionQueueDeps, base.Dependency));
        m_CountConsumptionSystem.AddConsumptionWriter(jobHandle);
        m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
        m_ResourceSystem.AddPrefabsReader(jobHandle);
        m_TaxSystem.AddReader(jobHandle);
        __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        ProcessConsumptionJob processConsumptionJob = default(ProcessConsumptionJob);
        processConsumptionJob.m_Queue = m_ConsumptionQueue;
        processConsumptionJob.m_ConsumptionAccumulator = m_CountConsumptionSystem.GetConsumptionAccumulator(out var deps4);
        processConsumptionJob.m_DebugResourceQueue = m_DebugResourceQueue;
        processConsumptionJob.m_DebugResourceCounter = m_DebugResourceCounter;
        processConsumptionJob.m_DebugResourceCounter2 = m_DebugResourceCounter2;
        processConsumptionJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        processConsumptionJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
        ProcessConsumptionJob jobData2 = processConsumptionJob;
        base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, deps4));
        m_ConsumptionQueueDeps = base.Dependency;
        m_CountConsumptionSystem.AddConsumptionWriter(base.Dependency);
    }

    public static int GetAgeWeight(ResourceData resourceData, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
    {
        int num = 0;
        for (int i = 0; i < citizens.Length; i++)
        {
            Entity citizen = citizens[i].m_Citizen;
            num = citizenDatas[citizen].GetAge() switch
            {
                CitizenAge.Child => num + resourceData.m_ChildWeight, 
                CitizenAge.Teen => num + resourceData.m_TeenWeight, 
                CitizenAge.Elderly => num + resourceData.m_ElderlyWeight, 
                _ => num + resourceData.m_AdultWeight, 
            };
        }
        return num;
    }

    public static int GetWeight(int wealth, Resource resource, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, int carCount, bool leisureIncluded, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
    {
        ResourceData resourceData = resourceDatas[resourcePrefabs[resource]];
        return GetWeight(wealth, resourceData, carCount, leisureIncluded, citizens, ref citizenDatas);
    }

    public static int GetWeight(int wealth, ResourceData resourceData, int carCount, bool leisureIncluded, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
    {
        float num = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_BaseConsumption : 0f);
        num += (float)(carCount * resourceData.m_CarConsumption);
        float a = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_WealthModifier : 0f);
        float num2 = GetAgeWeight(resourceData, citizens, ref citizenDatas);
        return Mathf.RoundToInt(100f * num2 * num * math.smoothstep(a, 1f, math.max(0.01f, ((float)wealth + 5000f) / 10000f)));
    }

    public static int GetWeight(int wealth, Resource resource, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, int carCount, bool leisureIncluded)
    {
        ResourceData resourceData = resourceDatas[resourcePrefabs[resource]];
        return GetWeight(wealth, resourceData, carCount, leisureIncluded);
    }

    public static int GetWeight(int wealth, ResourceData resourceData, int carCount, bool leisureIncluded)
    {
        float num = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_BaseConsumption : 0f) + (float)(carCount * resourceData.m_CarConsumption);
        float a = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_WealthModifier : 0f);
        return Mathf.RoundToInt(num * math.smoothstep(a, 1f, math.clamp(((float)wealth + 5000f) / 10000f, 0.1f, 0.9f)));
    }

    public static int GetHighestEducation(DynamicBuffer<HouseholdCitizen> citizenBuffer, ref ComponentLookup<Citizen> citizens)
    {
        int num = 0;
        for (int i = 0; i < citizenBuffer.Length; i++)
        {
            Entity citizen = citizenBuffer[i].m_Citizen;
            if (citizens.HasComponent(citizen))
            {
                Citizen citizen2 = citizens[citizen];
                CitizenAge age = citizen2.GetAge();
                if (age == CitizenAge.Teen || age == CitizenAge.Adult)
                {
                    num = math.max(num, citizen2.GetEducationLevel());
                }
            }
        }
        return num;
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
    public HouseholdBehaviorSystem()
    {
    }
}
