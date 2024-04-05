using Unity.Collections;
using Unity.Entities;
using Game;
using Game.Prefabs;

namespace RealEco.Systems;

public partial class CompanyBrandsInitializeSystem : GameSystemBase
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
                ComponentType.ReadOnly<Game.Common.Created>(),
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<CommercialCompanyData>(),
            }
        });
        RequireForUpdate(m_PrefabQuery);
        Mod.log.Info("CompanyBrandsInitializeSystem created.");
    }

    protected override void OnUpdate()
    {
        Mod.log.Info($"CompanyBrandsInitializeSystem.OnUpdate: {m_PrefabQuery.CalculateEntityCount()}");
        foreach (Entity companyEntity in m_PrefabQuery.ToEntityArray(Allocator.Temp))
        {
            if (m_PrefabSystem.TryGetPrefab<CompanyPrefab>(companyEntity, out CompanyPrefab companyPrefab))
            {
                Mod.log.Info($"CompanyBrandsInitializeSystem: patching {companyPrefab.name}");

                string[] brands = null;
                switch (companyPrefab.name)
                {
                    case "Commercial_SoftwareStore": brands = PrefabStore.BrandsSoftware; break;
                    case "Commercial_TelecomStore": brands = PrefabStore.BrandsTelecom; break;
                    case "Commercial_FinancialStore": brands = PrefabStore.BrandsFinancial; break;
                    case "Commercial_MediaStore": brands = PrefabStore.BrandsMedia; break;
                    default:
                        Mod.log.Warn($"CompanyBrandsInitializeSystem: unknown new company {companyPrefab.name}. Skipping.");
                        break;
                }

                if (brands == null) continue; // unknown company

                foreach (string brandName in brands)
                {
                    if (PrefabStore.TryGetPrefabAndEntity("BrandPrefab", brandName, out PrefabBase brandPrefab, out Entity brandEntity))
                    {
                        Mod.LogIf($"{brandPrefab.GetType().Name}.{brandPrefab.name}: patching");
                        // Update entity's buffer with a brand and an affiliated brand
                        // Based on CompanyInitializeSystem.OnUpdate
                        m_PrefabSystem.GetBuffer<CompanyBrandElement>(companyPrefab, isReadOnly: false).Add(new CompanyBrandElement(brandEntity));
                        m_PrefabSystem.GetBuffer<AffiliatedBrandElement>(companyPrefab, isReadOnly: false).Add(new AffiliatedBrandElement { m_Brand = brandEntity });
                        Mod.Log($"CompanyBrandsInitializeSystem: {companyPrefab.name}: uses {brandName}");
                    }
                    else
                        Mod.log.Warn($"CompanyBrandsInitializeSystem: Failed to retrieve BrandPrefab {brandName} from the PrefabSystem. Brands not added.");
                }
            }
            else
            {
                Mod.log.Warn($"CompanyBrandsInitializeSystem: Failed to retrieve CompanyPrefab from the PrefabSystem.");
            }
        }
        base.Enabled = false;
    }

    public CompanyBrandsInitializeSystem()
    {
    }
}
