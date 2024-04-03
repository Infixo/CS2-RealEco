using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Events;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
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
public partial class CitizenBehaviorSystem : GameSystemBase
{
    //[BurstCompile]
    private struct CitizenReserveHouseholdCarJob : IJob
    {
        public ComponentLookup<CarKeeper> m_CarKeepers;

        public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCars;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> m_HouseholdMembers;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;

        [ReadOnly]
        public ComponentLookup<Citizen> m_Citizens;

        public NativeQueue<Entity> m_ReserverQueue;

        public EntityCommandBuffer m_CommandBuffer;

        public void Execute()
        {
            Entity item;
            while (m_ReserverQueue.TryDequeue(out item))
            {
                if (m_HouseholdMembers.HasComponent(item))
                {
                    Entity household = m_HouseholdMembers[item].m_Household;
                    Entity car = Entity.Null;
                    if (m_Citizens[item].GetAge() != 0 && HouseholdBehaviorSystem.GetFreeCar(household, m_OwnedVehicles, m_PersonalCars, ref car) && !m_CarKeepers.IsComponentEnabled(item))
                    {
                        m_CarKeepers.SetComponentEnabled(item, value: true);
                        m_CarKeepers[item] = new CarKeeper
                        {
                            m_Car = car
                        };
                        Game.Vehicles.PersonalCar value = m_PersonalCars[car];
                        value.m_Keeper = item;
                        m_PersonalCars[car] = value;
                    }
                }
            }
        }
    }


    //[BurstCompile]
    private struct CitizenTryCollectMailJob : IJob
    {
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

        [ReadOnly]
        public ComponentLookup<MailAccumulationData> m_MailAccumulationData;

        [ReadOnly]
        public ComponentLookup<ServiceObjectData> m_ServiceObjectData;

        public ComponentLookup<MailSender> m_MailSenderData;

        public ComponentLookup<MailProducer> m_MailProducerData;

        public NativeQueue<Entity> m_MailSenderQueue;

        public EntityCommandBuffer m_CommandBuffer;

        public void Execute()
        {
            Entity item;
            while (m_MailSenderQueue.TryDequeue(out item))
            {
                if (!m_CurrentBuildingData.TryGetComponent(item, out var componentData) || !m_MailProducerData.TryGetComponent(componentData.m_CurrentBuilding, out var componentData2) || componentData2.m_SendingMail < 15 || RequireCollect(m_PrefabRefData[componentData.m_CurrentBuilding].m_Prefab))
                {
                    continue;
                }
                bool flag = m_MailSenderData.IsComponentEnabled(item);
                MailSender value = (flag ? m_MailSenderData[item] : default(MailSender));
                int num = math.min(componentData2.m_SendingMail, 100 - value.m_Amount);
                if (num > 0)
                {
                    value.m_Amount = (ushort)(value.m_Amount + num);
                    componentData2.m_SendingMail = (ushort)(componentData2.m_SendingMail - num);
                    m_MailProducerData[componentData.m_CurrentBuilding] = componentData2;
                    if (!flag)
                    {
                        m_MailSenderData.SetComponentEnabled(item, value: true);
                    }
                    m_MailSenderData[item] = value;
                }
            }
        }

