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
			line1 = mkLine(gameObj1);
			line2 = mkLine(gameObj2);
			line3 = mkLine(gameObj3);
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

				float camDist   = cameraDist(where);
				// Thin lines at a distance, slightly thicker up close, thicker for bigger SOIs
				float thickness = (camDist + ringScale) / 24000f;

				UpdateRing(line1, where - 2 * ringScale * direction, 3 * ringScale, direction, camDist, thickness, myNode.color);
				UpdateRing(line2, where -     ringScale * direction, 2 * ringScale, direction, camDist, thickness, myNode.color);
				UpdateRing(line3, where,                                 ringScale, direction, camDist, thickness, myNode.color);
			}
		}

		private void OnGUI()
		{
			if (myNode != null && textWhere != null) {
				var camDist = cameraDist(textWhere);
				if (0 < camDist && camDist < ringScale) {
					var screenWhere = PlanetariumCamera.Camera.WorldToScreenPoint(
							ScaledSpace.LocalToScaledSpace(textWhere))
						+ (0.6f * ringScale / camDist + 10) * Vector3.down;

					if (screenWhere.z > 0 && viewport.Contains(screenWhere)) {
						// In front of camera, so draw button
						labelStyle.normal.textColor = myNode.color;
						if (GUI.Button(
							new Rect(
								screenWhere.x - halfTextWidth,
								Screen.height - screenWhere.y,
								textWidth, 30),
							myNode.GetCaption(vessel),
							labelStyle
						)) {
							EditMe?.Invoke(myNode);
						}
					} else {
						// Off edge of screen or behind camera, draw marker at edge of screen
						labelStyle.normal.textColor = new Color(
							myNode.color.r, myNode.color.g, myNode.color.b, 0.6f);
						var edgePos = edgePosition((screenWhere.z < 0 ? -1f : 1f)
							* ((Vector2)screenWhere - screenCenterOffset));
						GUI.Label(
							new Rect(edgePos.x - halfTextWidth, edgePos.y, textWidth, 30),
							myNode.GetCaption(vessel),
							labelStyle
						);
					}
				}
			}
		}

		private Vector2 edgePosition(Vector2 fromCenter)
		{
			if (fromCenter.x == 0) {
				// Infinite slope, handle specially
				return new Vector2(screenCenterOffset.x,
					fromCenter.y > 0 ? viewport.yMin : viewport.yMax);
			} else {
				// Safe to divide by x
				float slope = fromCenter.y / fromCenter.x;
				if (slope > viewportSlope || slope < -viewportSlope) {
					// Top / bottom
					return new Vector2(
						Mathf.Clamp(screenCenterOffset.x + fromCenter.y / slope, 
							viewport.xMin, viewport.xMax),
						fromCenter.y > 0 ? viewport.yMin : viewport.yMax
					);
				} else {
					// Left / right
					return new Vector2(
						fromCenter.x < 0 ? viewport.xMin : viewport.xMax,
						Mathf.Clamp(screenCenterOffset.y - slope * fromCenter.x,
							viewport.yMin, viewport.yMax)
					);
				}
			}
		}

		private LineRenderer mkLine(GameObject gameObj)
		{
			// Trying to learn from TWP
			gameObj.layer = 9;
			var line = gameObj.AddComponent<LineRenderer>();
			line.transform.parent = null;
			line.useWorldSpace = true;
			line.loop = true;
			if (markerMaterial == null) {
				// An orbit without the fade
				markerMaterial = new Material(MapView.fetch.orbitLinesMaterial);
				markerMaterial.SetFloat("_FadeStrength", 0f);
			}
			line.material = markerMaterial;
			return line;
		}

		private static Vector3d atSOIlimit(CelestialBody body, Vector3d direction)
		{
			return body.sphereOfInfluence * direction.normalized;
		}

		private void UpdateRing(LineRenderer line, Vector3d center, double radius, Vector3d direction, float camDist, float thickness, Color color)
		{
			if (camDist > 0) {
				line.positionCount = Math.Min(96, Math.Max(16, 12 + 50000 / ((int)camDist + 1))) / 4 * 4;
				var poses = new Vector3[line.positionCount];
				int quadLen = line.positionCount / 4;
				Vector3d dX = radius * Vector3d.Cross(
					direction,
					(direction.x != 0 || direction.z != 0) ? Vector3d.up : Vector3d.forward
				).normalized;
				Vector3d dY = radius * Vector3d.Cross(direction, dX).normalized;
				for (int i = 0; i < quadLen; ++i) {
					// Make a circle 4 points at a time to reduce trig calls
					float theta = 2f * Mathf.PI * i / line.positionCount;
					float cosTheta = Mathf.Cos(theta);
					float sinTheta = Mathf.Sin(theta);
					poses[i              ] = ScaledSpace.LocalToScaledSpace(center + cosTheta * dX + sinTheta * dY);
					poses[i +     quadLen] = ScaledSpace.LocalToScaledSpace(center - sinTheta * dX + cosTheta * dY);
					poses[i + 2 * quadLen] = ScaledSpace.LocalToScaledSpace(center - cosTheta * dX - sinTheta * dY);
					poses[i + 3 * quadLen] = ScaledSpace.LocalToScaledSpace(center + sinTheta * dX - cosTheta * dY);
				}
				line.SetPositions(poses);
				line.startColor = line.endColor = color;
				line.startWidth = line.endWidth = thickness;
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

		// Markers must be within this area to be interactable,
		// otherwise they'll be clipped to its edges
		private static readonly Rect viewport = new Rect(
			50, 50, Screen.width - 100, Screen.height - 100);

		private static readonly float viewportSlope = viewport.height / viewport.width;

		private static readonly Vector2 screenCenterOffset = 0.5f * new Vector2(
			Screen.width, Screen.height);

		// Scale the markers to the size of their containing SOI
		private float ringScale => 0.006f * (float)(myNode?.origin?.sphereOfInfluence ?? 84000000);

		private readonly float screenRadius = 0.5f * Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);

		private const float textWidth     = 100f;
		private const float halfTextWidth = 0.5f * textWidth;

		private Vessel   vessel;
		private Vector3d where;
		private Vector3d textWhere;

		private readonly GUIStyle labelStyle = new GUIStyle() {
			alignment = TextAnchor.MiddleCenter,
			richText  = true,
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
