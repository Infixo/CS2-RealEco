using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.UI;
using Game.Simulation; // TODO: use UIUpdateState and Advance() eventully...

namespace RealEco.Systems;

[CompilerGenerated]
public class CommercialUISystem : UISystemBase
{
    public struct DemandData
    {
        public Resource Resource;
        //public FixedString32Bytes Name; // resource name
        public int Demand; // company demand
        public int Building; // building demand
        public int Free; // free properties
        public int Companies; // num of companies
        public int Workers; // num of workers
        public int SvcFactor; // service availability
        public int SvcPercent;
        public int CapFactor; // sales capacity
        public int CapPercent;
        public int CapPerCompany;
        public int WrkFactor; // employee ratio
        public int WrkPercent;
        public int EduFactor; // educated employees
        public int TaxFactor; // tax factor
        //public FixedString512Bytes Details;

        public DemandData(Resource resource) { Resource = resource; } // Name = EconomyUtils.GetNameFixed(EconomyUtils.GetResource(resource)); }
    }

    private static void WriteData(IJsonWriter writer, DemandData data)
    {
        writer.TypeBegin("DemandData");
        writer.PropertyName("resource");
        writer.Write(data.Resource.ToString());
        writer.PropertyName("demand");
        writer.Write(data.Demand);
        writer.PropertyName("building");
        writer.Write(data.Building);
        writer.PropertyName("free");
        writer.Write(data.Free);
        writer.PropertyName("companies");
        writer.Write(data.Companies);
        writer.PropertyName("workers");
        writer.Write(data.Workers);
        writer.PropertyName("svcfactor");
        writer.Write(data.SvcFactor);
        writer.PropertyName("svcpercent");
        writer.Write(data.SvcPercent);
        writer.PropertyName("capfactor");
        writer.Write(data.CapFactor);
        writer.PropertyName("cappercent");
        writer.Write(data.CapPercent);
        writer.PropertyName("cappercompany");
        writer.Write(data.CapPerCompany);
        writer.PropertyName("wrkfactor");
        writer.Write(data.WrkFactor);
        writer.PropertyName("wrkpercent");
        writer.Write(data.WrkPercent);
        writer.PropertyName("edufactor");
        writer.Write(data.EduFactor);
        writer.PropertyName("taxfactor");
        writer.Write(data.TaxFactor);
        //writer.PropertyName("details");
        //writer.Write(data.Details.ToString());
        writer.TypeEnd();
    }

    private const string kGroup = "realEco";

    private SimulationSystem m_SimulationSystem;

    private RawValueBinding m_uiResults;

    private NativeArray<DemandData> m_Results;

    // Set gameMode to avoid errors in the Editor
    public override GameMode gameMode => GameMode.Game;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>(); // TODO: use UIUpdateState eventually
        // data
        AddBinding(m_uiResults = new RawValueBinding(kGroup, "commercialDemand", delegate (IJsonWriter binder)
        {
            binder.ArrayBegin(m_Results.Length);
            for (int i = 0; i < m_Results.Length; i++)
                WriteData(binder, m_Results[i]);
            binder.ArrayEnd();
        }));
        Plugin.Log("CommercialUISystem created.");
    }

    /*
    [Preserve]
    protected override void OnDestroy()
    {
        //m_Results.Dispose();
        base.OnDestroy();
    }
    */

    [Preserve]
    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 64 != 17)
            return;
        // get the data
        m_Results = CommercialDemandSystem_Patches.m_DemandData;
        m_uiResults.Update(); // update UI
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        //__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public CommercialUISystem()
    {
    }
}
