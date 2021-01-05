using System;
using UnityEngine;

namespace PlanningNode {

	using MonoBehavior = MonoBehaviour;

	/// <summary>
	/// A behavior for the visual indicator of where to exit your SOI
	/// </summary>
	public class PlanningNodeMarker : MonoBehavior {

		/// <summary>
		/// Initialize a marker
		/// </summary>
		public PlanningNodeMarker() : base() { }

		private void Start()
		{
			line1 = mkLine(gameObj1, labelStyle.normal.textColor);
			line2 = mkLine(gameObj2, labelStyle.normal.textColor);
			line3 = mkLine(gameObj3, labelStyle.normal.textColor);
		}

		private void OnDisable()
		{
			Destroy(line1);
			Destroy(line2);
			Destroy(line3);
		}

		/// <summary>
		/// The node we're representing
		/// </summary>
		public PlanningNodeModel myNode;

		/// <summary>
		/// Fired when the user clicks the marker
		/// </summary>
		public event Action<PlanningNodeModel> EditMe;

		/// <summary>
		/// Reposiiton this marker based on the latest user actions
		/// </summary>
		/// <param name="parent">Celestial body that is this node's parent</param>
		/// <param name="vessel">The currently active vessel</param>
		/// <param name="direction">Direction in which the burn must happen</param>
		public void MoveTo(CelestialBody parent, Vessel vessel, Vector3 direction)
		{
			this.vessel = vessel;
			if ((HighLogic.LoadedScene == GameScenes.TRACKSTATION
				|| (HighLogic.LoadedScene == GameScenes.FLIGHT && MapView.MapIsEnabled))
				&& direction != null & line1 != null && line2 != null && line3 != null) {

				where     = parent.position + atSOIlimit(parent, direction);
				textWhere = where - ringScale * direction;

				UpdateRing(line1, where - 2 * ringScale * direction, 3 * ringScale, direction);
				UpdateRing(line2, where -     ringScale * direction, 2 * ringScale, direction);
				UpdateRing(line3, where,                                 ringScale, direction);
			}
		}

		private void OnGUI()
		{
			if (myNode != null && textWhere != null) {
				var screenWhere = PlanetariumCamera.Camera.WorldToScreenPoint(
					ScaledSpace.LocalToScaledSpace(textWhere));

				if (screenWhere.z > 0) {
					// In front of camera, so draw

					var camDist = cameraDist(textWhere);
					if (0 < camDist && camDist < ringScale) {
						var offset = 0.6f * ringScale / camDist + 10;

						if (GUI.Button(
							new Rect(screenWhere.x - 50, Screen.height - screenWhere.y + offset, 100, 30),
							myNode.GetCaption(vessel),
							labelStyle
						)) {
							EditMe?.Invoke(myNode);
						}
					}
				}
			}
		}

		private LineRenderer mkLine(GameObject gameObj, Color color)
		{
			// Trying to learn from TWP
			gameObj.layer = 9;
			var line = gameObj.AddComponent<LineRenderer>();
			line.transform.parent = null;
			line.useWorldSpace = true;
			if (markerMaterial == null) {
				// An orbit without the fade
				markerMaterial = new Material(MapView.fetch.orbitLinesMaterial);
				markerMaterial.SetFloat("_FadeStrength", 0f);
			}
			line.material = markerMaterial;
			line.startColor = line.endColor = color;
			return line;
		}

		private static Vector3d atSOIlimit(CelestialBody body, Vector3d direction)
		{
			return body.sphereOfInfluence * direction.normalized;
		}

		private void UpdateRing(LineRenderer line, Vector3d center, double radius, Vector3d direction)
		{
			var camDist = cameraDist(center);
			if (camDist > 0) {
				line.positionCount = Math.Min(96, Math.Max(16, 12 + 50000 / ((int)camDist + 1)));
				Vector3d dX = radius * Vector3d.Cross(
					direction,
					(direction.x != 0 || direction.z != 0) ? Vector3d.up : Vector3d.forward
				).normalized;
				Vector3d dY = radius * Vector3d.Cross(direction, dX).normalized;
				var poses = new Vector3[line.positionCount];
				for (int i = 0; i < line.positionCount; ++i) {
					float theta = 2f * Mathf.PI * i / line.positionCount;
					poses[i] = ScaledSpace.LocalToScaledSpace(
						center + Mathf.Cos(theta) * dX + Mathf.Sin(theta) * dY
					);
				}
				line.SetPositions(poses);
				// Thin lines at a distance, slightly thicker up close
				line.startWidth = line.endWidth = 0.05f * (float)Math.Sqrt(2 * camDist);
				line.loop       = true;
				line.enabled    = true;
			} else {
				// Can't see, don't draw old fragments
				line.enabled = false;
			}
		}

		private float cameraDist(Vector3d pos)
		{
			return (float)Vector3d.Dot(
				ScaledSpace.LocalToScaledSpace(pos) - PlanetariumCamera.Camera.transform.position,
				PlanetariumCamera.Camera.transform.forward
			);
		}

		private const float ringScale = 500000;

		private Vessel   vessel;
		private Vector3d where;
		private Vector3d textWhere;

		private readonly GUIStyle labelStyle = new GUIStyle() {
			alignment = TextAnchor.MiddleCenter,
			richText  = true,
			normal    = new GUIStyleState() {
				textColor = UnityEngine.Random.ColorHSV(0, 1, 0.5f, 0.5f, 0.75f, 0.75f),
			},
		};

		private static Material markerMaterial;

		private GameObject   gameObj1 = new GameObject($"{nameof(PlanningNodeMarker)}GameObject1");
		private GameObject   gameObj2 = new GameObject($"{nameof(PlanningNodeMarker)}GameObject2");
		private GameObject   gameObj3 = new GameObject($"{nameof(PlanningNodeMarker)}GameObject3");
		private LineRenderer line1;
		private LineRenderer line2;
		private LineRenderer line3;
	}
}
