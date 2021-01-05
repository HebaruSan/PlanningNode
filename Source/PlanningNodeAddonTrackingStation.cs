using KSP.UI.Screens;

namespace PlanningNode {

	/// <summary>
	/// Plugin behavior for the tracking station
	/// </summary>
	[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
	public class PlanningNodeAddonTrackingStation : PlanningNodeAddonBase {

		/// <summary>
		/// Initialize the plugin
		/// </summary>
		public PlanningNodeAddonTrackingStation() : base() { }

		/// <summary>
		/// This is called at creation
		/// </summary>
		protected override void Start()
		{
			base.Start();

			renderer.vessel = PlanetariumCamera.fetch.target?.vessel;

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Add(TrackingStationTargetChanged);
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		protected override void OnDisable()
		{
			base.OnDisable();

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Remove(TrackingStationTargetChanged);
		}

		/// <summary>
		/// Add us to the tracking station toolbar
		/// </summary>
		protected override ApplicationLauncher.AppScenes inToolbars => ApplicationLauncher.AppScenes.TRACKSTATION;

		/// <summary>
		/// Tracking station can't open maneuver nodes for editing
		/// </summary>
		protected override bool CanEdit => false;

		/// <summary>
		/// React to the user switching focus in tracking station
		/// </summary>
		private void TrackingStationTargetChanged(MapObject target)
		{
			// Keep the previous vessel if the new one is null, so we don't
			// lose access to vessel specific nodes.
			if (target?.vessel != null) {
				renderer.vessel = target.vessel;
			}
			if (PlanningNodesManager.Instance.NodesFor(renderer.vessel, true).Count < 1) {
				// No nodes, can't make any; disable
				launcher?.Disable(true);
			} else {
				launcher?.Enable(true);
			}
		}

	}
}