        private bool RequireCollect(Entity prefab)
        {
            if (m_SpawnableBuildingData.HasComponent(prefab))
            {
                SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefab];
                if (m_MailAccumulationData.HasComponent(spawnableBuildingData.m_ZonePrefab))
                {
                    return m_MailAccumulationData[spawnableBuildingData.m_ZonePrefab].m_RequireCollect;
                }
            }
            else if (m_ServiceObjectData.HasComponent(prefab))
            {
                ServiceObjectData serviceObjectData = m_ServiceObjectData[prefab];
                if (m_MailAccumulationData.HasComponent(serviceObjectData.m_Service))
                {
                    return m_MailAccumulationData[serviceObjectData.m_Service].m_RequireCollect;
                }
            }
            return false;
        }
    }


    //[BurstCompile]
    private struct CitizeSleepJob : IJob
    {
        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

        public ComponentLookup<CitizenPresence> m_CitizenPresenceData;

        public NativeQueue<Entity> m_SleepQueue;

        public void Execute()
        {
            Entity item;
            while (m_SleepQueue.TryDequeue(out item))
            {
                if (m_CurrentBuildingData.HasComponent(item))
                {
                    CurrentBuilding currentBuilding = m_CurrentBuildingData[item];
                    if (m_CitizenPresenceData.HasComponent(currentBuilding.m_CurrentBuilding))
                    {
                        CitizenPresence value = m_CitizenPresenceData[currentBuilding.m_CurrentBuilding];
                        value.m_Delta = (sbyte)math.max(-127, value.m_Delta - 1);
                        m_CitizenPresenceData[currentBuilding.m_CurrentBuilding] = value;
                    }
                }
            }
        }
    }


    //[BurstCompile]
    private struct CitizenAITickJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

        [ReadOnly]
        public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

        public BufferTypeHandle<TripNeeded> m_TripType;

        [ReadOnly]
        public ComponentTypeHandle<Leisure> m_LeisureType;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<HouseholdNeed> m_HouseholdNeeds;

        [ReadOnly]
        public ComponentLookup<Household> m_Households;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_Properties;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_Transforms;

        [ReadOnly]
        public ComponentLookup<CarKeeper> m_CarKeepers;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCars;

        [ReadOnly]
        public ComponentLookup<MovingAway> m_MovingAway;

        [ReadOnly]
        public ComponentLookup<Worker> m_Workers;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> m_Students;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> m_TouristHouseholds;

        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

        [ReadOnly]
        public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

        [ReadOnly]
        public ComponentLookup<InDanger> m_InDangerData;

        [ReadOnly]
        public ComponentLookup<AttendingMeeting> m_AttendingMeetings;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<CoordinatedMeeting> m_Meetings;

        [ReadOnly]
        public BufferLookup<CoordinatedMeetingAttendee> m_Attendees;

        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> m_MeetingDatas;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> m_BuildingStudents;

        [ReadOnly]
        public ComponentLookup<Population> m_PopulationData;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> m_OwnedVehicles;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;

        [ReadOnly]
        public EntityArchetype m_HouseholdArchetype;

        [ReadOnly]
        public NativeList<Entity> m_OutsideConnectionEntities;

        [ReadOnly]
        public EconomyParameterData m_EconomyParameters;

        [ReadOnly]
        public LeisureParametersData m_LeisureParameters;

        public uint m_UpdateFrameIndex;

        public float m_NormalizedTime;

        public uint m_SimulationFrame;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public NativeQueue<Entity>.ParallelWriter m_CarReserverQueue;

        public NativeQueue<Entity>.ParallelWriter m_MailSenderQueue;

        public NativeQueue<Entity>.ParallelWriter m_SleepQueue;

        public TimeData m_TimeData;

        public Entity m_PopulationEntity;

        public RandomSeed m_RandomSeed;

        private bool CheckSleep(int index, Entity entity, ref Citizen citizen, Entity currentBuilding, Entity household, Entity home, DynamicBuffer<TripNeeded> trips, ref EconomyParameterData economyParameters, ref Unity.Mathematics.Random random)
        {
            if (IsSleepTime(entity, citizen, ref economyParameters, m_NormalizedTime, ref m_Workers, ref m_Students))
            {
                if (home != Entity.Null && currentBuilding == home)
                {
                    TravelPurpose component = default(TravelPurpose);
                    component.m_Purpose = Purpose.Sleeping;
                    m_CommandBuffer.AddComponent(index, entity, component);
                    m_SleepQueue.Enqueue(entity);
                    ReleaseCar(index, entity);
                }
                else
                {
                    GoHome(entity, home, trips, currentBuilding);
                }
                return true;
            }
            return false;
        }

        private bool CheckLeisure(ref Citizen citizenData, ref Unity.Mathematics.Random random)
        {
            int num = 128 - citizenData.m_LeisureCounter;
            return random.NextInt(m_LeisureParameters.m_LeisureRandomFactor) < num;
        }

        private void GoHome(Entity entity, Entity target, DynamicBuffer<TripNeeded> trips, Entity currentBuilding)
        {
            if (!(target == Entity.Null) && !(currentBuilding == target))
            {
                if (!m_CarKeepers.IsComponentEnabled(entity))
                {
                    m_CarReserverQueue.Enqueue(entity);
                }
                m_MailSenderQueue.Enqueue(entity);
                TripNeeded tripNeeded = default(TripNeeded);
                tripNeeded.m_TargetAgent = target;
                tripNeeded.m_Purpose = Purpose.GoingHome;
                TripNeeded elem = tripNeeded;
                trips.Add(elem);
            }
        }

        private void GoToOutsideConnection(Entity entity, Entity household, Entity currentBuilding, ref Citizen citizen, DynamicBuffer<TripNeeded> trips, Purpose purpose, ref Unity.Mathematics.Random random)
        {
            if (purpose == Purpose.MovingAway)
            {
                for (int i = 0; i < trips.Length; i++)
                {
                    if (trips[i].m_Purpose == Purpose.MovingAway)
                    {
                        return;
                    }
                }
            }
            if (!m_OutsideConnections.HasComponent(currentBuilding))
            {
                if (!m_CarKeepers.IsComponentEnabled(entity))
                {
                    m_CarReserverQueue.Enqueue(entity);
                }
                m_MailSenderQueue.Enqueue(entity);
                Entity result;
                if (m_OwnedVehicles.HasBuffer(household) && m_OwnedVehicles[household].Length > 0 && purpose == Purpose.MovingAway)
                {
                    BuildingUtils.GetRandomOutsideConnectionByTransferType(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_Prefabs, random, OutsideConnectionTransferType.Road, out result);
                }
                else
                {
                    OutsideConnectionTransferType outsideConnectionTransferType = OutsideConnectionTransferType.Train | OutsideConnectionTransferType.Air | OutsideConnectionTransferType.Ship;
                    if (m_OwnedVehicles.HasBuffer(household) && m_OwnedVehicles[household].Length > 0)
                    {
                        outsideConnectionTransferType |= OutsideConnectionTransferType.Road;
                    }
                    BuildingUtils.GetRandomOutsideConnectionByTransferType(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_Prefabs, random, outsideConnectionTransferType, out result);
                }
                if (result == Entity.Null)
                {
                    int index = random.NextInt(m_OutsideConnectionEntities.Length);
                    result = m_OutsideConnectionEntities[index];
                }
                TripNeeded elem = default(TripNeeded);
                elem.m_TargetAgent = result;
                elem.m_Purpose = purpose;
                trips.Add(elem);
            }
            else if (purpose == Purpose.MovingAway)
            {
                citizen.m_State |= CitizenFlags.MovingAway;
            }
        }

        private void GoShopping(int chunkIndex, Entity citizen, Entity household, HouseholdNeed need, float3 position)
        {
            if (!m_CarKeepers.IsComponentEnabled(citizen))
            {
                m_CarReserverQueue.Enqueue(citizen);
            }
            m_MailSenderQueue.Enqueue(citizen);
            ResourceBuyer component = default(ResourceBuyer);
            component.m_Payer = household;
            component.m_Flags = SetupTargetFlags.Commercial;
            component.m_Location = position;
            component.m_ResourceNeeded = need.m_Resource;
            component.m_AmountNeeded = need.m_Amount;
            // 240306 fix Office
            //if (need.m_Resource == Resource.Software || need.m_Resource == Resource.Telecom || need.m_Resource == Resource.Media || need.m_Resource == Resource.Financial)
                //component.m_Flags = SetupTargetFlags.Industrial;
            m_CommandBuffer.AddComponent(chunkIndex, citizen, component);
            //Plugin.Log($"Shopping {household.Index}: {need.m_Resource} {need.m_Amount} => {component.m_Flags}");
        }

        private float GetTimeLeftUntilInterval(float2 interval)
        {
            if (!(m_NormalizedTime < interval.x))
            {
                return 1f - m_NormalizedTime + interval.x;
            }
            return interval.x - m_NormalizedTime;
        }

        private bool DoLeisure(int chunkIndex, Entity entity, ref Citizen citizen, Entity household, float3 position, int population, ref Unity.Mathematics.Random random, ref EconomyParameterData economyParameters)
        {
            int num = math.min(80, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(economyParameters.m_TrafficReduction * (float)population))));
            if (random.NextInt(100) > num)
            {
                citizen.m_LeisureCounter = byte.MaxValue;
                return true;
            }
            float2 sleepTime = GetSleepTime(entity, citizen, ref economyParameters, ref m_Workers, ref m_Students);
            float num2 = GetTimeLeftUntilInterval(sleepTime);
            if (m_Workers.HasComponent(entity))
            {
                Worker worker = m_Workers[entity];
                float2 timeToWork = WorkerSystem.GetTimeToWork(citizen, worker, ref economyParameters, includeCommute: true);
                num2 = math.min(num2, GetTimeLeftUntilInterval(timeToWork));
            }
            else if (m_Students.HasComponent(entity))
            {
                Game.Citizens.Student student = m_Students[entity];
                float2 timeToStudy = StudentSystem.GetTimeToStudy(citizen, student, ref economyParameters);
                num2 = math.min(num2, GetTimeLeftUntilInterval(timeToStudy));
            }
            uint num3 = (uint)(num2 * 262144f);
            Leisure leisure = default(Leisure);
            leisure.m_LastPossibleFrame = m_SimulationFrame + num3;
            Leisure component = leisure;
            m_CommandBuffer.AddComponent(chunkIndex, entity, component);
            return false;
        }

        private void ReleaseCar(int chunkIndex, Entity citizen)
        {
            if (m_CarKeepers.IsComponentEnabled(citizen))
            {
                Entity car = m_CarKeepers[citizen].m_Car;
                if (m_PersonalCars.HasComponent(car))
                {
                    Game.Vehicles.PersonalCar value = m_PersonalCars[car];
                    value.m_Keeper = Entity.Null;
                    m_PersonalCars[car] = value;
                }
                m_CommandBuffer.SetComponentEnabled<CarKeeper>(chunkIndex, citizen, value: false);
            }
        }

        private bool AttendMeeting(int chunkIndex, Entity entity, ref Citizen citizen, Entity household, Entity currentBuilding, DynamicBuffer<TripNeeded> trips, ref Unity.Mathematics.Random random)
        {
            Entity meeting = m_AttendingMeetings[entity].m_Meeting;
            if (m_Attendees.HasBuffer(meeting) && m_Meetings.HasComponent(meeting))
            {
                CoordinatedMeeting value = m_Meetings[meeting];
                if (m_Prefabs.HasComponent(meeting) && value.m_Status != MeetingStatus.Done)
                {
                    HaveCoordinatedMeetingData haveCoordinatedMeetingData = m_MeetingDatas[m_Prefabs[meeting].m_Prefab][value.m_Phase];
                    DynamicBuffer<CoordinatedMeetingAttendee> dynamicBuffer = m_Attendees[meeting];
                    if (value.m_Status == MeetingStatus.Waiting && value.m_Target == Entity.Null)
                    {
                        if (dynamicBuffer.Length > 0 && dynamicBuffer[0].m_Attendee == entity)
                        {
                            if (haveCoordinatedMeetingData.m_Purpose.m_Purpose == Purpose.Shopping)
                            {
                                float3 position = m_Transforms[currentBuilding].m_Position;
                                GoShopping(chunkIndex, entity, household, new HouseholdNeed
                                {
                                    m_Resource = haveCoordinatedMeetingData.m_Purpose.m_Resource,
                                    m_Amount = haveCoordinatedMeetingData.m_Purpose.m_Data
                                }, position);
                                //Plugin.Log($"...in meeting, household need NOT removed");
                                return true;
                            }
                            if (haveCoordinatedMeetingData.m_Purpose.m_Purpose == Purpose.Traveling)
                            {
                                Citizen citizen2 = default(Citizen);
                                GoToOutsideConnection(entity, household, currentBuilding, ref citizen2, trips, haveCoordinatedMeetingData.m_Purpose.m_Purpose, ref random);
                            }
                            else
                            {
                                if (haveCoordinatedMeetingData.m_Purpose.m_Purpose != Purpose.GoingHome)
                                {
                                    TripNeeded elem = default(TripNeeded);
                                    elem.m_Purpose = haveCoordinatedMeetingData.m_Purpose.m_Purpose;
                                    elem.m_Resource = haveCoordinatedMeetingData.m_Purpose.m_Resource;
                                    elem.m_Data = haveCoordinatedMeetingData.m_Purpose.m_Data;
                                    elem.m_TargetAgent = default(Entity);
                                    trips.Add(elem);
                                    return true;
                                }
                                if (m_Properties.HasComponent(household))
                                {
                                    value.m_Target = m_Properties[household].m_Property;
                                    m_Meetings[meeting] = value;
                                    GoHome(entity, m_Properties[household].m_Property, trips, currentBuilding);
                                }
                            }
                        }
                    }
                    else if (value.m_Status == MeetingStatus.Waiting || value.m_Status == MeetingStatus.Traveling)
                    {
                        for (int i = 0; i < dynamicBuffer.Length; i++)
                        {
                            if (dynamicBuffer[i].m_Attendee == entity)
                            {
                                if (value.m_Target != Entity.Null && currentBuilding != value.m_Target && (!m_Properties.HasComponent(value.m_Target) || m_Properties[value.m_Target].m_Property != currentBuilding))
                                {
                                    TripNeeded elem2 = default(TripNeeded);
                                    elem2.m_Purpose = haveCoordinatedMeetingData.m_Purpose.m_Purpose;
                                    elem2.m_Resource = haveCoordinatedMeetingData.m_Purpose.m_Resource;
                                    elem2.m_Data = haveCoordinatedMeetingData.m_Purpose.m_Data;
                                    elem2.m_TargetAgent = value.m_Target;
                                    trips.Add(elem2);
                                }
                                return true;
                            }
                        }
                        m_CommandBuffer.RemoveComponent<AttendingMeeting>(chunkIndex, entity);
                        return false;
                    }
                }
                return value.m_Status != MeetingStatus.Done;
            }
            m_CommandBuffer.RemoveComponent<AttendingMeeting>(chunkIndex, entity);
            return false;
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdMemberType);
            NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray(ref m_CurrentBuildingType);
            BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
            bool flag = chunk.Has(ref m_HealthProblemType);
            int population = m_PopulationData[m_PopulationEntity].m_Population;
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity household = nativeArray3[i].m_Household;
                Entity entity = nativeArray[i];
                bool flag2 = m_TouristHouseholds.HasComponent(household);
                DynamicBuffer<TripNeeded> trips = bufferAccessor[i];
                if (household == Entity.Null)
                {
                    household = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_HouseholdArchetype);
                    m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, new HouseholdMember
                    {
                        m_Household = household
                    });
                    m_CommandBuffer.SetBuffer<HouseholdCitizen>(unfilteredChunkIndex, household).Add(new HouseholdCitizen
                    {
                        m_Citizen = entity
                    });
                    continue;
                }
                if (!m_Households.HasComponent(household))
                {
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
                    continue;
                }
                Entity currentBuilding = nativeArray4[i].m_CurrentBuilding;
                if (!m_Transforms.HasComponent(currentBuilding) || (m_InDangerData.HasComponent(currentBuilding) && (m_InDangerData[currentBuilding].m_Flags & DangerFlags.StayIndoors) != 0))
                {
                    continue;
                }
                Citizen citizen = nativeArray2[i];
                bool flag3 = (citizen.m_State & CitizenFlags.Commuter) != 0;
                CitizenAge age = citizen.GetAge();
                if (flag3 && (age == CitizenAge.Elderly || age == CitizenAge.Child))
                {
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
                }
                if ((citizen.m_State & CitizenFlags.MovingAway) != 0)
                {
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
                    continue;
                }
                if (m_MovingAway.HasComponent(household))
                {
                    GoToOutsideConnection(entity, household, currentBuilding, ref citizen, trips, Purpose.MovingAway, ref random);
                    if (chunk.Has(ref m_LeisureType))
                    {
                        m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
                    }
                    if (m_Workers.HasComponent(entity))
                    {
                        m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
                    }
                    if (m_Students.HasComponent(entity))
                    {
                        if (m_BuildingStudents.HasBuffer(m_Students[entity].m_School))
                        {
                            m_CommandBuffer.AddComponent<StudentsRemoved>(unfilteredChunkIndex, m_Students[entity].m_School);
                        }
                        m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(unfilteredChunkIndex, entity);
                    }
                    nativeArray2[i] = citizen;
                    continue;
                }
                Entity entity2 = Entity.Null;
                if (m_Properties.HasComponent(household))
                {
                    entity2 = m_Properties[household].m_Property;
                }
                else if (flag2)
                {
                    Entity hotel = m_TouristHouseholds[household].m_Hotel;
                    if (m_Properties.HasComponent(hotel))
                    {
                        entity2 = m_Properties[hotel].m_Property;
                    }
                }
                else if (flag3)
                {
                    if (m_OutsideConnections.HasComponent(currentBuilding))
                    {
                        entity2 = currentBuilding;
                    }
                    else
                    {
                        if (m_CommuterHouseholds.TryGetComponent(household, out var componentData))
                        {
                            entity2 = componentData.m_OriginalFrom;
                        }
                        if (entity2 == Entity.Null)
                        {
                            entity2 = m_OutsideConnectionEntities[random.NextInt(m_OutsideConnectionEntities.Length)];
                        }
                    }
                }
                if (flag)
                {
                    if (chunk.Has(ref m_LeisureType))
                    {
                        m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
                    }
                }
                else
                {
                    if (m_AttendingMeetings.HasComponent(entity) && AttendMeeting(unfilteredChunkIndex, entity, ref citizen, household, currentBuilding, trips, ref random))
                    {
                        continue;
                    }
                    if ((m_Workers.HasComponent(entity) && !WorkerSystem.IsTodayOffDay(citizen, ref m_EconomyParameters, m_SimulationFrame, m_TimeData, population) && WorkerSystem.IsTimeToWork(citizen, m_Workers[entity], ref m_EconomyParameters, m_NormalizedTime)) || (m_Students.HasComponent(entity) && StudentSystem.IsTimeToStudy(citizen, m_Students[entity], ref m_EconomyParameters, m_NormalizedTime, m_SimulationFrame, m_TimeData, population)))
                    {
                        if (chunk.Has(ref m_LeisureType))
                        {
                            m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
                        }
                        continue;
                    }
                    if (CheckSleep(i, entity, ref citizen, currentBuilding, household, entity2, trips, ref m_EconomyParameters, ref random))
                    {
                        if (chunk.Has(ref m_LeisureType))
                        {
                            m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
                        }
                        continue;
                    }
                    if (age == CitizenAge.Adult || age == CitizenAge.Elderly)
                    {
                        HouseholdNeed householdNeed = m_HouseholdNeeds[household];
                        if (householdNeed.m_Resource != Resource.NoResource && m_Transforms.HasComponent(currentBuilding))
                        {
                            GoShopping(unfilteredChunkIndex, entity, household, householdNeed, m_Transforms[currentBuilding].m_Position);
                            householdNeed.m_Resource = Resource.NoResource;
                            m_HouseholdNeeds[household] = householdNeed;
                            if (chunk.Has(ref m_LeisureType))
                            {
                                m_CommandBuffer.RemoveComponent<Leisure>(unfilteredChunkIndex, entity);
                            }
                            //Plugin.Log($"...in building, household need removed");
                            continue;
                        }
                    }
                    bool num = !chunk.Has(ref m_LeisureType) && !m_OutsideConnections.HasComponent(currentBuilding) && CheckLeisure(ref citizen, ref random);
                    nativeArray2[i] = citizen;
                    if (num)
                    {
                        if (DoLeisure(unfilteredChunkIndex, entity, ref citizen, household, m_Transforms[currentBuilding].m_Position, population, ref random, ref m_EconomyParameters))
                        {
                            nativeArray2[i] = citizen;
                        }
                    }
                    else if (!chunk.Has(ref m_LeisureType))
                    {
                        if (currentBuilding != entity2)
                        {
                            GoHome(entity, entity2, trips, currentBuilding);
                        }
                        else
                        {
                            ReleaseCar(unfilteredChunkIndex, entity);
                        }
                    }
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

        public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RO_ComponentTypeHandle;

        public ComponentLookup<HouseholdNeed> __Game_Citizens_HouseholdNeed_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

        public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<CoordinatedMeetingAttendee> __Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup;

        public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;

        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<MailAccumulationData> __Game_Prefabs_MailAccumulationData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

        public ComponentLookup<MailSender> __Game_Citizens_MailSender_RW_ComponentLookup;

        public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RW_ComponentLookup;

        public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RW_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
            __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
            __Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
            __Game_Citizens_Leisure_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>(isReadOnly: true);
            __Game_Citizens_HouseholdNeed_RW_ComponentLookup = state.GetComponentLookup<HouseholdNeed>();
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
            __Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
            __Game_Vehicles_PersonalCar_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>();
            __Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
            __Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
            __Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
            __Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
            __Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
            __Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
            __Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup = state.GetBufferLookup<CoordinatedMeetingAttendee>(isReadOnly: true);
            __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
            __Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(isReadOnly: true);
            __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
            __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
            __Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
            __Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(isReadOnly: true);
            __Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
            __Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
            __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            __Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_MailAccumulationData_RO_ComponentLookup = state.GetComponentLookup<MailAccumulationData>(isReadOnly: true);
            __Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
            __Game_Citizens_MailSender_RW_ComponentLookup = state.GetComponentLookup<MailSender>();
            __Game_Buildings_MailProducer_RW_ComponentLookup = state.GetComponentLookup<MailProducer>();
            __Game_Buildings_CitizenPresence_RW_ComponentLookup = state.GetComponentLookup<CitizenPresence>();
        }
    }

    public static readonly float kMaxPathfindCost = 17000f;

    private JobHandle m_CarReserveWriters;

    private EntityQuery m_CitizenQuery;

    private EntityQuery m_OutsideConnectionQuery;

    private EntityQuery m_EconomyParameterQuery;

    private EntityQuery m_LeisureParameterQuery;

    private EntityQuery m_TimeDataQuery;

    private EntityQuery m_PopulationQuery;

    private SimulationSystem m_SimulationSystem;

    private TimeSystem m_TimeSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private CityStatisticsSystem m_CityStatisticsSystem;

    private EntityArchetype m_HouseholdArchetype;

    private NativeQueue<Entity> m_CarReserverQueue;

    private NativeQueue<Entity>.ParallelWriter m_ParallelCarReserverQueue;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 16;
    }

    public override int GetUpdateOffset(SystemUpdatePhase phase)
    {
        return 11;
    }

    public static float2 GetSleepTime(Entity entity, Citizen citizen, ref EconomyParameterData economyParameters, ref ComponentLookup<Worker> workers, ref ComponentLookup<Game.Citizens.Student> students)
    {
        CitizenAge age = citizen.GetAge();
        float2 x = new float2(0.875f, 0.175f);
        float num = x.y - x.x;
        x += citizen.GetPseudoRandom(CitizenPseudoRandom.SleepOffset).NextFloat(0f, 0.2f);
        if (age == CitizenAge.Elderly)
        {
            x -= 0.05f;
        }
        if (age == CitizenAge.Child)
        {
            x -= 0.1f;
        }
        if (age == CitizenAge.Teen)
        {
            x += 0.05f;
        }
        x = math.frac(x);
        float2 @float;
        if (workers.HasComponent(entity))
        {
            @float = WorkerSystem.GetTimeToWork(citizen, workers[entity], ref economyParameters, includeCommute: true);
        }
        else
        {
            if (!students.HasComponent(entity))
            {
                return x;
            }
            @float = StudentSystem.GetTimeToStudy(citizen, students[entity], ref economyParameters);
        }
        if (@float.x < @float.y)
        {
            if (x.x > x.y && @float.y > x.x)
            {
                x += @float.y - x.x;
            }
            else if (x.y > @float.x)
            {
                x += 1f - (x.y - @float.x);
            }
        }
        else
        {
            x = new float2(@float.y, @float.y + num);
        }
        return math.frac(x);
    }

    public static bool IsSleepTime(Entity entity, Citizen citizen, ref EconomyParameterData economyParameters, float normalizedTime, ref ComponentLookup<Worker> workers, ref ComponentLookup<Game.Citizens.Student> students)
    {
        float2 sleepTime = GetSleepTime(entity, citizen, ref economyParameters, ref workers, ref students);
        if (sleepTime.y < sleepTime.x)
        {
            if (!(normalizedTime > sleepTime.x))
            {
                return normalizedTime < sleepTime.y;
            }
            return true;
        }
        if (normalizedTime > sleepTime.x)
        {
            return normalizedTime < sleepTime.y;
        }
        return false;
    }

    public NativeQueue<Entity>.ParallelWriter GetCarReserverQueue(out JobHandle deps)
    {
        deps = m_CarReserveWriters;
        return m_ParallelCarReserverQueue;
    }

    public void AddCarReserveWriter(JobHandle writer)
    {
        m_CarReserveWriters = JobHandle.CombineDependencies(m_CarReserveWriters, writer);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        m_CarReserverQueue = new NativeQueue<Entity>(Allocator.Persistent);
        m_ParallelCarReserverQueue = m_CarReserverQueue.AsParallelWriter();
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_LeisureParameterQuery = GetEntityQuery(ComponentType.ReadOnly<LeisureParametersData>());
        m_PopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Population>());
        m_CitizenQuery = GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.Exclude<TravelPurpose>(), ComponentType.Exclude<ResourceBuyer>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        m_HouseholdArchetype = base.World.EntityManager.CreateArchetype(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<HouseholdNeed>(), ComponentType.ReadWrite<HouseholdCitizen>(), ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadWrite<UpdateFrame>(), ComponentType.ReadWrite<Created>());
        RequireForUpdate(m_CitizenQuery);
        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_LeisureParameterQuery);
        RequireForUpdate(m_TimeDataQuery);
        RequireForUpdate(m_PopulationQuery);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_CarReserverQueue.Dispose();
        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
        NativeQueue<Entity> mailSenderQueue = new NativeQueue<Entity>(Allocator.TempJob);
        NativeQueue<Entity> sleepQueue = new NativeQueue<Entity>(Allocator.TempJob);
        __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Student_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        CitizenAITickJob citizenAITickJob = default(CitizenAITickJob);
        citizenAITickJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle;
        citizenAITickJob.m_CurrentBuildingType = __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;
        citizenAITickJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        citizenAITickJob.m_HouseholdMemberType = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
        citizenAITickJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        citizenAITickJob.m_HealthProblemType = __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle;
        citizenAITickJob.m_TripType = __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle;
        citizenAITickJob.m_LeisureType = __TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle;
        citizenAITickJob.m_HouseholdNeeds = __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentLookup;
        citizenAITickJob.m_Households = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
        citizenAITickJob.m_Properties = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
        citizenAITickJob.m_Transforms = __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
        citizenAITickJob.m_CarKeepers = __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup;
        citizenAITickJob.m_PersonalCars = __TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup;
        citizenAITickJob.m_MovingAway = __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup;
        citizenAITickJob.m_Workers = __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup;
        citizenAITickJob.m_Students = __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup;
        citizenAITickJob.m_TouristHouseholds = __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup;
        citizenAITickJob.m_OutsideConnections = __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup;
        citizenAITickJob.m_InDangerData = __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup;
        citizenAITickJob.m_Attendees = __TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RO_BufferLookup;
        citizenAITickJob.m_Meetings = __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;
        citizenAITickJob.m_AttendingMeetings = __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup;
        citizenAITickJob.m_MeetingDatas = __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;
        citizenAITickJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        citizenAITickJob.m_BuildingStudents = __TypeHandle.__Game_Buildings_Student_RO_BufferLookup;
        citizenAITickJob.m_PopulationData = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
        citizenAITickJob.m_OutsideConnectionDatas = __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
        citizenAITickJob.m_OwnedVehicles = __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        citizenAITickJob.m_CommuterHouseholds = __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup;
        citizenAITickJob.m_HouseholdArchetype = m_HouseholdArchetype;
        citizenAITickJob.m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(Allocator.TempJob, out var outJobHandle);
        citizenAITickJob.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
        citizenAITickJob.m_LeisureParameters = m_LeisureParameterQuery.GetSingleton<LeisureParametersData>();
        citizenAITickJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        citizenAITickJob.m_UpdateFrameIndex = updateFrameWithInterval;
        citizenAITickJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        citizenAITickJob.m_NormalizedTime = m_TimeSystem.normalizedTime;
        citizenAITickJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
        citizenAITickJob.m_PopulationEntity = m_PopulationQuery.GetSingletonEntity();
        citizenAITickJob.m_CarReserverQueue = m_ParallelCarReserverQueue;
        citizenAITickJob.m_MailSenderQueue = mailSenderQueue.AsParallelWriter();
        citizenAITickJob.m_SleepQueue = sleepQueue.AsParallelWriter();
        citizenAITickJob.m_RandomSeed = RandomSeed.Next();
        CitizenAITickJob jobData = citizenAITickJob;
        JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, JobHandle.CombineDependencies(m_CarReserveWriters, JobHandle.CombineDependencies(base.Dependency, outJobHandle)));
        jobData.m_OutsideConnectionEntities.Dispose(jobHandle);
        m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
        AddCarReserveWriter(jobHandle);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        CitizenReserveHouseholdCarJob jobData2 = default(CitizenReserveHouseholdCarJob);
        jobData2.m_CarKeepers = __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup;
        jobData2.m_HouseholdMembers = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup;
        jobData2.m_OwnedVehicles = __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup;
        jobData2.m_PersonalCars = __TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup;
        jobData2.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
        jobData2.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
        jobData2.m_ReserverQueue = m_CarReserverQueue;
        JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, m_CarReserveWriters));
        m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
        AddCarReserveWriter(jobHandle2);
        __TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        CitizenTryCollectMailJob jobData3 = default(CitizenTryCollectMailJob);
        jobData3.m_CurrentBuildingData = __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup;
        jobData3.m_PrefabRefData = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        jobData3.m_SpawnableBuildingData = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
        jobData3.m_MailAccumulationData = __TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup;
        jobData3.m_ServiceObjectData = __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup;
        jobData3.m_MailSenderData = __TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup;
        jobData3.m_MailProducerData = __TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup;
        jobData3.m_MailSenderQueue = mailSenderQueue;
        jobData3.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
        JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle);
        m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
        mailSenderQueue.Dispose(jobHandle3);
        __TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        CitizeSleepJob jobData4 = default(CitizeSleepJob);
        jobData4.m_CurrentBuildingData = __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup;
        jobData4.m_CitizenPresenceData = __TypeHandle.__Game_Buildings_CitizenPresence_RW_ComponentLookup;
        jobData4.m_SleepQueue = sleepQueue;
        JobHandle jobHandle4 = IJobExtensions.Schedule(jobData4, jobHandle);
        sleepQueue.Dispose(jobHandle4);
        base.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
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
    public CitizenBehaviorSystem()
    {
    }
}
