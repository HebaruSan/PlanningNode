# Planning Node

This repository contains a mod for Kerbal Space Program. Its goal is to create a stock-alike method for planning interplanetary transfers that preserves the flexibility and "learn by doing" approach of stock maneuver nodes.

See the [README in the download] for installation and usage instructions.

See the [README in the Localization folder] for instructions for adding or improving translations for languages other than English.

[README in the download]: GameData/PlanningNode/README.md

[README in the Localization folder]: GameData/PlanningNode/Localization/README.md

## Download

The [latest version] is available on Github.

[latest version]: https://github.com/HebaruSan/PlanningNode/releases/latest

## How to donate

[![Donate][Donation image]][Donation link]

[Donation link]: https://paypal.me/HebaruSan

[Donation image]: https://i.imgur.com/M9m07Qw.png

## Building

1. Install mono
2. If you don't have Steam, make `Source/KSP_Data` a symbolic link to your game's `KSP_Data` folder
3. `make`

## References

This was [5thHorseman]/[Superfluous J]'s idea:

[5thHorseman]: https://github.com/5thHorseman/
[Superfluous J]: https://forum.kerbalspaceprogram.com/index.php?/profile/73725-superfluous-j/

- ["Planning Nodes" outside of flight mode for interplanetary transfers](https://forum.kerbalspaceprogram.com/index.php?/topic/70998-quotplanning-nodesquot-outside-of-flight-mode-for-interplanetary-transfers/)
- [I just want to be able to place "planning nodes" on planetary orbits](https://forum.kerbalspaceprogram.com/index.php?/topic/110236-transfer-window-stock-integration/&tab=comments#comment-1961697)
- [a "Mission Planning Node" that works just like a maneuver node](https://forum.kerbalspaceprogram.com/index.php?/topic/147304-transfer-window-visualization/&do=findComment&comment=2747442)
- [a planning node that works just like a maneuver node but is on planet and moon orbits instead of craft orbits](https://forum.kerbalspaceprogram.com/index.php?/topic/182440-what-stock-features-are-not-fully-developed/&do=findComment&comment=3562995)
- [You should be able to make "planning nodes"](https://forum.kerbalspaceprogram.com/index.php?/topic/199111-how-are-you-meant-to-figure-out-transfer-windows-in-stock/&do=findComment&comment=3903587)

Three years later I started promoting the same concept. I thought that I came up with it on my own, but it is possible that I saw some of the above posts at some point and they sank into my subconscious.

- [If there was an obvious way to "switch to" a planet to plan maneuvers from it, transfer windows would become much more apparent](https://forum.kerbalspaceprogram.com/index.php?/topic/163363-where-can-you-see-detailed-orbital-anglesinfo/&do=findComment&comment=3125991)
- [![image](https://i.imgur.com/LBa1YaR.png)](https://forum.kerbalspaceprogram.com/index.php?/topic/163363-where-can-you-see-detailed-orbital-anglesinfo/&do=findComment&comment=3126635)
- [Imagine if you could go to map view and click a special new button to create a "meta maneuver node" on your parent body's orbit rather than on your own craft's orbit](https://forum.kerbalspaceprogram.com/index.php?/topic/199111-how-are-you-meant-to-figure-out-transfer-windows-in-stock/&do=findComment&comment=3903549)

I have a rule that if I catch myself pining for something on the forum multiple times, and no one else starts working on it, I have to; hence this mod.

How to draw orbits!

- [[SOLVED] Drawing orbits with no attached vessel](https://forum.kerbalspaceprogram.com/index.php?/topic/143101-solved-drawing-orbits-with-no-attached-vessel/)
- https://github.com/DBooots/ESLDBeacons/blob/master/Source/HailerGUI.cs

## Acknowledgements

- Thanks to [5thHorseman] for originating and tirelessly advocating for the idea
- Thanks to @DBooots for posting about how to render your own orbits
- Thanks to the good folks of the Discord for random help when I needed it
- Thanks to my wife for assistance with the icon
