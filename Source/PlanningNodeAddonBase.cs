using UnityEngine;
using KSP.UI.Screens;

namespace PlanningNode {

	using MonoBehavior = MonoBehaviour;

	/// <summary>
	/// Base class for the scene-specific plugin behaviors,
	/// does everything they have in common.
	/// </summary>
	public abstract class PlanningNodeAddonBase : MonoBehavior {

		/// <summary>
		/// Initialize the plugin
		/// </summary>
		public PlanningNodeAddonBase() : base() { }

		/// <summary>
		/// Machine-readable name for this mod.
		/// Use this for directory/file names, etc.
		/// </summary>
		public const string Name = "PlanningNode";

		/// <summary>
		/// This is called at creation
		/// </summary>
		protected virtual void Start()
		{
			renderer = PlanetariumCamera.fetch.gameObject.AddComponent<PlanningNodeMarkersRenderer>();
			renderer.EditNode += editNode;

			// This event fires when KSP is ready for mods to add toolbar buttons
			GameEvents.onGUIApplicationLauncherReady.Add(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncher);

			// The game closes our dialog when you press Esc, listen to this to bring it back
			GameEvents.onGameUnpause.Add(OnGameUnpause);
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		protected virtual void OnDisable()
		{
			// Close and clean up editor UI
			launcher.SetFalse(true);

			// Clean up the markers
			renderer.EditNode -= editNode;
			Destroy(renderer);
			renderer = null;

			// The "dead" copy of our object will re-add itself if we don't unsubscribe to this!
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);

			// The game closes our dialog when you press Esc, listen to this to bring it back
			GameEvents.onGameUnpause.Remove(OnGameUnpause);

			// The launcher destroyed event doesn't always fire when we need it (?)
			RemoveLauncher();
		}

		#region App launcher

		/// <summary>
		/// Our toolbar button
		/// </summary>
		protected ApplicationLauncherButton launcher;

		/// <summary>
		/// Which toolbars this addon should use
		/// </summary>
		protected abstract ApplicationLauncher.AppScenes inToolbars { get; }

		/// <summary>
		/// Whether we can edit nodes
		/// </summary>
		protected abstract bool CanEdit { get; }

		/// <value>
		/// The icon to show for this mod in the app launcher
		/// </value>
		private static readonly Texture2D AppIcon = GameDatabase.Instance.GetTexture($"{Name}/Icons/{Name}", false);

		private void AddLauncher()
		{
			if (ApplicationLauncher.Ready && launcher == null)
			{
				launcher = ApplicationLauncher.Instance.AddModApplication(
					onAppLaunchToggleOn, onAppLaunchToggleOff,
					null,                null,
					null,                null,
					inToolbars,
					AppIcon);

				if (!CanEdit && PlanningNodesManager.Instance.NodesFor(null, true).Count < 1) {
					// No nodes, can't make any; disable
					launcher?.Disable(true);
				}

				string lockReason;
				if (!unlocked(out lockReason)) {
					launcher?.Disable(true);
				}

				launcher?.gameObject?.SetTooltip(
					"PlanningNode_mainTitle",
					launcher.IsEnabled 
						? "PlanningNode_mainTooltip"
						: string.IsNullOrEmpty(lockReason)
							? "PlanningNode_viewOnlyTooltip"
							: lockReason
				);
			}
		}

		private void RemoveLauncher()
		{
			if (launcher != null) {
				ApplicationLauncher.Instance.RemoveModApplication(launcher);
				launcher = null;
			}
		}

