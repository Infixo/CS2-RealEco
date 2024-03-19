# Economy Rebalance Mod
This mod allows to build a self-sufficient city in terms of resources production and consumption. It achieves that mostly by modifying existing game parameters like companies profitability, resource prices and base consumption or workplaces complexity. The mod's effects become more visible and useful the bigger the city is (>100k).

Version 0.5 includes modded commercial demand that gives more control over the commercials in your city. The increased taxes sensitivity allows to manually control what companies settle in the city.

Version 0.4 includes new commercial companies and allows to selectively toggle key mod features: new parameters, consumption fix, new companies, modded commercial demand. Use BepInEx config `RealEco.cfg` to enable or disable the features.

**Important note.**
Tuning and balancing is an iterative process. The feedback about how companies and economy behave is highly appreciated and welcomed. Please see the Support paragraph below on how to reach me. If you decide to try out the mod, please make sure to turn off other mods that affect economy, resource management and companies. While the mod technically will work as advertised, the results produced may be distorted and not very useful for further balancing.

### Key features
 - (v0.3) Configuration in the xml file can be easily modified.
 - Increases output from Extractors allowing for bigger cities to be self-sufficient. Resources on standard maps should suffice for cities >300k.
 - The individual consumption does not skyrocket in bigger cities. See "Consumption Fix" below for details.
 - Adjusts and tweaks prices and consumption for some resources to make their consumption a bit more balanced and realistic.
 - Decreases the profitability of companies. This slows down their leveling and the increase of land value is much slower.
 - Tweaks a few production chains, to better support production of resources that are consumed in high quantities.
 - Tweaks workplaces number and distribution for better alignment with available workforce.
 - Tweaks residential zones upkeep for better rent progression and leveling.
 - (v0.4) New commercial companies that sell immaterial resources.
 - (v0.5) Rebalanced commercial demand with a dedicated UI window.
 - (v0.5) Adjustments for lodging and leisure companies should result in more of them spawning in the city.
 
### Comparison to the vanilla city
 - It is possible to turn on the mod on an existing city. The effects will be seen after few in-game hours, but it will take a few in-game days for all processes to fully adjust.
 - Output of Extractors should increase by 20%-40%. Conversly, the utilization will also increase, it may be higher than renewal rate in some cases.
 - Individual consumption will decrease, the more the bigger the city is. For cities >200k it may decrease by 80% or even more.
 - There will be shifts in production and consumption of various resources.
 - The land value will slowly start to decrease. It will look for a new equilibrium, but it will take even a few in-game days.
 - Companies with profitability <255 will appear. The "Company Profitability" infoview will show other colors than green.
 - Companies will require more well educated and much more highly educated people. They may start employ commuters.
 - Companies that are not profitable enough will go bankrupt.
 - There will be lower demand for offices, since there will be less demand for immaterial resources.
 - (v0.3) Companies will increase number of workplaces by 20%-30%.
 - (v0.5) More hotels and motels in the city.

## Features

### Toggle mod features (v0.4)
 - You can selectively toggle key mod features: new parameters, consumption fix, new companies and modded commercial demand. Use BepInEx config `RealEco.cfg` to enable or disable the features.

### Config.xml file (v0.3)
 - The prefab configuration is stored in the `Config.xml` file that comes together with the mod. Please do not confuse with `RealEco.cfg` file.
 - The file is loaded when the game is started, so for new params to take effect you need to restart the game.
 - Note for the future. Please note that new mod versions will overwrite the file so if you did any changes and want to keep them - make a backup before update and then reapply to the updated version.

### Profitability
 - Commercial companies and offices have profitability cut drastically, by 50%-70%. Their huge profitability in the vanilla game is the reason for quick leveling and skyrocketing land value.
 - Industrial companies have smaller profitability, usually cut down by 20%-30%.
 - (TURNED OFF, I am not sure it is working) Warehouses have increased profitability, to quicken their leveling.
 
### Resources production and consumption
 - Increased production of Food, Convenience Food, Beverages and Textiles - these resources are the most consumed by cims.
 - Decreased production of Immaterial Office resources - to cut down the huge overproduction by the high density offices.
 - BioRefinery doesn't use so much grain to produce petrochemicals.
 - Cims won't eat pharmaceuticals for breakfast.
 
### Workplaces distribution
 - The structure of workfoce education does not match the workplaces distribution, by a huge factor.
 - In the vanilla game, the population eventually becomes well and highly educated, and there is not enough jobs for them. As a result, most of highly educated cims will be underemployed.
 - The mod "shifts" workplaces distribution to higher education levels. There will be much more demand for well and highly educated cims.
 - (v0.3) Industrial buildings have increased workplace capacity by 30%, office buildings by 20% and commercial buildings by 20%. Can be turned off in the config file.

### Consumption fix
 - In the vanilla game, the average consumption PER CIM increases expotentially-like with the size of the city. Yes, read that carefully again. Not the overall cosumption, because that's obvious, but an average PER CIM.
 - And not by like 10% or 20% because this is possible due to e.g. wealth factors, but it is 5-6 times more in bigger cities (300k) than in smaller cities (30k).
 - As a result, in bigger cities the demand for some resources skyrockets to absurd amounts.
 - The mod fixes that and the average consumption per cim is stable.
  
### Garbage fee adjustment (v0.2)
 - The garbage fee in the vanilla game may cause some industrial companies to not level-up.
 - The mod lowers the garbage fee from 1.0 to 0.4 so those companies will level-up.
  
