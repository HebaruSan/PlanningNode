using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNode {

	using MonoBehavior = MonoBehaviour;

	/// <summary>
	/// Manager for an active planning node
	/// </summary>
	public class PlanningNodeEditor : MonoBehavior {

		/// <summary>
		/// Initialize the behavior
		/// </summary>
		public PlanningNodeEditor() : base() { }

		/// <summary>
		/// The node we're editing, should be set between AddComponent and Start
		/// </summary>
		public PlanningNodeModel editingNode;

		/// <summary>
		/// True if we should open the maneuver node editing tools, false otherwise
		/// </summary>
		public bool canEdit;

		/// <summary>
		/// Event to notify calling code that we're no longer valid (user deleted the node)
		/// </summary>
		public event Action DeleteMe;

		/// <summary>
		/// Event to notify calling code that we should stop editing (user edited another node)
		/// </summary>
		public event Action CloseMe;

		private IEnumerator Start()
		{
			// Fingers crossed that nobody else does this, or if they do,
			// that we restore our old references in the right order
			originalCheckEncounter = PatchedConics.CheckEncounter;
			PatchedConics.CheckEncounter = MyCheckEncounter;

			// This represents our starting orbit
			driver                    = gameObject.AddComponent<OrbitDriver>();
			driver.lowerCamVsSmaRatio = 0.0001f;
			driver.upperCamVsSmaRatio = 999999;
			// Without this everything crashes and our app launcher deletes itself
			driver.SetOrbitMode(OrbitDriver.UpdateMode.UPDATE);

			// Just enough of a Vessel to not crash
			fakeVessel             = gameObject.AddComponent<Vessel>();
			fakeVessel.vesselName  = "DUMMY";
			fakeVessel.orbitDriver = driver;
			fakeVessel.parts       = new List<Part>();
			fakeVessel.protoVessel = new ProtoVessel(fakeVessel);
			// Try to avoid immediately crashing into the Sun in tracking station
			fakeVessel.altitude    = FlightGlobals.GetHomeBody().sphereOfInfluence;
			fakeVessel.GoOnRails();
			driver.vessel          = fakeVessel;
			// Rendezvous achievement expects enabled vessels to have a rootPart
			fakeVessel.enabled     = false;

			// This calculates the conic patches, including after maneuvers
			solver            = gameObject.AddComponent<PatchedConicSolver>();
			solver.obtDriver  = driver;
			solver.targetBody = FlightGlobals.ActiveVessel?.targetObject as CelestialBody;

			// This draws the solver's calculated info
			conicRenderer        = gameObject.AddComponent<PatchedConicRenderer>();
			conicRenderer.solver = solver;

			if (canEdit) {
				// This thing shows the close approach markers, only works in map view
				targeter = gameObject.AddComponent<OrbitTargeter>();
				targeter.SetTarget(solver.targetBody?.orbitDriver);
			}

			GameEvents.onManeuverNodeSelected.Add(OnManeuverNodeSelected);

			// HACK: Let the stock objects finish initializing before we finish our initialization
			yield return new WaitForEndOfFrame();
			if (conicRenderer != null) {
				while (conicRenderer.relativeTo == null) {
					if (!HighLogic.LoadedSceneIsFlight) {
						yield break;
					}
					yield return null;
				}
			}

			conicRenderer.solver = solver;

			// Just in case the vessel makes its own stuff
			fakeVessel.DetachPatchedConicsSolver();
			fakeVessel.patchedConicSolver   = solver;
			fakeVessel.patchedConicRenderer = conicRenderer;

			StartCoroutine(SwitchTo(editingNode));
		}

		/// <summary>
		/// Start editing a different node
		/// </summary>
		/// <param name="newNode">The node to edit</param>
		/// <returns>
		/// This is a coroutine because we want to give the stock objects one
		/// frame to update themselves before we create a new maneuver node
		/// </returns>
		public IEnumerator SwitchTo(PlanningNodeModel newNode)
		{
			DestroyNode();

			editingNode = newNode;

			driver.orbit.SetOrbit(
				editingNode.origin.orbit.inclination,
				editingNode.origin.orbit.eccentricity,
				editingNode.origin.orbit.semiMajorAxis,
				editingNode.origin.orbit.LAN,
				editingNode.origin.orbit.argumentOfPeriapsis,
				editingNode.origin.orbit.meanAnomalyAtEpoch,
				editingNode.origin.orbit.epoch,
				editingNode.origin.orbit.referenceBody
			);

			yield return new WaitForEndOfFrame();

			node = solver.AddManeuverNode(editingNode.burnTime);
			// Don't need to update plan for zero dV (new) node
			if (editingNode.deltaV != node.DeltaV) {
				node.DeltaV = editingNode.deltaV;
				solver.UpdateFlightPlan();
			}
			if (canEdit) {
				node.AttachGizmo(MapView.ManeuverNodePrefab, conicRenderer);
				node.attachedGizmo.OnGizmoUpdated += OnGizmoUpdated;
			}

			// Hide the first orbit because it's already drawn with the original planet
			// and if drawn again it will be gold and distracting because PatchedConicRenderer
			// assumes that any orbit with a vessel that isn't the active vessel is the target
			var pr = conicRenderer.patchRenders[0];
			pr.lineWidth = 0;
			pr.MakeVector();

			ZoomTo();
		}

		private void OnManeuverNodeSelected()
		{
			if (node?.attachedGizmo != null) {
				// It's ours, get notified
				node.attachedGizmo.OnGizmoUpdated += OnGizmoUpdated;
			} else {
				// Don't try to edit multiple nodes at once
				CloseMe?.Invoke();
			}
		}

		private void OnDisable()
		{
			// Put the encounter checker back to normal when we're done
			PatchedConics.CheckEncounter = originalCheckEncounter;
			originalCheckEncounter = null;

			GameEvents.onManeuverNodeSelected.Remove(OnManeuverNodeSelected);
			origCamTarget = null;
			DestroyNode();
			Destroy(fakeVessel);
			Destroy(targeter);
			Destroy(conicRenderer);
			Destroy(solver);
			Destroy(driver);
		}

		private void DestroyNode()
		{
			if (node != null) {
				if (node.attachedGizmo != null) {
					node.attachedGizmo.OnGizmoUpdated -= OnGizmoUpdated;
					node.DetachGizmo();
				}
				node.RemoveSelf();
				node = null;
			}
		}

		/// <summary>
		/// Focus the map view to make it easy to edit this node
		/// </summary>
		private void ZoomTo()
		{
			if (origCamTarget == null) {
				origCamTarget = MapView.MapCamera.target;
				origCamDist   = MapView.MapCamera.Distance;
			}
			var newFocus = PlanetariumCamera.fetch.targets.Find(mapObj =>
				mapObj.celestialBody != null && mapObj.celestialBody.Equals(driver.orbit.referenceBody));
			bool sameSOI = newFocus == MapView.MapCamera.target;
			if (!sameSOI) {
				// Focus new grandparent body
				MapView.MapCamera.SetTarget(newFocus);
			}
			// Zoom to size of parent body's orbit, only zooming in if changing SOI
			float dist = ZoomDistance();
			if (!sameSOI || dist > MapView.MapCamera.Distance) {
				MapView.MapCamera.SetDistance(dist);
			}
		}

		/// <summary>
		/// Reset the view to what it was before we started editing
		/// </summary>
		public void ZoomBack()
		{
			if (origCamTarget != null) {
				MapView.MapCamera.SetTarget(origCamTarget);
				MapView.MapCamera.SetDistance(origCamDist);
				if (editingNode.origin == (origCamTarget.celestialBody ?? origCamTarget.vessel?.mainBody)) {
					LookAtMarker();
				}
				origCamTarget = null;
			}
		}

		private void LookAtMarker()
		{
			var dir = (Vector3)editingNode.BurnDirection();
			PlanetariumCamera.fetch.camHdg = Mathf.Atan2(dir.x, dir.z)
				- Mathf.Deg2Rad * (float)Planetarium.fetch.inverseRotAngle;
			PlanetariumCamera.fetch.camPitch = -Mathf.Asin(dir.y) + 6f * Mathf.Deg2Rad;
		}

		private float ZoomDistance()
		{
			float semiMajorAxis = (float)driver.orbit.semiMajorAxis;
			CelestialBody targetBody = FlightGlobals.fetch.VesselTarget as CelestialBody;
			if (targetBody != null && targetBody.orbit.referenceBody == driver.orbit.referenceBody) {
				// Zoom to target body's orbit if larger
				semiMajorAxis = SaneMax(semiMajorAxis,
				                        (float)targetBody.orbit.semiMajorAxis);
			}
			if (solver?.flightPlan != null) {
				for (int i = 0; i < solver.flightPlan.Count; ++i) {
					// Zoom to planned orbit if larger
					semiMajorAxis = SaneMax(semiMajorAxis,
					                        (float)solver.flightPlan[i].semiMajorAxis);
				}
			}
			return semiMajorAxis / 3000f;
		}

		/// <summary>
		/// Returns the greater of two floats in a non-insane way.
		/// Math.Max(10, NaN) = NaN, which means you can't use it to
		/// find the largest value in a sequence that might contain NaN.
		/// </summary>
		/// <param name="a">One of the values</param>
		/// <param name="b">The other value</param>
		/// <returns>
		/// The greater of the two values.
		/// Only returns NaN if BOTH are NaN, unlike Math.Max.
		/// </returns>
		private float SaneMax(float a, float b)
		{
			return float.IsNaN(a) ? b : float.IsNaN(b) ? a : Mathf.Max(a, b);
		}

		/// <summary>
		/// Old school style override of PatchedConics.CheckEncounter, set up in Start and reset at destroy.
		/// We prevent any encounter with our starting body, and otherwise hand off to the default implementation.
		/// </summary>
		/// <param name="p">The patch currently being analyzed</param>
		/// <param name="nextPatch">The next patch to be analyzed</param>
		/// <param name="startEpoch">The time when the vessel reaches p</param>
		/// <param name="sec">The driver of the orbit to check for an encounter; this is the only parameter that we actually use here rather than passing along to the default implementation</param>
		/// <param name="targetBody">The user's currently selected target</param>
		/// <param name="pars">Stuff that controls how the solver works</param>
		/// <param name="logErrors">true to print things to the log, false otherwise</param>
		/// <returns>
		/// true if encounter found, false otherwise (or if one would have been found for our starting body)
		/// </returns>
		private bool MyCheckEncounter(
			Orbit p, Orbit nextPatch, double startEpoch, OrbitDriver sec,
			CelestialBody targetBody, PatchedConics.SolverParameters pars, bool logErrors = true)
		{
			// Suppress encounters with our starting body, because it makes the solver put NaN orbits in the flight plan
			return sec?.celestialBody == editingNode?.origin
				? false
				: originalCheckEncounter(p, nextPatch, startEpoch, sec, targetBody, pars, logErrors);
		}

		private void Update()
		{
			if (node != null && solver != null && !solver.maneuverNodes.Contains(node)) {
				// User deleted the node! Remove it from everything.
				PlanningNodesManager.Instance.nodes.Remove(editingNode);
				editingNode = null;
				node = null;
				// Tell listeners to return to non-editing state
				DeleteMe?.Invoke();
			}
		}

		private void FixedUpdate()
		{
			if (solver != null && solver.targetBody != FlightGlobals.ActiveVessel?.targetObject as CelestialBody) {
				solver.targetBody = FlightGlobals.ActiveVessel?.targetObject as CelestialBody;
				editingNode.color = PlanningNodeModel.GetBodyColor(solver.targetBody);
				if (canEdit) {
					targeter.SetTarget(solver.targetBody?.orbitDriver);
				} else {
					// targeter will call this for us if needed
					solver.UpdateFlightPlan();
				}
			}
		}

		private void OnGizmoUpdated(Vector3d dV, double ut)
		{
			if (editingNode != null) {
				editingNode.burnTime = ut;
				editingNode.deltaV   = dV;
			}
		}

		private PatchedConics.CheckEncounterDelegate originalCheckEncounter;

		private MapObject origCamTarget;
		private float     origCamDist;

		private OrbitDriver          driver;
		private Vessel               fakeVessel;
		private PatchedConicSolver   solver;
		private PatchedConicRenderer conicRenderer;
		private OrbitTargeter        targeter;
		private ManeuverNode         node;
	}
}
