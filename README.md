
# Economy Rebalance Mod
This mod allows to build a self-sufficient city in terms of resources production and consumption. Except for a single change in HouseholdBehaviorSystem, it achieves that by only modifying existing game parameters like companies profitability, resources price and base consumption or workplaces complexity.

**Important note.**
This is the first version released mostly for testing purposes. The feedback about how companies and economy behave is appreciated and welcomed. Please see the Support paragraph below on how to reach me.
However, if you decide to try out the mod, please make sure to turn off other mods that affect economy, resource management and companies. While the mod technically will work as advertised, the results produced will be distorted and not very useful for further balancing.

### Key features.
 - Increases output from Extractors allowing for bigger cities to be self-sufficient. Resources on standard maps should suffice for cities >300k.
 - The individual consumption does not skyrocket in bigger cities. See "Consumption Fix" below for details.
 - Adjusts and tweaks prices and consumption for some resources to make their consumption a bit more balanced and realistic.
 - Decreases the profitability of companies. This slows down their leveling and the increase of land value is much slower.
 - Tweaks a few production chains, to better support production of resources that are consumed in high quantities.
 - Tweaks workplace definition for better alignment with available workforce.
 
### Comparison to vanilla city
 - It is possible to turn on the mod on an existing city. The effects will be seen after few in-game hours, but it will take a few in-game days for all processes to fully adjust.
 - Output of Extractors should increase by 30%-50%. Conversly, the utilization will also increase, it may be higher than renewal rate in some cases.
 - Individual consumption will decrease, the more the bigger the city is. For cities >200k it may decrease by 80% or even more.
 - There will be shifts in production of various resources.
 - The land value will slowly start to decrease. It will look for a new equilibrium, but it will take even a few in-game days.
 - Companies with profitability <255 will appear. The "Company Profitability" infoview will show other colors than green.
 - Companies will require more well educated and much more highly educated people. They may start employ commuters.
 - Companies will be more responsive to taxes. Taxes above 15% will actually start hurting companies.
 - Companies that are not profitable enough will go bankrupt.
 - There will be lower demand for offices, since there will be less demand for immaterial resources.

## Features

### Profitability
 - Commercial companies and offices have profitability cut drastically, by 80%-90%. Their huge profitability in the vanilla game is the reason for quick leveling and skyrocketing land value.
 - Industrial companies have smaller profitability, usually cut down by 20%-30%.
 - Warehouses have increased profitability, to quicken their leveling.
 
### Resources production and consumption
 - Increased production of Food, Convenience Food, Beverages and Textiles - these resources are the most consumed by cims.
 - Decreased production of Immaterial Office resources - to cut down the huge overproduction by the high density offices.
 - BioRefinery doesn't use so much grain to produce petrochemicals.
 - Cims won't eat pharmaceuticals for breakfast.
 
### Workplaces distribution
 - The structure of workfoce education does not match the workplaces distribution, by a huge factor.
 - In the vanilla game, the population eventually becomes well and highly educated, and there is not enough jobs for them. As a result, most of highly educated cims will be underemployed.
 - The mod "shifts" workplaces distribution to higher education levels. There will be much more demand for well and highly educated cims.

### Consumption fix
  - In the vanilla game, average consumption PER CIM increases expotentially with the size of the city. Yes, read that carefuly again. Not the overall cosumption, because this is obvious, but on average PER CIM.
  - And not by like 10% or 20% because this is possible due to e.g. wealth factors, but it is 5-6 times more in bigger cities (300k) than in smaller cities (30k).
  - As a result, in bigger cities the demand for some resources skyrockets to absurd amounts.
  - The mod fixes that and the average consumption per cim is stable.

## Technical

### Requirements and Compatibility
- Cities Skylines II version 1.0.19f1 or later; check GitHub or Discord if the mod is compatible with the latest game version.
- BepInEx 5.
- Modified systems: HouseholdBehaviorSystem.

### Installation
1. Place the `RealEco.dll` file in your BepInEx `Plugins` folder.

### Known Issues
- Nothing atm.

### Changelog
- v0.1.0 (2024-02-15)
  - Initial build.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/CS2-RealEco).
- You may also leave comments on [Discord1](https://discord.com/channels/1169011184557637825/1207641575362920508) or [Discord2](https://discord.com/channels/1024242828114673724/1207641284647587922).

## Disclaimers and Notes

> [!NOTE]
The mod uses Cities: Skylines 2 Mod Template by Captain-Of-Coit.
