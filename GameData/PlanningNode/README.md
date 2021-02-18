# Planning Node

This download contains a mod for Kerbal Space Program. Its goal is to create a stock-alike method for planning interplanetary transfers that preserves the flexibility and "learn by doing" approach of stock maneuver nodes.

See the [README in the repo] for references and additional acknowledgements.

See the [README in the Localization folder] for instructions for adding or improving translations for languages other than English.

[README in the repo]: https://github.com/HebaruSan/PlanningNode/blob/master/README.md

[README in the Localization folder]: Localization/README.md

## Installation

Unzip the PlanningNode folder into your GameData folder. The structure should look like this:

    Kerbal Space Program
    +-- GameData
        +-- PlanningNode
            +-- Icons
            +-- Localization
            +-- Plugins

## Usage

Planning nodes can be edited in the map view and viewed in the tracking station.

1. Launch or switch to a vessel.
2. Open the map view.
3. Set your target body so you will be able to see the close approach markers.
4. Click the Planning Node icon in the toolbar (![icon](Icons/PlanningNode.png)). A planning node will be created and opened for editing, and the view will zoom to display it.

   ![created]
5. Use the node editing tools to set up the transfer you wish to perform (drag the node, add prograde/normal/radial components, etc.). You can also choose a name and color for this transfer and determine whether it should be shown for all vessels or just the current vessel in the editing window next to the toolbar button.

   ![transfer]

   ![renamed]
6. Click the close button in the Planning Node window or click the toolbar button again. The view will zoom back to your craft, and a three-ring marker will appear showing the time and excess V needed to execute your transfer. (Note that the excess V is **not** your burn delta V, but rather how much faster than escape velocity you need to go.)

   ![marker]
7. Create a stock maneuver node and increase its delta V until you are escaping the current sphere of influence; the planning node's timing and excess V displays will switch from absolute values to the difference between them and your current escape.
8. It's usually easiest to match the excess V first, so do that. This step is somewhat forgiving; you can often get within a few dozen m/s and call it good enough.
9. Adjust the time and normal/antinormal handles till your point of escape lines up with the planning node's bulls-eye marker. This tends to be a bit forgiving as well, so don't worry about hitting the exact center. Excitedly use the phrase "five by five" in a sentence.
10. Right click the node and use the forward/back orbit buttons to match up the times; if you are too early, the planning node will the time difference in the usual format, and if you are too late, the time difference will appear in parentheses. This is the most forgiving step because transfer windows are generally weeks long. Note that the stock controls can require **many** clicks here, so you may want to install a maneuver editing mod (not included).

    ![escape]
11. Zoom out to solar orbit to see how close you are to your plan. You may already have an encounter!

[created]: https://raw.githubusercontent.com/HebaruSan/PlanningNode/master/screenshots/created.png
[transfer]: https://raw.githubusercontent.com/HebaruSan/PlanningNode/master/screenshots/transfer.png
[renamed]: https://raw.githubusercontent.com/HebaruSan/PlanningNode/master/screenshots/renamed.png
[marker]: https://raw.githubusercontent.com/HebaruSan/PlanningNode/master/screenshots/marker.png
[escape]: https://raw.githubusercontent.com/HebaruSan/PlanningNode/master/screenshots/escape.png

Other notes:

- You can click the text labels for a planning node to edit it.
- You can use the "New" button in the dialog to create multiple nodes for the same vessel, and the `<` and `>` buttons next to the name field to switch to other nodes.
- Editing in the tracking station would be desirable but is not possible because the stock maneuver editor crashes outside of the flight scene's map view.
- Similarly, you may experience extreme log spam in some instances while editing nodes due to the stock maneuver editor being extremely sensitive about being used in unexpected ways (and the difficulty of figuring out what it takes to pacify it).
- Blizzy's toolbar is not and will not be supported. 0.23.5 was a **long** time ago.

## How to donate

[![Donate][Donation image]][Donation link]

[Donation link]: https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=7H2LCH6SP7TTE&lc=US&item_name=HebaruSan_Mods&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted

[Donation image]: https://i.imgur.com/M9m07Qw.png

<!--
## Acknowledgements

So far there are no translations, but this section should exist.
-->
