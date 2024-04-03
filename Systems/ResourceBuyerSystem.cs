#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace RealEco.Systems;

[CompilerGenerated]
public partial class ResourceBuyerSystem : GameSystemBase
{
    [Flags]
    private enum SaleFlags : byte
    {
        None = 0,
        CommercialSeller = 1,
        ImportFromOC = 2,
        Virtual = 4
    }

    private struct SalesEvent
    {
        public SaleFlags m_Flags;

        public Entity m_Buyer;

        public Entity m_Seller;

        public Resource m_Resource;

        public int m_Amount;

        public float m_Distance;
    }


    //[BurstCompile]
    private struct BuyJob : IJob
    {
        public NativeQueue<SalesEvent> m_SalesQueue;

        public EconomyParameterData m_EconomyParameters;

        public BufferLookup<Game.Economy.Resources> m_Resources;

        public ComponentLookup<TaxPayer> m_TaxPayers;

        public ComponentLookup<ServiceAvailable> m_Services;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<Household> m_Households;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<BuyingCompany> m_BuyingCompanies;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_TransformDatas;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public ComponentLookup<ServiceCompanyData> m_ServiceCompanies;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [ReadOnly]
        public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

        public BufferLookup<TradeCost> m_TradeCosts;

        [ReadOnly]
        public ComponentLookup<BuildingData> m_BuildingDatas;

        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

        [ReadOnly]
        public BufferLookup<Employee> m_Employees;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_Spawnables;

        [ReadOnly]
        public BufferLookup<Efficiency> m_BuildingEfficiencies;

        [ReadOnly]
        public ComponentLookup<WorkProvider> m_WorkProviders;

        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public PersonalCarSelectData m_PersonalCarSelectData;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        [ReadOnly]
        public ComponentLookup<CurrentDistrict> m_Districts;

        [ReadOnly]
        public BufferLookup<DistrictModifier> m_DistrictModifiers;

        [ReadOnly]
        public ComponentLookup<Population> m_PopulationData;

        public Entity m_PopulationEntity;

        public RandomSeed m_RandomSeed;

        public EntityCommandBuffer m_CommandBuffer;