### Buildings' upkeep (v0.3)
 - High density residential zone have lower upkeep that allows for leveling up with much higher land value all arounnd. This SHOULD allow for reaching slowly Level 5.
 - Low Rent zones have much lower upkeep that allows for having homes with much lower rent than other residential zones.
 - Mixed and Medium have slightly tweaked upkeeps to better align with LowRena and HighDens.
 - As a result, the rent progression should be (from lowest to highest): Low Rent -> Mixed, High Density -> Medium Density -> Row Housing -> Low Density.
  
## New commercial companies (v0.4)
 - This feature is DISABLED by default. To enable, set `FeatureNewCompanies` to `true` in BepInEx config `RealEco.cfg`.
 - In the vanilla game cims don't buy immaterial resources at all, despite generating household needs for them. It is a by-product of the patch 1.0.15 where cims where forbiden to buy directly at industrial factories. However, somebody forgot that Offices are also Industries and thus cims stopped buying immaterial resources.
 - The mod creates 4 new commercial companies that sell immaterial resources. Think of them as Point of Sales for Offices that are Headquarters.
 - The mod also fixes an issue where commercial companies pay for resources but don't get them. This is the reason why they end up in a negative cash balance very often.
 - Please note. This feature will modify your save file. There will be 4 new prefabs. After disabling the mod or the feature, there will be warnings in the log. You may clean your savefile just by manually removing all new companies from the city.
 - Known issue. Atm the texts in the Statistics are not displayed correctly.

![NewStores](https://raw.githubusercontent.com/infixo/cs2-realeco/master/docs/newstores.png)

## Modded commercial demand (v0.5, EXPERIMENTAL)
 - This feature is DISABLED by default. To enable, set `FeatureCommercialDemand` to `true` in BepInEx config `RealEco.cfg`.
 - Gives more control over the commercials in your city. The increased taxes sensitivity allows to manually control what companies settle in the city.
 - Service availability - the neutral point is lowered to 50% (70% in vanilla).
 - Sales capacity - the neutral point is increased to 200% (100% and vanilla) but sensitivity is halved.
 - Employee capacity - the neutral point is moved to 90% (75% in vanilla).
 - Tax sensitivity - twice as much as vanilla.
 - As a result, the modded version is more repsonsive to actual consumption and how many customers are actually using shops - this is what Service Availability means. Sales capacity is calibrated to match the servive i.e. 50% of service matches 200% of capacity since it an inverse relation. Taxes sensitivity allows to manually boost or nerf the demand as needed.

## Commercial demand window (v0.5) 
 - This feature is DISABLED by default. To enable, set `FeatureCommercialDemand` to `true` in BepInEx config `RealEco.cfg`.
 - Description of the columns from left to right.
 - Demand (1): resource demand - key number that decides what companies will spawn; negative value will block spawning; the higher the number, the higher **probability** of a company being spawned.
 - Demand (2): zone demand - key number that decides what buildings will spawn; 0 means there is no demand.
 - Free: number of free properties (buildings) for companies selling the specific resource. Please note that as long as free properties exist and there is no company demand, no new buildings will spawn.
 - Num: number of companies.
 - Service: service available, factor and percentage vs. maximum service.
 - Sales capacity: factor, percentage vs. consumption, and average capacity per company.
 - Workers: factor, percentage vs. maximum workers, and total workers.
 - Edu: educated workforce factor, it basically says if there is available wworkforce that matches required education levels for a company.
 - Tax: tax factor.
  
![Commercial](https://raw.githubusercontent.com/infixo/cs2-realeco/master/docs/commercial.png)

## Technical

### Requirements and Compatibility
- Cities Skylines II version 1.0.19f1 or later; check GitHub or Discord if the mod is compatible with the latest game version.
- BepInEx 5.
- The mod will modify the savefile if new companies feature is enabled.
- Modified systems: HouseholdBehaviorSystem (when consumption fix is enabled), ResourceBuyerSystem (when new companies feature is enabled), CommercialDemandSystem (if modded commercial demand is enabled).

### Installation
1. Place the `RealEco.dll` file in your BepInEx `Plugins` folder.
2. The BepInEx config file `RealEco.cfg` is automatically created in BepInEx\config when the game is run once.

### Known Issues
- (v0.4) The resource labels in the Statistics window are not displayed correctly for new companies.

### Changelog
- v0.5.1 (2024-03-19)
  - Fixed issue with Mixed Zones classified as High Density.
  - Slightly boosts profitability of Saw Mill and Leisure Commercial companies.
- v0.5.0 (2024-03-14)
  - Modded commercial demand.
  - Commercial demand UI window.
  - Commercial companies balancing and tuning.
- v0.4.0 (2024-03-06)
  - Mod features can be selectively turned on/off.
  - Wages are slightly tweaked to decrease profitability when the company levels up. This also helps with increased number of workers.
  - Industrial Signature buildings have approx. 20% less workers.
  - New commercial companies selling immaterial resources - experimental feature, disabled by default.
- v0.3.0 (2024-03-01)
  - Configuration stored in the xml file.
  - More tuning and balancing.
  - New features: tweaks for residential building upkeeps and more industrial and office jobs.
  - Fixed issue where some industrial process tweaks were not applied.
- v0.2.0 (2024-02-18)
  - Tuning and balancing, especially in the early game.
  - Garbage fee adjustment.
- v0.1.0 (2024-02-15)
  - Initial build.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/CS2-RealEco).
- You may also leave comments on [Discord1](https://discord.com/channels/1169011184557637825/1207641575362920508) or [Discord2](https://discord.com/channels/1024242828114673724/1207641284647587922).

## Disclaimers and Notes

> [!NOTE]
The mod uses Cities: Skylines 2 Mod Template by Captain-Of-Coit.
