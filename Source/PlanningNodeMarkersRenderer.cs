using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNode {

	using MonoBehavior = MonoBehaviour;

	/// <summary>
	/// Makes sure the appropriate escape markers are visible and up to date
	/// </summary>
	public class PlanningNodeMarkersRenderer : MonoBehavior {

		/// <summary>
		/// Iniitalize a renderer
		/// </summary>
		public PlanningNodeMarkersRenderer() : base() { }

		/// <summary>
		/// The vessel for which to show vessel-specific nodes
		/// </summary>
		public Vessel vessel;

		/// <summary>
		/// Fired when the user clicks a marker
		/// </summary>
		public event Action<PlanningNodeModel> EditNode;

		private void OnDisable()
		{
			DestroyAll();
		}

		private void AddMarker()
		{
			var gob = new GameObject();
			gameObjects.Add(gob);
			markers.Add(gob.AddComponent<PlanningNodeMarker>());
			markers[markers.Count - 1].EditMe += MarkerClicked;
		}

		private void DestroyMarker(int which)
		{
			if (markers.Count >= which) {
				markers[which].EditMe -= MarkerClicked;
				Destroy(markers[which]);
				markers.RemoveAt(which);
			}
			if (gameObjects.Count >= which) {
				Destroy(gameObjects[which]);
				gameObjects.RemoveAt(which);
			}
		}
		
		private void DestroyAll()
		{
			while (markers.Count > 0) {
				DestroyMarker(markers.Count - 1);
			}
		}

		private void MarkerClicked(PlanningNodeModel whichNode)
		{
			EditNode?.Invoke(whichNode);
		}

		private void OnPreCull()
		{
			var mgr = PlanningNodesManager.Instance;
			if (mgr != null && (HighLogic.LoadedScene == GameScenes.TRACKSTATION
				|| (HighLogic.LoadedScene == GameScenes.FLIGHT && MapView.MapIsEnabled))) {

				var nodes = mgr.NodesFor(vessel, true);
				nodes.RemoveAll(nd => nd.deltaV.IsZero());
				for (int i = 0; i < nodes.Count; ++i) {
					while (i >= markers.Count) {
						AddMarker();
					}
					markers[i].myNode = nodes[i];
					markers[i].MoveTo(nodes[i].origin, vessel, nodes[i].BurnDirection());
				}
				for (int j = markers.Count - 1; j >= nodes.Count; --j) {
					DestroyMarker(j);
				}
			} else {
				// Not supposed to be showing anything
				DestroyAll();
			}
		}

		private List<GameObject>         gameObjects = new List<GameObject>();
		private List<PlanningNodeMarker> markers     = new List<PlanningNodeMarker>();
	}

}
