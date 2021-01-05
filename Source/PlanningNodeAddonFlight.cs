using KSP.UI.Screens;

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
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		protected override void OnDisable()
		{
			base.OnDisable();
			MapView.OnExitMapView -= OnExitMapView;
			GameEvents.onVesselChange.Remove(OnVesselChange);
		}

		/// <summary>
		/// Add us to the map view toolbar
		/// </summary>
		protected override ApplicationLauncher.AppScenes inToolbars => ApplicationLauncher.AppScenes.MAPVIEW;

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
