# Nemesis Rising Tides

Reworks and rebalances [Rising Tides](https://thunderstore.io/package/TheMysticSword/RisingTides/) elites that keeps the core functionality but with different execution. Ideas funded by the community (but mostly me still). Individually togglable. Not affiliated with starstorm despite its name. Also adds 2 new(ish) elites.

<i title="Also try: [Nemesis Spikestrip](https://thunderstore.io/package/prodzpod/Nemesis_Spikestrip/)">RIP Spikestrip, i am the only brother left...</i>

## T1: Magnetic Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon1.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/1.png)
- Magnetic Elites will now create a zone that grants a buff to all nearby enemies. Getting hit by any of the enemies in the zone will sap money from you. This change is intended to screw melee less. Setting the zone to 0 will only apply this buff to the elite itself.
- Killing a magnetic elite will give more money based on the currently alive enemies in that area.
- Replaces On use with an explosion that saps money from all the enemies hit.

## T1: Oppressive Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon2.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/2.png)
- **NEW**: Magnetic elite's on hit effect as a separate type.
- Creates a small cylindrical area that increases gravity around it. On hit, disables jump completely for a short period.
- On use: Forcefully drop enemies around you.

## T1: Buffered Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon3.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/3.png)
- **NEW**: Barrier effects from Bismuth elites as a separate type.
- On hit, gains damage as barrier. When barrier is active, take less damage and no knockback. Barrier does not naturally decay.
- On death, give nearby enemies barrier.
- On use: gain barrier.

## T2: Nocturnal Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon4.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/4.png)
- Upgraded to Tier 2 - Now renamed to **Anglesite** elite, on top of the existing effect, increases attack and movement speed to all enemies nearby. 
  - **UPDATE Rising Tides CONFIG!**
  - Health Boost Coefficient, Damage Boost Coefficient should be adjusted to fit T2 stats.
- On-use invisibility will now only trigger if a player uses it - effectively removing it from the enemy.
- Blind effect is replaced by the [Artifact of Blindness](https://thunderstore.io/package/HIFU/ArtifactOfBlindness/) version.

Yes this is literally [WRB](https://thunderstore.io/package/TheBestAssociatedLargelyLudicrousSillyheadGroup/WellRoundedBalance/) Celestine. I'm stealing my own code again :trollshrug:  
- EXCEPT WRB IS NO MORE - Seeker of the Storm baby prod stocks 📈📈📈📈

Alternatively, you can disable the "zone" and have just the blind effect for T1 (configurable)

## T2: Aquamarine Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon5.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/5.png)
- Instead of being immune to damage, now releases a zone where every 3rd hit against the enemies in that area becomes nullified. This makes the debuff negatable by focusing on the elite first rather than being Plated Elite 2.
- Bubble attack is unchanged.

## T2: Bismuth Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon6.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/6.png)
- Added a configurable debuff blacklist.
- Removed barrier related functions.
- Replaced on-use with a blast that applies 3 random debuffs around the enemy.
- Renamed affix equipment name.

## T2: Onyx Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon7.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/7.png)
- Unchanged. we love onyx elite. (not that we dont love other elites but still)

## T2: Realgar Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/icon8.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisRisingTides/master/8.png)
- Changes on-use to the spike summon (to the current position). if spike is summoned, previously spawned spike disappears.
- Renamed affix equipment name.

## Other Changes
- [Blighted Elites](https://thunderstore.io/package/Moffein/BlightedElites/): Renamed to **Obsidian** Elite. (fits the T2 naming scheme)
- Rising Tides compatibility with affix-in-logbook mods ([WolfoQoL](https://thunderstore.io/package/Wolfo/WolfoQualityOfLife/), [ZetAspects](https://thunderstore.io/package/William758/ZetAspects/)) and better-description mods such as [BetterUI](https://thunderstore.io/package/XoXFaby/BetterUI/).
- Ability to set cooldown / enemy usage for each aspect on-use.

## Credits / Thanks
- [Mystic](https://thunderstore.io/package/TheMysticSword/): i mean yeah, also stole codes (i still love you :cry:)
- [HIFU](https://thunderstore.io/package/HIFU/): Anglesite / WRB Celestine / Artifact of Blindness post-processing
- [William758](https://thunderstore.io/package/William758/): Aspect Description  
- [Smxrez](https://thunderstore.io/package/Smxrez/): Idea guy (helped me come up with rework ideas somewhat)

## Changelog
- 1.0.4: added korean translation (thanks @ggang-b on git)
- 1.0.3: added config for blighted name change
- 1.0.2: fixed magnetic elites giving way too much money on death
- 1.0.1: VFX additions, icon changes, bugfixes, fixed typos