		/// <summary>
		/// Check whether the mod should be available for use in the current save.
		/// We need patched conics and flight planning.
		/// </summary>
		/// <param name="reason">User friendly explanation of why we're not available</param>
		/// <returns>
		/// true if unlocked, false if locked
		/// </returns>
		private static bool unlocked(out string reason)
		{
			if (ScenarioUpgradeableFacilities.Instance == null) {
				reason = "";
				return true;
			}
			var gVars        = GameVariables.Instance;
			var stationLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);
			var stationOK    = gVars.GetOrbitDisplayMode(stationLevel) == GameVariables.OrbitDisplayMode.PatchedConics;
			var missionLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.MissionControl);
			var missionOK    = gVars.UnlockedFlightPlanning(missionLevel);
			if (!stationOK) {
				reason = !missionOK ? "PlanningNode_unlockBothTooltip"
				                    : "PlanningNode_unlockStationTooltip";
				return false;
			} else {
				if (!missionOK) {
					reason = "PlanningNode_unlockMisConTooltip";
					return false;
				} else {
					reason = "";
					return true;
				}
			}
		}

		/// <summary>
		/// This is called when they click our toolbar button
		/// </summary>
		private void onAppLaunchToggleOn()
		{
			// Make a node if needed
			var nodes = PlanningNodesManager.Instance.NodesFor(renderer.vessel, true);
			if (nodes.Count < 1 && CanEdit) {
				PlanningNodesManager.Instance.nodes.Add(new PlanningNodeModel(
					renderer.vessel?.mainBody ?? FlightGlobals.GetHomeBody(),
					renderer.vessel));

				nodes = PlanningNodesManager.Instance.NodesFor(renderer.vessel, true);
			}
			if (nodes.Count > 0) {
				editNode(nodes[0]);
			}
		}

		/// <summary>
		/// This is called when they click our toolbar button again
		/// </summary>
		private void onAppLaunchToggleOff()
		{
			if (editor != null) {
				editor.ZoomBack();
				editor.DeleteMe -= OnNodeDeleted;
				editor.CloseMe -= OnClose;
				Destroy(editor);
				editor = null;
			}
			if (editDialog != null) {
				editDialog.Dismiss();
				editDialog = null;
			}
		}

		#endregion App launcher

		private void editNode(PlanningNodeModel toEdit)
		{
			if (editor != null) {
				StartCoroutine(editor.SwitchTo(toEdit));
			}
			if (editor == null) {
				editor = gameObject.AddComponent<PlanningNodeEditor>();
				editor.canEdit = CanEdit;
				editor.DeleteMe += OnNodeDeleted;
				editor.CloseMe += OnClose;
				editor.editingNode = toEdit;
			}

			openDialog(toEdit);
		}

		private void openDialog(PlanningNodeModel toEdit)
		{
			if (editDialog == null) {
				editDialog = new PlanningNodeEditDialog(toEdit, CanEdit);
				editDialog.CloseDialog += () => launcher.SetFalse(true);
				editDialog.NewNode += () => {
					var nd = new PlanningNodeModel(
						renderer.vessel?.mainBody ?? FlightGlobals.GetHomeBody(),
						renderer.vessel);
					PlanningNodesManager.Instance.nodes.Add(nd);
					editNode(nd);
				};
				editDialog.DeleteNode += () => {
					PlanningNodesManager.Instance.nodes.Remove(editDialog.editingNode);
					OnNodeDeleted();
				};
				editDialog.PrevNode += () => editNode(PlanningNodesManager.Instance.PrevNode(renderer.vessel, editDialog.editingNode));
				editDialog.NextNode += () => editNode(PlanningNodesManager.Instance.NextNode(renderer.vessel, editDialog.editingNode));
				editDialog.BodyChanged += OnBodyChanged;
				editDialog.WarpTo += WarpTo;
				editDialog.Show(launcher.GetAnchor());
			} else {
				// Already open, just switch to this node
				editDialog.editingNode = toEdit;
			}
		}

		private void OnBodyChanged(CelestialBody b)
		{
			editDialog.editingNode.origin = b;
			editNode(editDialog.editingNode);
		}

		private void OnNodeDeleted()
		{
			// Unclick the button when the user deletes our node
			launcher.SetFalse(true);
			if (!CanEdit && PlanningNodesManager.Instance.NodesFor(renderer.vessel, true).Count < 1) {
				// Can't do much more if we delete our last node in the tracking station
				launcher?.Disable(true);
			}
		}

		private void OnClose()
		{
			// Unclick the button when the user edits another node
			launcher.SetFalse(true);
		}

		private void OnGameUnpause()
		{
			// Re-open our dialog after you un-pause the game
			if (editor?.editingNode != null && editDialog != null) {
				editDialog.Dismiss();
				editDialog = null;
				openDialog(editor.editingNode);
			}
		}

		private void WarpTo(PlanningNodeModel node)
		{
			if (TimeWarp.CurrentRate > 1) {
				TimeWarp.fetch.CancelAutoWarp();
				TimeWarp.SetRate(0, false);
			} else {
				TimeWarp.fetch.WarpTo(node.burnTime - WarpBuffer(node.origin, (float?)renderer.vessel?.orbit.ApA ?? 100000f));
			}
		}

		/// <summary>
		/// Calculate a good buffer for transferring from the given orbit
		/// </summary>
		/// <param name="parent">Body we're orbiting</param>
		/// <param name="apoapsis">Furthest distance from parent</param>
		/// <returns>
		/// One fourth the orbital period of a transfer orbit from your apoapsis to the edge of the sphere of influence;
		/// From LKO this is about 10d, and escaping to Duna takes 3d
		/// </returns>
		private static float WarpBuffer(CelestialBody parent, float apoapsis)
		{
			return 0.25f * OrbitalPeriod(parent, (float)parent.sphereOfInfluence, apoapsis);
		}

		/// <returns>
		/// Period of an orbit with the given characteristics.
		/// </returns>
		/// <param name="parent">Body around which to orbit</param>
		/// <param name="apoapsis">Greatest distance from center of parent</param>
		/// <param name="periapsis">Smallest distance from center of parent</param>
		private static float OrbitalPeriod(CelestialBody parent, float apoapsis, float periapsis)
		{
			float r = 0.5f * (apoapsis + periapsis);
			return 2 * Mathf.PI * Mathf.Sqrt(r * r * r / (float)parent.gravParameter);
		}

		/// <summary>
		/// Manages the drawing of the markers
		/// </summary>
		protected PlanningNodeMarkersRenderer renderer   = null;

		private PlanningNodeEditor     editor     = null;
		private PlanningNodeEditDialog editDialog = null;
	}

}
