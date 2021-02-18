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

				launcher?.gameObject?.SetTooltip(
					"PlanningNode_mainTitle",
					launcher.IsEnabled 
						? "PlanningNode_mainTooltip"
						: "PlanningNode_viewOnlyTooltip"
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
		/// This is called when they click our toolbar button
		/// </summary>
		private void onAppLaunchToggleOn()
		{
			// Make a node if needed
			var nodes = PlanningNodesManager.Instance.NodesFor(FlightGlobals.ActiveVessel, true);
			if (nodes.Count < 1 && CanEdit) {
				PlanningNodesManager.Instance.nodes.Add(new PlanningNodeModel(
					FlightGlobals.ActiveVessel?.mainBody ?? FlightGlobals.GetHomeBody(),
					FlightGlobals.ActiveVessel));

				nodes = PlanningNodesManager.Instance.NodesFor(FlightGlobals.ActiveVessel, true);
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
						FlightGlobals.ActiveVessel?.mainBody ?? FlightGlobals.GetHomeBody(),
						FlightGlobals.ActiveVessel);
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

		#endregion App launcher

		/// <summary>
		/// Manages the drawing of the markers
		/// </summary>
		protected PlanningNodeMarkersRenderer renderer   = null;

		private PlanningNodeEditor          editor     = null;
		private PlanningNodeEditDialog      editDialog = null;
	}

}
