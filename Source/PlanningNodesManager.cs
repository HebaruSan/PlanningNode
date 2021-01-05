using UnityEngine;
using System.Collections.Generic;

namespace PlanningNode {

	using MonoBehavior = MonoBehaviour;

	/// <summary>
	/// In charge of managing all of our nodes, global or vessel specific.
	/// </summary>
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
	public class PlanningNodesManager : ScenarioModule {

		/// <summary>
		/// Initialize the manager, hopefully only when the game adds us a scenario
		/// </summary>
		public PlanningNodesManager() : base()
		{
			Instance = this;
		}

		/// <summary>
		/// Get our previously created nodes from the save file
		/// </summary>
		/// <param name="node">Our config node in the save file</param>
		public override void OnLoad(ConfigNode node)
		{
			nodes.Clear();
			var children = node.GetNodes(PlanningNodeModel.NodeName);
			for (int i = 0; i < children.Length; ++i) {
				nodes.Add(new PlanningNodeModel(children[i]));
			}
		}

		/// <summary>
		/// Put our nodes into the save file
		/// </summary>
		/// <param name="node">Our config node in the save file</param>
		public override void OnSave(ConfigNode node)
		{
			for (int i = 0; i < nodes.Count; ++i) {
				node.AddNode(nodes[i].GetConfigNode());
			}
		}

		/// <summary>
		/// The one copy of ourself that everyone else will use to get nodes
		/// </summary>
		public static PlanningNodesManager Instance { get; private set; }

		/// <summary>
		/// The nodes to save and load
		/// </summary>
		public readonly List<PlanningNodeModel> nodes = new List<PlanningNodeModel>();

		/// <summary>
		/// Return the nodes for the given vessel
		/// </summary>
		/// <param name="vessel">The vessel for which to return nodes</param>
		/// <param name="includeGlobal">True to include non-vessel-specific nodes, false to omit</param>
		/// <returns>
		/// List of nodes
		/// </returns>
		public List<PlanningNodeModel> NodesFor(Vessel vessel, bool includeGlobal)
		{
			var matched = new List<PlanningNodeModel>();
			for (int i = 0; i < nodes.Count; ++i) {
				PlanningNodeModel nd = nodes[i];
				if (nd.vessel == vessel || (nd.vessel == null && includeGlobal)) {
					matched.Add(nd);
				}
			}
			return matched;
		}

		/// <summary>
		/// Find the node before a given node
		/// </summary>
		/// <param name="vessel">The currently active vessel</param>
		/// <param name="cur">Node from which to start searching</param>
		/// <returns>
		/// Another node if any
		/// </returns>
		public PlanningNodeModel PrevNode(Vessel vessel, PlanningNodeModel cur)
		{
			var nodes = NodesFor(vessel, true);
			var curIdx = nodes.IndexOf(cur);
			return curIdx >= 0 ? nodes[(curIdx + nodes.Count - 1) % nodes.Count] : cur;
		}

		/// <summary>
		/// Find the node after a given node
		/// </summary>
		/// <param name="vessel">The currently active vessel</param>
		/// <param name="cur">Node from which to start searching</param>
		/// <returns>
		/// Another node if any
		/// </returns>
		public PlanningNodeModel NextNode(Vessel vessel, PlanningNodeModel cur)
		{
			var nodes = NodesFor(vessel, true);
			var curIdx = nodes.IndexOf(cur);
			return curIdx >= 0 ? nodes[(curIdx + 1) % nodes.Count] : cur;
		}

	}
}