        public void Execute()
        {
            Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
            int population = m_PopulationData[m_PopulationEntity].m_Population;
            SalesEvent item;
            while (m_SalesQueue.TryDequeue(out item))
            {
                if (!m_Resources.HasBuffer(item.m_Buyer))
                {
                    continue;
                }
                bool flag = (item.m_Flags & SaleFlags.CommercialSeller) != 0;
                float num = (float)item.m_Amount * (flag ? EconomyUtils.GetMarketPrice(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas) : EconomyUtils.GetManufacturePrice(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas, ref m_EconomyParameters));
                if (m_TradeCosts.HasBuffer(item.m_Seller))
                {
                    DynamicBuffer<TradeCost> costs = m_TradeCosts[item.m_Seller];
                    TradeCost tradeCost = EconomyUtils.GetTradeCost(item.m_Resource, costs);
                    num += (float)item.m_Amount * tradeCost.m_BuyCost;
                    float weight = EconomyUtils.GetWeight(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
                    Assert.IsTrue(item.m_Amount != -1);
                    float num2 = (float)EconomyUtils.GetTransportCost(item.m_Distance, item.m_Resource, item.m_Amount, weight) / (1f + (float)item.m_Amount);
                    TradeCost tradeCost2 = default(TradeCost);
                    if (m_TradeCosts.HasBuffer(item.m_Buyer))
                    {
                        tradeCost2 = EconomyUtils.GetTradeCost(item.m_Resource, m_TradeCosts[item.m_Buyer]);
                    }
                    if (!m_OutsideConnections.HasComponent(item.m_Seller) && (item.m_Flags & SaleFlags.CommercialSeller) != 0)
                    {
                        tradeCost.m_SellCost = math.lerp(tradeCost.m_SellCost, num2 + tradeCost2.m_SellCost, 0.5f);
                        EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs, keepLastTime: true);
                    }
                    if (m_TradeCosts.HasBuffer(item.m_Buyer) && !m_OutsideConnections.HasComponent(item.m_Buyer))
                    {
                        tradeCost2.m_BuyCost = math.lerp(tradeCost2.m_BuyCost, num2 + tradeCost.m_BuyCost, 0.5f);
                        EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, m_TradeCosts[item.m_Buyer], keepLastTime: true);
                    }
                }
                if (m_Resources.HasBuffer(item.m_Seller) && EconomyUtils.GetResources(item.m_Resource, m_Resources[item.m_Seller]) <= 0)
                {
                    continue;
                }
                int num3 = 0;
                float num4 = 1f;
                num3 = TaxSystem.GetIndustrialTaxRate(item.m_Resource, m_TaxRates);
                if (flag && m_Services.HasComponent(item.m_Seller) && m_PropertyRenters.HasComponent(item.m_Seller))
                {
                    num4 = 0.01f * math.min(80f, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(m_EconomyParameters.m_TrafficReduction * (float)population))));
                    Entity prefab = m_Prefabs[item.m_Seller].m_Prefab;
                    ServiceAvailable value = m_Services[item.m_Seller];
                    ServiceCompanyData serviceCompanyData = m_ServiceCompanies[prefab];
                    num *= EconomyUtils.GetServicePriceMultiplier(value.m_ServiceAvailable, serviceCompanyData.m_MaxService);
                    value.m_ServiceAvailable = math.max(0, Mathf.RoundToInt((float)value.m_ServiceAvailable - (float)item.m_Amount / num4));
                    if (value.m_MeanPriority > 0f)
                    {
                        value.m_MeanPriority = math.min(1f, math.lerp(value.m_MeanPriority, (float)value.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService, 0.1f));
                    }
                    else
                    {
                        value.m_MeanPriority = math.min(1f, (float)value.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService);
                    }
                    m_Services[item.m_Seller] = value;
                    Entity property = m_PropertyRenters[item.m_Seller].m_Property;
                    if (m_Districts.HasComponent(property))
                    {
                        Entity district = m_Districts[property].m_District;
                        num3 = TaxSystem.GetModifiedCommercialTaxRate(item.m_Resource, m_TaxRates, district, m_DistrictModifiers);
                    }
                    else
                    {
                        num3 = TaxSystem.GetCommercialTaxRate(item.m_Resource, m_TaxRates);
                    }
                }
                if (m_Resources.HasBuffer(item.m_Seller))
                {
                    DynamicBuffer<Game.Economy.Resources> resources = m_Resources[item.m_Seller];
                    int resources2 = EconomyUtils.GetResources(item.m_Resource, resources);
                    EconomyUtils.AddResources(item.m_Resource, -math.min(resources2, Mathf.RoundToInt((float)item.m_Amount / num4)), resources);
                    //Plugin.Log($"ResourceBuy1 {item.m_Seller.Index}->{item.m_Buyer.Index} {item.m_Amount} {item.m_Resource}: seller stock {-math.min(resources2, Mathf.RoundToInt((float)item.m_Amount / num4))}");
                }
                EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(num), m_Resources[item.m_Buyer]);
                //Plugin.Log($"ResourceBuy2 {item.m_Seller.Index}->{item.m_Buyer.Index} {item.m_Amount} {item.m_Resource}: buyer money {-Mathf.RoundToInt(num)} flags {item.m_Flags}");
                if (m_Households.HasComponent(item.m_Buyer))
                {
                    Household value2 = m_Households[item.m_Buyer];
                    value2.m_Resources += Mathf.RoundToInt(num);
                    m_Households[item.m_Buyer] = value2;
                    //Plugin.Log($"ResourceBuy3 {item.m_Seller.Index}->{item.m_Buyer.Index} {item.m_Amount} {item.m_Resource}: household {Mathf.RoundToInt(num)}");
                }
                else if (m_BuyingCompanies.HasComponent(item.m_Buyer))
                {
                    BuyingCompany value3 = m_BuyingCompanies[item.m_Buyer];
                    value3.m_LastTradePartner = item.m_Seller;
                    m_BuyingCompanies[item.m_Buyer] = value3;
                    // 240309 FIX FOR VANISHING RESOURCES AND BANKRUPTING COMMERCIALS
                    // This is necessary ALWAYS to get the resources we've just paid for
                    //if ((item.m_Flags & SaleFlags.Virtual) != 0)
                    //{
                        EconomyUtils.AddResources(item.m_Resource, item.m_Amount, m_Resources[item.m_Buyer]);
                        //Plugin.Log($"ResourceBuy4 {item.m_Seller.Index}->{item.m_Buyer.Index} {item.m_Amount} {item.m_Resource}: buyer stock {item.m_Amount}");
                    //}
                }
                if (m_TaxPayers.HasComponent(item.m_Seller))
                {
                    TaxPayer value4 = m_TaxPayers[item.m_Seller];
                    if (m_PropertyRenters.HasComponent(item.m_Seller) && m_WorkProviders.HasComponent(item.m_Seller))
                    {
                        Entity prefab2 = m_Prefabs[item.m_Seller].m_Prefab;
                        Entity property2 = m_PropertyRenters[item.m_Seller].m_Property;
                        Entity prefab3 = m_Prefabs[property2].m_Prefab;
                        BuildingData buildingData = m_BuildingDatas[prefab3];
                        IndustrialProcessData processData = m_ProcessDatas[prefab2];
                        DynamicBuffer<Employee> employees = m_Employees[item.m_Seller];
                        WorkplaceData workplaceData = m_WorkplaceDatas[prefab2];
                        SpawnableBuildingData spawnableData = m_Spawnables[prefab3];
                        WorkProvider workProvider = m_WorkProviders[item.m_Seller];
                        if (m_Services.HasComponent(item.m_Seller))
                        {
                            ServiceCompanyData serviceData = m_ServiceCompanies[prefab2];
                            float efficiency = BuildingUtils.GetEfficiency(property2, ref m_BuildingEfficiencies);
                            DynamicBuffer<TradeCost> tradeCosts = m_TradeCosts[item.m_Seller];
                            float num5 = ServiceCompanySystem.EstimateDailyProduction(efficiency, workProvider.m_MaxWorkers, spawnableData.m_Level, serviceData, workplaceData, ref m_EconomyParameters, processData.m_Output.m_Resource, buildingData.m_LotSize.x * buildingData.m_LotSize.y);
                            float num6 = ServiceCompanySystem.EstimateDailyProfit(efficiency, workProvider.m_MaxWorkers, employees, m_Services[item.m_Seller], serviceData, buildingData, processData, ref m_EconomyParameters, workplaceData, spawnableData, m_ResourcePrefabs, m_ResourceDatas, tradeCosts);
                            if (num5 > 0f)
                            {
                                float num7 = num6 / num5;
                                int num8 = Mathf.CeilToInt(math.max(0f, num7 * (float)item.m_Amount));
                                value4.m_UntaxedIncome += num8;
                                if (num8 > 0)
                                {
                                    value4.m_AverageTaxRate = Mathf.RoundToInt(math.lerp(value4.m_AverageTaxRate, num3, (float)num8 / (float)(num8 + value4.m_UntaxedIncome)));
                                }
                                m_TaxPayers[item.m_Seller] = value4;
                            }
                        }
                    }
                }
                if (!m_Storages.HasComponent(item.m_Seller) && m_PropertyRenters.HasComponent(item.m_Seller))
                {
                    DynamicBuffer<Game.Economy.Resources> resources3 = m_Resources[item.m_Seller];
                    EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt(num / num4), resources3);
                    //Plugin.Log($"ResourceBuy5 {item.m_Seller.Index}->{item.m_Buyer.Index} {item.m_Amount} {item.m_Resource}: seller money {Mathf.RoundToInt(num / num4)}");
                }
                if (item.m_Resource != Resource.Vehicles || item.m_Amount != HouseholdBehaviorSystem.kCarAmount || !m_PropertyRenters.HasComponent(item.m_Seller))
                {
                    continue;
                }
                Entity property3 = m_PropertyRenters[item.m_Seller].m_Property;
                if (m_TransformDatas.HasComponent(property3) && m_HouseholdCitizens.HasBuffer(item.m_Buyer))
                {
                    Entity buyer = item.m_Buyer;
                    Game.Objects.Transform transform = m_TransformDatas[property3];
                    int length = m_HouseholdCitizens[buyer].Length;
                    int num9 = (m_HouseholdAnimals.HasBuffer(buyer) ? m_HouseholdAnimals[buyer].Length : 0);
                    int passengerAmount;
                    int num10;
                    if (m_OwnedVehicles.HasBuffer(buyer) && m_OwnedVehicles[buyer].Length >= 1)
                    {
                        passengerAmount = random.NextInt(1, 1 + length);
                        num10 = random.NextInt(1, 2 + num9);
                    }
                    else
                    {
                        passengerAmount = length;
                        num10 = 1 + num9;
                    }
                    if (random.NextInt(20) == 0)
                    {
                        num10 += 5;
                    }
                    Entity entity = m_PersonalCarSelectData.CreateVehicle(m_CommandBuffer, ref random, passengerAmount, num10, avoidTrailers: true, noSlowVehicles: false, transform, property3, Entity.Null, (PersonalCarFlags)0u, stopped: true);
                    if (entity != Entity.Null)
                    {
                        m_CommandBuffer.AddComponent(entity, new Owner(buyer));
                        m_CommandBuffer.AddBuffer<OwnedVehicle>(buyer);
                    }
                }
            }
        }
    }


    //[BurstCompile]
    private struct HandleBuyersJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> m_BuyerType;

        [ReadOnly]
        public ComponentTypeHandle<ResourceBought> m_BoughtType;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        public BufferTypeHandle<TripNeeded> m_TripType;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<CreatureData> m_CreatureDataType;

        [ReadOnly]
        public ComponentTypeHandle<ResidentData> m_ResidentDataType;

        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> m_AttendingMeetingType;

        [ReadOnly]
        public ComponentLookup<PathInformation> m_PathInformation;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_Properties;

        [ReadOnly]
        public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

        [ReadOnly]
        public ComponentLookup<CarKeeper> m_CarKeepers;

        [ReadOnly]
        public ComponentLookup<ParkedCar> m_ParkedCarData;

        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

        [ReadOnly]
        public ComponentLookup<Target> m_Targets;

        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> m_HouseholdMembers;

        [ReadOnly]
        public ComponentLookup<Household> m_Households;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> m_TouristHouseholds;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;

        [ReadOnly]
        public ComponentLookup<Worker> m_Workers;

        [ReadOnly]
        public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

        [ReadOnly]
        public BufferLookup<Game.Economy.Resources> m_Resources;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<CoordinatedMeeting> m_CoordinatedMeetings;

        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetingDatas;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<CarData> m_PrefabCarData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<HumanData> m_PrefabHumanData;

        [ReadOnly]
        public float m_TimeOfDay;

        [ReadOnly]
        public RandomSeed m_RandomSeed;

        [ReadOnly]
        public ComponentTypeSet m_PathfindTypes;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HumanChunks;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

        public NativeQueue<SalesEvent>.ParallelWriter m_SalesQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<ResourceBuyer> nativeArray2 = chunk.GetNativeArray(ref m_BuyerType);
            NativeArray<ResourceBought> nativeArray3 = chunk.GetNativeArray(ref m_BoughtType);
            BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
            NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<AttendingMeeting> nativeArray5 = chunk.GetNativeArray(ref m_AttendingMeetingType);
            for (int i = 0; i < nativeArray3.Length; i++)
            {
                Entity e = nativeArray[i]; // Entity = ResourceBought
                ResourceBought resourceBought = nativeArray3[i];
                if (m_PrefabRefData.HasComponent(resourceBought.m_Payer) && m_PrefabRefData.HasComponent(resourceBought.m_Seller))
                {
                    SalesEvent salesEvent = default(SalesEvent);
                    salesEvent.m_Amount = resourceBought.m_Amount;
                    salesEvent.m_Buyer = resourceBought.m_Payer;
                    salesEvent.m_Seller = resourceBought.m_Seller;
                    salesEvent.m_Resource = resourceBought.m_Resource;
                    salesEvent.m_Flags = SaleFlags.None;
                    salesEvent.m_Distance = resourceBought.m_Distance;
                    SalesEvent value = salesEvent;
                    m_SalesQueue.Enqueue(value);
                    //Plugin.Log($"SalesEvent1 {salesEvent.m_Seller.Index}->{salesEvent.m_Buyer.Index} {salesEvent.m_Amount} {salesEvent.m_Resource}: flags {salesEvent.m_Flags} dist {salesEvent.m_Distance}");
                }
                m_CommandBuffer.RemoveComponent<ResourceBought>(unfilteredChunkIndex, e);
            }
            for (int j = 0; j < nativeArray2.Length; j++)
            {
                ResourceBuyer resourceBuyer = nativeArray2[j];
                Entity entity = nativeArray[j]; // Entity = ResourceBuyer & TripNeeded
                DynamicBuffer<TripNeeded> dynamicBuffer = bufferAccessor[j];
                bool flag = false; // immaterial resource
                Entity entity2 = m_ResourcePrefabs[resourceBuyer.m_ResourceNeeded];
                if (m_ResourceDatas.HasComponent(entity2))
                {
                    flag = EconomyUtils.GetWeight(resourceBuyer.m_ResourceNeeded, m_ResourcePrefabs, ref m_ResourceDatas) == 0f;
                }
                if (m_PathInformation.HasComponent(entity))
                {
                    PathInformation pathInformation = m_PathInformation[entity];
                    if ((pathInformation.m_State & PathFlags.Pending) != 0)
                    {
                        continue;
                    }
                    Entity destination = pathInformation.m_Destination;
                    if (m_Properties.HasComponent(destination) || m_OutsideConnections.HasComponent(destination))
                    {
                        if (m_ServiceAvailables.HasComponent(destination))
                        {
                            ServiceAvailable serviceAvailable = m_ServiceAvailables[destination];
                            Entity prefab = m_PrefabRefData[destination].m_Prefab;
                            ServiceCompanyData serviceCompanyData = m_ServiceCompanyDatas[prefab];
                            float servicePriceMultiplier = EconomyUtils.GetServicePriceMultiplier(serviceAvailable.m_ServiceAvailable, serviceCompanyData.m_MaxService);
                            resourceBuyer.m_AmountNeeded = Mathf.RoundToInt((float)resourceBuyer.m_AmountNeeded / servicePriceMultiplier);
                        }
                        DynamicBuffer<Game.Economy.Resources> resources = m_Resources[destination];
                        int resources2 = EconomyUtils.GetResources(resourceBuyer.m_ResourceNeeded, resources);
                        if (resourceBuyer.m_AmountNeeded < 2 * resources2)
                        {
                            resourceBuyer.m_AmountNeeded = math.min(resourceBuyer.m_AmountNeeded, resources2);
                            SaleFlags saleFlags = (m_ServiceAvailables.HasComponent(destination) ? SaleFlags.CommercialSeller : SaleFlags.None);
                            if (m_OutsideConnections.HasComponent(destination))
                            {
                                saleFlags |= SaleFlags.ImportFromOC;
                            }
                            SalesEvent salesEvent = default(SalesEvent);
                            salesEvent.m_Amount = resourceBuyer.m_AmountNeeded;
                            salesEvent.m_Buyer = resourceBuyer.m_Payer;
                            salesEvent.m_Seller = destination;
                            salesEvent.m_Resource = resourceBuyer.m_ResourceNeeded;
                            salesEvent.m_Flags = saleFlags;
                            salesEvent.m_Distance = pathInformation.m_Distance;
                            SalesEvent value2 = salesEvent;
                            m_SalesQueue.Enqueue(value2);
                            //Plugin.Log($"SalesEvent2 {salesEvent.m_Seller.Index}->{salesEvent.m_Buyer.Index} {salesEvent.m_Amount} {salesEvent.m_Resource}: flags {salesEvent.m_Flags} dist {salesEvent.m_Distance}");
                            m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
                            m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
							// 240309 Cims will also visit new shops for shopping
                            //if (!flag) // not immaterial
                            //{
                                TripNeeded elem = default(TripNeeded);
                                elem.m_TargetAgent = destination;
                                elem.m_Purpose = Purpose.Shopping;
                                elem.m_Data = resourceBuyer.m_AmountNeeded;
                                elem.m_Resource = resourceBuyer.m_ResourceNeeded;
                                dynamicBuffer.Add(elem);
                                if (!m_Targets.HasComponent(nativeArray[j]))
                                {
                                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Target
                                    {
                                        m_Target = destination
                                    });
                                }
                                //Plugin.Log($"TripNeeded {entity.Index}->{destination.Index} {elem.m_Data} {elem.m_Resource}: {elem.m_Purpose}");
                            //}
                        }
                        else
                        {
                            m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
                            m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                        }
                        continue;
                    }
                    m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                    m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
                    if (nativeArray5.IsCreated)
                    {
                        AttendingMeeting attendingMeeting = nativeArray5[j];
                        Entity prefab2 = m_PrefabRefData[attendingMeeting.m_Meeting].m_Prefab;
                        CoordinatedMeeting value3 = m_CoordinatedMeetings[attendingMeeting.m_Meeting];
                        if (m_HaveCoordinatedMeetingDatas[prefab2][value3.m_Phase].m_Purpose.m_Purpose == Purpose.Shopping)
                        {
                            value3.m_Status = MeetingStatus.Done;
                            m_CoordinatedMeetings[attendingMeeting.m_Meeting] = value3;
                        }
                    }
                }
                else if ((!m_HouseholdMembers.HasComponent(entity) || (!m_TouristHouseholds.HasComponent(m_HouseholdMembers[entity].m_Household) && !m_CommuterHouseholds.HasComponent(m_HouseholdMembers[entity].m_Household))) && m_CurrentBuildings.HasComponent(entity) && m_OutsideConnections.HasComponent(m_CurrentBuildings[entity].m_CurrentBuilding) && !nativeArray5.IsCreated)
                {
                    // IMPORT FROM OUTSIDE CONNECTION
                    SaleFlags flags = SaleFlags.ImportFromOC;
                    SalesEvent salesEvent = default(SalesEvent);
                    salesEvent.m_Amount = resourceBuyer.m_AmountNeeded;
                    salesEvent.m_Buyer = resourceBuyer.m_Payer;
                    salesEvent.m_Seller = m_CurrentBuildings[entity].m_CurrentBuilding;
                    salesEvent.m_Resource = resourceBuyer.m_ResourceNeeded;
                    salesEvent.m_Flags = flags;
                    salesEvent.m_Distance = 0f;
                    SalesEvent value4 = salesEvent;
                    m_SalesQueue.Enqueue(value4);
                    //Plugin.Log($"SalesEvent3 {salesEvent.m_Seller.Index}->{salesEvent.m_Buyer.Index} {salesEvent.m_Amount} {salesEvent.m_Resource}: flags {salesEvent.m_Flags} distOC");
                    m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
                }
                else
                {
                    Citizen citizen = default(Citizen);
                    if (nativeArray4.Length > 0)
                    {
                        citizen = nativeArray4[j];
                        Entity household = m_HouseholdMembers[entity].m_Household;
                        Household householdData = m_Households[household];
                        DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_HouseholdCitizens[household];
                        FindShopForCitizen(chunk, unfilteredChunkIndex, entity, resourceBuyer.m_ResourceNeeded, resourceBuyer.m_AmountNeeded, resourceBuyer.m_Flags, citizen, householdData, dynamicBuffer2.Length);
                        //Plugin.Log($"FindShopForCitizen {entity.Index} {resourceBuyer.m_AmountNeeded} {resourceBuyer.m_ResourceNeeded}: flags {resourceBuyer.m_Flags} household {household.Index}");
                    }
                    else
                    {
                        FindShopForCompany(chunk, unfilteredChunkIndex, entity, resourceBuyer.m_ResourceNeeded, resourceBuyer.m_AmountNeeded, resourceBuyer.m_Flags);
                        //Plugin.Log($"FindShopForCompany {entity.Index} {resourceBuyer.m_AmountNeeded} {resourceBuyer.m_ResourceNeeded}: flags {resourceBuyer.m_Flags}");
                    }
                }
            }
        }

        private void FindShopForCitizen(ArchetypeChunk chunk, int index, Entity buyer, Resource resource, int amount, SetupTargetFlags flags, Citizen citizenData, Household householdData, int householdCitizenCount)
        {
            m_CommandBuffer.AddComponent(index, buyer, in m_PathfindTypes);
            m_CommandBuffer.SetComponent(index, buyer, new PathInformation
            {
                m_State = PathFlags.Pending
            });
            CreatureData creatureData;
            PseudoRandomSeed randomSeed;
            Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, m_HumanChunks, m_EntityType, ref m_CreatureDataType, ref m_ResidentDataType, out creatureData, out randomSeed);
            HumanData humanData = default(HumanData);
            if (entity != Entity.Null)
            {
                humanData = m_PrefabHumanData[entity];
            }
            PathfindParameters pathfindParameters = default(PathfindParameters);
            pathfindParameters.m_MaxSpeed = 277.77777f;
            pathfindParameters.m_WalkSpeed = humanData.m_WalkSpeed;
            pathfindParameters.m_Weights = 0.01f * CitizenUtils.GetPathfindWeights(citizenData, householdData, householdCitizenCount);
            pathfindParameters.m_Methods = PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(m_TimeOfDay);
            pathfindParameters.m_SecondaryIgnoredFlags = VehicleUtils.GetIgnoredPathfindFlagsTaxiDefaults();
            pathfindParameters.m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost;
            PathfindParameters parameters = pathfindParameters;
            SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
            setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
            setupQueueTarget.m_Methods = PathMethod.Pedestrian;
            setupQueueTarget.m_RandomCost = 30f;
            SetupQueueTarget origin = setupQueueTarget;
            setupQueueTarget = default(SetupQueueTarget);
            setupQueueTarget.m_Type = SetupTargetType.ResourceSeller;
            setupQueueTarget.m_Methods = PathMethod.Pedestrian;
            setupQueueTarget.m_Resource = resource;
            setupQueueTarget.m_Value = amount;
            setupQueueTarget.m_Flags = flags;
            setupQueueTarget.m_RandomCost = 30f;
            setupQueueTarget.m_ActivityMask = creatureData.m_SupportedActivities;
            SetupQueueTarget destination = setupQueueTarget;
            if (m_HouseholdMembers.HasComponent(buyer))
            {
                Entity household = m_HouseholdMembers[buyer].m_Household;
                if (m_Properties.HasComponent(household))
                {
                    parameters.m_Authorization1 = m_Properties[household].m_Property;
                }
            }
            if (m_Workers.HasComponent(buyer))
            {
                Worker worker = m_Workers[buyer];
                if (m_Properties.HasComponent(worker.m_Workplace))
                {
                    parameters.m_Authorization2 = m_Properties[worker.m_Workplace].m_Property;
                }
                else
                {
                    parameters.m_Authorization2 = worker.m_Workplace;
                }
            }
            if (m_CarKeepers.IsComponentEnabled(buyer))
            {
                Entity car = m_CarKeepers[buyer].m_Car;
                if (m_ParkedCarData.HasComponent(car))
                {
                    PrefabRef prefabRef = m_PrefabRefData[car];
                    ParkedCar parkedCar = m_ParkedCarData[car];
                    CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
                    parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
                    parameters.m_ParkingTarget = parkedCar.m_Lane;
                    parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
                    parameters.m_ParkingLength = VehicleUtils.GetParkingLength(car, ref m_PrefabRefData, ref m_ObjectGeometryData);
                    parameters.m_Methods |= PathMethod.Road | PathMethod.Parking;
                    parameters.m_IgnoredFlags = VehicleUtils.GetIgnoredPathfindFlags(carData);
                    if (m_PersonalCarData.TryGetComponent(car, out var componentData) && (componentData.m_State & PersonalCarFlags.HomeTarget) == 0)
                    {
                        parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
                    }
                }
            }
            SetupQueueItem value = new SetupQueueItem(buyer, parameters, origin, destination);
            m_PathfindQueue.Enqueue(value);
        }

        private void FindShopForCompany(ArchetypeChunk chunk, int index, Entity buyer, Resource resource, int amount, SetupTargetFlags flags)
        {
            m_CommandBuffer.AddComponent(index, buyer, in m_PathfindTypes);
            m_CommandBuffer.SetComponent(index, buyer, new PathInformation
            {
                m_State = PathFlags.Pending
            });
            float transportCost = EconomyUtils.GetTransportCost(1f, amount, m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
            PathfindParameters pathfindParameters = default(PathfindParameters);
            pathfindParameters.m_MaxSpeed = 111.111115f;
            pathfindParameters.m_WalkSpeed = 5.555556f;
            pathfindParameters.m_Weights = new PathfindWeights(0.01f, 0.01f, transportCost, 0.01f);
            pathfindParameters.m_Methods = PathMethod.Road | PathMethod.CargoLoading;
            pathfindParameters.m_IgnoredFlags = EdgeFlags.ForbidSlowTraffic;
            PathfindParameters parameters = pathfindParameters;
            SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
            setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
            setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.CargoLoading;
            setupQueueTarget.m_RoadTypes = RoadTypes.Car;
            SetupQueueTarget origin = setupQueueTarget;
            setupQueueTarget = default(SetupQueueTarget);
            setupQueueTarget.m_Type = SetupTargetType.ResourceSeller;
            setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.CargoLoading;
            setupQueueTarget.m_RoadTypes = RoadTypes.Car;
            setupQueueTarget.m_Resource = resource;
            setupQueueTarget.m_Value = amount;
            setupQueueTarget.m_Flags = flags;
            SetupQueueTarget destination = setupQueueTarget;
            SetupQueueItem value = new SetupQueueItem(buyer, parameters, origin, destination);
            m_PathfindQueue.Enqueue(value);
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

        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ResourceBought> __Game_Citizens_ResourceBought_RO_ComponentTypeHandle;

        public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;

        public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;

        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

        public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

        public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;

        public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

        public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

        public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RW_ComponentLookup;

        public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;

        [ReadOnly]
        public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>(isReadOnly: true);
            __Game_Citizens_ResourceBought_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBought>(isReadOnly: true);
            __Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
            __Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
            __Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(isReadOnly: true);
            __Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(isReadOnly: true);
            __Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttendingMeeting>(isReadOnly: true);
            __Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
            __Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            __Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
            __Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
            __Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
            __Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
            __Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
            __Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
            __Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
            __Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(isReadOnly: true);
            __Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
            __Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
            __Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
            __Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
            __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
            __Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(isReadOnly: true);
            __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
            __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
            __Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
            __Game_Companies_ServiceAvailable_RW_ComponentLookup = state.GetComponentLookup<ServiceAvailable>();
            __Game_Agents_TaxPayer_RW_ComponentLookup = state.GetComponentLookup<TaxPayer>();
            __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
            __Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
            __Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
            __Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
            __Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
            __Game_Companies_BuyingCompany_RW_ComponentLookup = state.GetComponentLookup<BuyingCompany>();
            __Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
            __Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
            __Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
            __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
            __Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
            __Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
            __Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
            __Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
        }
    }

    private const int UPDATE_INTERVAL = 16;

    private EntityQuery m_BuyerQuery;

    private EntityQuery m_CarPrefabQuery;

    private EntityQuery m_EconomyParameterQuery;

    private EntityQuery m_ResidentPrefabQuery;

    private EntityQuery m_PopulationQuery;

    private ComponentTypeSet m_PathfindTypes;

    private EndFrameBarrier m_EndFrameBarrier;

    private PathfindSetupSystem m_PathfindSetupSystem;

    private ResourceSystem m_ResourceSystem;

    private TaxSystem m_TaxSystem;

    private TimeSystem m_TimeSystem;

    private CityConfigurationSystem m_CityConfigurationSystem;

    private PersonalCarSelectData m_PersonalCarSelectData;

    private NativeQueue<SalesEvent> m_SalesQueue;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 16;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
        m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
        m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
        m_PersonalCarSelectData = new PersonalCarSelectData(this);
        m_SalesQueue = new NativeQueue<SalesEvent>(Allocator.Persistent);
        m_BuyerQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[2]
            {
                ComponentType.ReadWrite<ResourceBuyer>(),
                ComponentType.ReadWrite<TripNeeded>()
            },
            None = new ComponentType[3]
            {
                ComponentType.ReadOnly<TravelPurpose>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
            }
        }, new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<ResourceBought>() },
            None = new ComponentType[2]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
            }
        });
        m_CarPrefabQuery = GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_PopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Population>());
        m_ResidentPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
        m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
        RequireForUpdate(m_BuyerQuery);
        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_PopulationQuery);
		Mod.log.Info("Modded ResourceBuyerSystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_SalesQueue.Dispose();
        base.OnDestroy();
    }

    [Preserve]
    protected override void OnStopRunning()
    {
        base.OnStopRunning();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        if (m_BuyerQuery.CalculateEntityCount() > 0)
        {
            m_PersonalCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_CarPrefabQuery, Allocator.TempJob, out var jobHandle);
            __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Economy_Resources_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Common_Target_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_ResourceBought_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            HandleBuyersJob handleBuyersJob = default(HandleBuyersJob);
            handleBuyersJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            handleBuyersJob.m_BuyerType = __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;
            handleBuyersJob.m_BoughtType = __TypeHandle.__Game_Citizens_ResourceBought_RO_ComponentTypeHandle;
            handleBuyersJob.m_TripType = __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle;
            handleBuyersJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle;
            handleBuyersJob.m_CreatureDataType = __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle;
            handleBuyersJob.m_ResidentDataType = __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle;
            handleBuyersJob.m_AttendingMeetingType = __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle;
            handleBuyersJob.m_ServiceAvailables = __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup;
            handleBuyersJob.m_PathInformation = __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup;
            handleBuyersJob.m_Properties = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
            handleBuyersJob.m_CarKeepers = __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup;
            handleBuyersJob.m_ParkedCarData = __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup;
            handleBuyersJob.m_PersonalCarData = __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup;
            handleBuyersJob.m_Targets = __TypeHandle.__Game_Common_Target_RO_ComponentLookup;
            handleBuyersJob.m_CurrentBuildings = __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup;
            handleBuyersJob.m_OutsideConnections = __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup;
            handleBuyersJob.m_HouseholdMembers = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup;
            handleBuyersJob.m_Households = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
            handleBuyersJob.m_TouristHouseholds = __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup;
            handleBuyersJob.m_CommuterHouseholds = __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup;
            handleBuyersJob.m_ServiceCompanyDatas = __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            handleBuyersJob.m_Workers = __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup;
            handleBuyersJob.m_Resources = __TypeHandle.__Game_Economy_Resources_RO_BufferLookup;
            handleBuyersJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            handleBuyersJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            handleBuyersJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            handleBuyersJob.m_PrefabRefData = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            handleBuyersJob.m_PrefabCarData = __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup;
            handleBuyersJob.m_ObjectGeometryData = __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
            handleBuyersJob.m_PrefabHumanData = __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup;
            handleBuyersJob.m_CoordinatedMeetings = __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;
            handleBuyersJob.m_HaveCoordinatedMeetingDatas = __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;
            handleBuyersJob.m_TimeOfDay = m_TimeSystem.normalizedTime;
            handleBuyersJob.m_RandomSeed = RandomSeed.Next();
            handleBuyersJob.m_PathfindTypes = m_PathfindTypes;
            handleBuyersJob.m_HumanChunks = m_ResidentPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            handleBuyersJob.m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 80, 16).AsParallelWriter();
            handleBuyersJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            handleBuyersJob.m_SalesQueue = m_SalesQueue.AsParallelWriter();
            HandleBuyersJob jobData = handleBuyersJob;
            base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BuyerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
            m_ResourceSystem.AddPrefabsReader(base.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
            m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
            __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_BuyingCompany_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            BuyJob buyJob = default(BuyJob);
            buyJob.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            buyJob.m_Resources = __TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            buyJob.m_SalesQueue = m_SalesQueue;
            buyJob.m_Services = __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup;
            buyJob.m_TaxPayers = __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentLookup;
            buyJob.m_TransformDatas = __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            buyJob.m_PropertyRenters = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
            buyJob.m_OwnedVehicles = __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup;
            buyJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            buyJob.m_HouseholdAnimals = __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup;
            buyJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            buyJob.m_ServiceCompanies = __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            buyJob.m_Storages = __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup;
            buyJob.m_Households = __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup;
            buyJob.m_BuyingCompanies = __TypeHandle.__Game_Companies_BuyingCompany_RW_ComponentLookup;
            buyJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            buyJob.m_TradeCosts = __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup;
            buyJob.m_BuildingDatas = __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            buyJob.m_Employees = __TypeHandle.__Game_Companies_Employee_RO_BufferLookup;
            buyJob.m_ProcessDatas = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            buyJob.m_Spawnables = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            buyJob.m_WorkplaceDatas = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            buyJob.m_BuildingEfficiencies = __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup;
            buyJob.m_WorkProviders = __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup;
            buyJob.m_OutsideConnections = __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup;
            buyJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            buyJob.m_RandomSeed = RandomSeed.Next();
            buyJob.m_PersonalCarSelectData = m_PersonalCarSelectData;
            buyJob.m_TaxRates = m_TaxSystem.GetTaxRates();
            buyJob.m_Districts = __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup;
            buyJob.m_DistrictModifiers = __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup;
            buyJob.m_PopulationData = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
            buyJob.m_PopulationEntity = m_PopulationQuery.GetSingletonEntity();
            buyJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
            BuyJob jobData2 = buyJob;
            base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, jobHandle));
            m_PersonalCarSelectData.PostUpdate(base.Dependency);
            m_ResourceSystem.AddPrefabsReader(base.Dependency);
            m_TaxSystem.AddReader(base.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        }
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
    public ResourceBuyerSystem()
    {
    }
}
