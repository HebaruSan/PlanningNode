using System;
using UnityEngine;
using KSP.Localization;

namespace PlanningNode {

	/// <summary>
	/// One planning node the user has created, may or may not be active or visible
	/// </summary>
	public class PlanningNodeModel {

		/// <summary>
		/// Load a planned node from a config node from the save file
		/// </summary>
		/// <param name="cfg">ConfigNode to load from</param>
		public PlanningNodeModel(ConfigNode cfg)
		{
			string bodyName = "";
			if (cfg.TryGetValue("origin", ref bodyName)) {
				origin = FlightGlobals.GetBodyByName(bodyName);
			}
			cfg.TryGetValue("name",     ref name);
			cfg.TryGetValue("burnTime", ref burnTime);
			cfg.TryGetValue("deltaV",   ref deltaV);
			uint vesId = 0;
			if (cfg.TryGetValue("vessel", ref vesId)) {
				vesselId = vesId;
			}
		}

		/// <summary>
		/// Make a new node for a given SOI and owner
		/// </summary>
		/// <param name="origin">The body of the starting SOI</param>
		/// <param name="vessel">The vessel associated with this, if any</param>
		public PlanningNodeModel(CelestialBody origin, Vessel vessel = null)
		{
			this.origin = origin;
			this.vessel = vessel;
			burnTime    = Planetarium.GetUniversalTime() + origin.orbit.period / 8;
			deltaV      = new Vector3d(0, 0, 0);
		}

		/// <summary>
		/// The name used for our config nodes
		/// </summary>
		public const string NodeName = "PLANNINGNODE";

		/// <summary>
		/// Users are going to want to label their planning nodes
		/// </summary>
		public string name = Localizer.Format("PlanningNode_defaultNodeName");

		/// <summary>
		/// Planet where the burn occurs
		/// </summary>
		public CelestialBody origin;

		/// <summary>
		/// When the burn happens
		/// </summary>
		public double burnTime;

		/// <summary>
		/// Amount and direction of the burn
		/// </summary>
		public Vector3d deltaV;

		/// <summary>
		/// The vessel that owns this node, if any
		/// </summary>
		public Vessel vessel {
			get { return FlightGlobals.FindVessel(vesselId ?? 0, out Vessel v) ? v : null; }
			set { vesselId = value?.persistentId; }
		}

		private uint? vesselId;

		/// <returns>
		/// A ConfigNode representing this node
		/// </returns>
		public ConfigNode GetConfigNode()
		{
			var cfg = new ConfigNode(NodeName);
			cfg.AddValue("name",     name);
			cfg.AddValue("origin",   origin.bodyName);
			cfg.AddValue("burnTime", burnTime);
			cfg.AddValue("deltaV",   deltaV);
			if (vessel != null) {
				cfg.AddValue("vessel", vesselId);
			}
			return cfg;
		}

		/// <returns>
		/// Direction in which we need to burn within the body's SOI
		/// </returns>
		public Vector3d BurnDirection()
		{
			Vector3d planetPrograde = origin.orbit.getOrbitalVelocityAtUT(burnTime).xzy.normalized;
			Vector3d planetNormal   = origin.orbit.GetOrbitNormal().xzy.normalized;
			Vector3d planetRadial   = QuaternionD.AngleAxis(90, planetNormal) * planetPrograde;
			return (deltaV.z * planetPrograde
			      + deltaV.y * planetNormal
			      + deltaV.x * planetRadial).normalized;
		}

		/// <summary>
		/// Describe this node to the user
		/// </summary>
		/// <returns>
		/// name
		/// Excess V: (burn magnitude) m/s
		/// T - (time till burn)
		/// </returns>
		public string GetCaption(Vessel vessel)
		{
			var timeAndSpeed = escapeTimeAndSpeed(vessel);
			return timeAndSpeed == null
				? Localizer.Format("PlanningNode_markerCaption", name,
					deltaV.magnitude.ToString("N1"),
					TimeTill(Planetarium.GetUniversalTime()))
				: Localizer.Format("PlanningNode_markerCaption", name,
					(timeAndSpeed.Item2 - deltaV.magnitude).ToString("+0.#;-0.#;0.#"),
					TimeTill(timeAndSpeed.Item1));
		}

		private Tuple<double, double> escapeTimeAndSpeed(Vessel vessel)
		{
			var patch = escapePatch(vessel);
			if (patch != null) {
				return new Tuple<double, double>(patch.EndUT, patch.getOrbitalVelocityAtUT(patch.EndUT).magnitude);
			}
			return null;
		}

		private Orbit escapePatch(Vessel v)
		{
			var vesSolv = v?.patchedConicSolver;
			if (vesSolv != null) {
				for (int i = 0; i < vesSolv.flightPlan.Count; ++i) {
					var patch = vesSolv.flightPlan[i];
					if (patch.activePatch && patch.referenceBody == origin
						&& patch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
						return patch;
					}
				}
				for (int i = 0; i < vesSolv.patches.Count; ++i) {
					var patch = vesSolv.patches[i];
					if (patch.activePatch && patch.referenceBody == origin
						&& patch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
						return patch;
					}
				}
			}
			return null;
		}

		private string TimeTill(double fromUT)
		{
			return TimespanDescription(burnTime - fromUT);
		}

		private string TimespanDescription(double UT)
		{
			if (UT < 0) {
				// Draw negatives with parentheses to reduce confusion with "T - " format
				return Localizer.Format("PlanningNode_negativeLength", TimespanDescription(-UT));
			}
			var dttm = new DateTimeParts(UT);
			return dttm.needYears
				? Localizer.Format("PlanningNode_yearsLength", dttm.years, dttm.days, dttm.hours, dttm.minutes, dttm.seconds)
				: dttm.needDays
				? Localizer.Format("PlanningNode_daysLength", dttm.days, dttm.hours, dttm.minutes, dttm.seconds)
				: dttm.needHours
				? Localizer.Format("PlanningNode_hoursLength", dttm.hours, dttm.minutes, dttm.seconds)
				: dttm.needMinutes
				? Localizer.Format("PlanningNode_minutesLength", dttm.minutes, dttm.seconds)
				: Localizer.Format("PlanningNode_secondsLength", dttm.seconds);
		}

	}

}
