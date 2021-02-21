using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace PlanningNode {

	/// <summary>
	/// Plugin behavior for the flight scene
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class PlanningNodeAddonFlight : PlanningNodeAddonBase {

		/// <summary>
		/// Initialize the plugin
		/// </summary>
		public PlanningNodeAddonFlight() : base() { }

		/// <summary>
		/// This is called at creation
		/// </summary>
		protected override void Start()
		{
			base.Start();
			renderer.vessel = FlightGlobals.ActiveVessel;
			MapView.OnExitMapView += OnExitMapView;
			GameEvents.onVesselChange.Add(OnVesselChange);
			GameEvents.onManeuverNodeSelected.Add(OnManeuverNodeSelected);
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		protected override void OnDisable()
		{
			base.OnDisable();
			MapView.OnExitMapView -= OnExitMapView;
			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onManeuverNodeSelected.Remove(OnManeuverNodeSelected);
		}

		/// <summary>
		/// Add us to the map view toolbar
		/// </summary>
		protected override ApplicationLauncher.AppScenes inToolbars => ApplicationLauncher.AppScenes.MAPVIEW;

		private static readonly Rect    spriteRect     = new Rect(0, 0, 46, 48);
		private static readonly Vector2 spritePivot    = new Vector2(0.5f, 0.5f);
		private static readonly Sprite  AutoTimeSprite = Sprite.Create(
			GameDatabase.Instance.GetTexture($"{Name}/Icons/AutoTime_Normal", false),
			spriteRect, spritePivot);
		private static readonly SpriteState AutoTimeSpriteState = new SpriteState() {
			highlightedSprite = Sprite.Create(
				GameDatabase.Instance.GetTexture($"{Name}/Icons/AutoTime_Highlight", false),
				spriteRect, spritePivot),
			pressedSprite     = Sprite.Create(
				GameDatabase.Instance.GetTexture($"{Name}/Icons/AutoTime_Active", false),
				spriteRect, spritePivot),
			disabledSprite    = Sprite.Create(
				GameDatabase.Instance.GetTexture($"{Name}/Icons/AutoTime_Disabled", false),
				spriteRect, spritePivot)
		};

		private void OnManeuverNodeSelected()
		{
			var nd = OpenManeuver(renderer.vessel);
			if (nd != null) {
				// Make a copy of the plus-orbit button
				var plusObj = nd.attachedGizmo.plusOrbitBtn.gameObject;
				GameObject btnGameObj = Instantiate<GameObject>(plusObj);
				btnGameObj.transform.SetParent(plusObj.transform.parent);
				btnGameObj.transform.localPosition = new Vector3(150, -60, 0);
				btnGameObj.transform.localScale    = Vector3.one;
				btnGameObj.SetTooltip("PlanningNode_AutoTimeTooltip");
				btnGameObj.SetActive(true);

				Button btn = btnGameObj.GetComponent<Button>();
				btn.onClick.RemoveAllListeners();
				btn.onClick.AddListener(OnAutoTimeClick);
				btnGameObj.GetComponentInChildren<Image>().sprite = AutoTimeSprite;
				btn.spriteState = AutoTimeSpriteState;
			}
		}

		private ManeuverNode OpenManeuver(Vessel v)
		{
			var nodes = v.patchedConicSolver.maneuverNodes;
			for (int i = 0; i < nodes.Count; ++i) {
				ManeuverNode nd = nodes[i];
				if (nd.attachedGizmo != null) {
					return nd;
				}
			}
			return null;
		}

		private void OnAutoTimeClick()
		{
			Mouse.Left.ClearMouseState();
			var nd = OpenManeuver(renderer.vessel);
			if (nd != null) {
				var plnNd = PlanningNodesManager.Instance.ClosestExcessV(renderer.vessel, renderer.vessel.mainBody);
				if (plnNd != null) {
					var newTime = plnNd.BestManeuverTime(renderer.vessel, nd.UT);
					if (newTime.HasValue) {
						nd.attachedGizmo.orbitsAdded += (int)Math.Round((newTime.Value - nd.UT) / renderer.vessel.orbit.period);
						nd.UT = newTime.Value;
						renderer.vessel.patchedConicSolver.UpdateFlightPlan();
					}
				}
				nd.attachedGizmo?.SetMouseOverGizmo(true);
			}
		}

		/// <summary>
		/// Map view can open maneuver nodes for editing
		/// </summary>
		protected override bool CanEdit => true;

		private void OnExitMapView()
		{
			// Unclick the button when the user closes map view
			launcher.SetFalse(true);
		}

		private void OnVesselChange(Vessel v)
		{
			renderer.vessel = v;
		}

	}
}
