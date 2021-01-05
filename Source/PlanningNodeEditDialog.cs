using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.Localization;
using TMPro;

namespace PlanningNode {

	/// <summary>
	/// Window for editing properties of a planning node outside of the maneuver gizmo
	/// </summary>
	public class PlanningNodeEditDialog : DialogGUIVerticalLayout {

		/// <summary>
		/// Initialize the dialog
		/// </summary>
		/// <param name="nodeToEdit">The node we'll be editing</param>
		/// <param name="canEdit">True if we can open the maneuver node editing tools, false otherwise</param>
		public PlanningNodeEditDialog(PlanningNodeModel nodeToEdit, bool canEdit)
			: base(-1, -1, pad, new RectOffset(4, 4, 4, 4), TextAnchor.UpperLeft)
		{
			editingNode = nodeToEdit;

			var toprow = new List<DialogGUIBase>() {
				TooltipExtensions.DeferTooltip(new DialogGUIButton(
					"PlanningNode_DeleteButtonCaption",
					() => DeleteNode?.Invoke(),
					buttonWidth, buttonHeight,
					false
				) {
					tooltipText = "PlanningNode_DeleteButtonTooltip"
				}),
				new DialogGUIFlexibleSpace(),
				TooltipExtensions.DeferTooltip(new DialogGUIButton(
					"PlanningNode_CloseButtonCaption",
					() => CloseDialog?.Invoke(),
					buttonWidth, buttonHeight,
					false
				) {
					tooltipText = "PlanningNode_CloseButtonTooltip"
				})
			};
			if (canEdit) {
				toprow.Insert(0, TooltipExtensions.DeferTooltip(new DialogGUIButton(
					"PlanningNode_NewButtonCaption",
					() => NewNode?.Invoke(),
					buttonWidth, buttonHeight,
					false
				) {
					tooltipText = "PlanningNode_NewButtonTooltip"
				}));
			}
			AddChild(new DialogGUIHorizontalLayout(
				-1, -1, 8, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft,
				toprow.ToArray()
			));
			AddChild(new DialogGUIHorizontalLayout(
				-1, -1, pad, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft,
				new DialogGUILabel("PlanningNode_NameLabelCaption", buttonWidth / 2),
				new DialogGUITextInput(
					editingNode.name,
					false,
					24,
					s  => { return editingNode.name = s; },
					() => { return editingNode.name;     },
					TMP_InputField.ContentType.Standard,
					buttonHeight
				),
				TooltipExtensions.DeferTooltip(new DialogGUIButton(
					"PlanningNode_PrevNodeCaption",
					() => PrevNode?.Invoke(),
					smallBtnWidth, buttonHeight,
					false
				) {
					tooltipText = canEdit ? "PlanningNode_PrevNodeTooltip" : "PlanningNode_PrevNodeViewOnlyTooltip"
				}),
				TooltipExtensions.DeferTooltip(new DialogGUIButton(
					"PlanningNode_NextNodeCaption",
					() => NextNode?.Invoke(),
					smallBtnWidth, buttonHeight,
					false
				) {
					tooltipText = canEdit ? "PlanningNode_NextNodeTooltip" : "PlanningNode_NextNodeViewOnlyTooltip"
				})
			));
			if (canEdit) {
				AddChild(new DialogGUIHorizontalLayout(
					-1, -1, pad, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft,
					new DialogGUILabel("PlanningNode_BodyLabelCaption", buttonWidth / 2),
					new DialogGUILabel(
						() => editingNode.origin.bodyName,
						-1
					),
					TooltipExtensions.DeferTooltip(new DialogGUIButton(
						"PlanningNode_PrevBodyCaption",
						() => { editingNode.origin = prevBody(editingNode.origin); },
						smallBtnWidth, buttonHeight,
						false
					) {
						tooltipText = "PlanningNode_PrevBodyTooltip"
					}),
					TooltipExtensions.DeferTooltip(new DialogGUIButton(
						"PlanningNode_NextBodyCaption",
						() => { editingNode.origin = nextBody(editingNode.origin); },
						smallBtnWidth, buttonHeight,
						false
					) {
						tooltipText = "PlanningNode_NextBodyTooltip"
					})
				));
			}
			if (canEdit) {
				AddChild(TooltipExtensions.DeferTooltip(new DialogGUIToggle(
					() => editingNode.vessel == null,
					"PlanningNode_ShowForAllCheckboxCaption",
					b => { editingNode.vessel = b ? null : FlightGlobals.ActiveVessel; }
				) {
					tooltipText = "PlanningNode_ShowForAllCheckboxTooltip"
				}));
			}

			// Don't try to plot a maneuver from the Sun
			for (int i = 0; i < FlightGlobals.Bodies.Count; ++i) {
				var b = FlightGlobals.Bodies[i];
				if (b.referenceBody != null) {
					allowedBodies.Add(b);
				}
			}
		}

		/// <summary>
		/// The node we're editing
		/// </summary>
		public PlanningNodeModel editingNode;

		/// <summary>
		/// Function to call when the user clicks the button to create a new node
		/// </summary>
		public event Action NewNode;

		/// <summary>
		/// Function to call when the user clicks the button to delete the current node
		/// </summary>
		public event Action DeleteNode;

		/// <summary>
		/// Function to call when the user clicks the button to close the dialog
		/// </summary>
		public event Action CloseDialog;

		/// <summary>
		/// Function to call when the user clicks the button to edit the previous node
		/// </summary>
		public event Action PrevNode;

		/// <summary>
		/// Function to call when the user clicks the button to edit the next node node
		/// </summary>
		public event Action NextNode;

		/// <summary>
		/// Create a dialog and display it
		/// </summary>
		/// <param name="where">Vector describing location on screen for the popup</param>
		/// <returns>
		/// Reference to the dialog
		/// </returns>
		public PopupDialog Show(Vector3 where)
		{
			var anchor = HighLogic.LoadedScene == GameScenes.TRACKSTATION ? Vector2.right : Vector2.one;
			dialog = PopupDialog.SpawnPopupDialog(
				anchor,
				anchor,
				new MultiOptionDialog(
					PlanningNodeAddonBase.Name,
					"",
					Localizer.Format("PlanningNode_editorTitle", modVersion.Major, modVersion.Minor, modVersion.Build),
					skin,
					new Rect(
						where.x / Screen.width  + 0.5f,
						where.y / Screen.height + 0.5f,
						dialogWidth, dialogHeight
					),
					this
				),
				false,
				skin,
				false
			);
			InputLockManager.SetControlLock(MyLocks, "PlanningNodeEditDialogName");
			return dialog;
		}

		/// <summary>
		/// Close the dialog
		/// </summary>
		public void Dismiss()
		{
			if (dialog != null) {
				InputLockManager.RemoveControlLock("PlanningNodeEditDialogName");
				dialog.Dismiss();
				dialog = null;
			}
		}

		private CelestialBody prevBody(CelestialBody b)
		{
			var idx = allowedBodies.IndexOf(b);
			return idx >= 0
				? allowedBodies[(idx + allowedBodies.Count - 1) % allowedBodies.Count]
				: b;
		}

		private CelestialBody nextBody(CelestialBody b)
		{
			var idx = allowedBodies.IndexOf(b);
			return idx >= 0
				? allowedBodies[(idx + 1) % allowedBodies.Count]
				: b;
		}

		/// <summary>
		/// Borrowed from Kerbal Engineer Redux and Americanized.
		/// </summary>
		/// <param name="c">The color to use</param>
		/// <returns>
		/// A 1x1 texture
		/// </returns>
		private static Texture2D SolidColorTexture(Color c)
		{
			Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			tex.SetPixel(1, 1, c);
			tex.Apply();
			return tex;
		}

		private static Sprite SpriteFromTexture(Texture2D tex)
		{
			return tex == null ? null : Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				tex.width
			);
		}

		private static readonly UIStyleState winState = new UIStyleState() {
			background = SpriteFromTexture(SolidColorTexture(new Color(0.15f, 0.15f, 0.15f, 0.8f))),
			textColor  = Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		private static readonly UISkinDef skin = new UISkinDef() {
			name   = "PlanningNode Skin",
			box    = UISkinManager.defaultSkin.box,
			font   = UISkinManager.defaultSkin.font,
			label  = UISkinManager.defaultSkin.label,
			toggle = UISkinManager.defaultSkin.toggle,
			button = UISkinManager.defaultSkin.button,
			window = new UIStyle() {
				normal    = winState,
				active    = winState,
				disabled  = winState,
				highlight = winState,
				alignment = TextAnchor.UpperCenter,
				fontSize  = UISkinManager.defaultSkin.window.fontSize,
				fontStyle = FontStyle.Bold,
			},
		};

		private const int pad           =   8;
		private const int buttonWidth   =  80;
		private const int buttonHeight  =  24;
		private const int smallBtnWidth =  24;
		private const int dialogWidth   = 300;
		private const int dialogHeight  =  -1;

		private const ControlTypes MyLocks = ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP
			| ControlTypes.MISC | ControlTypes.EVA_INPUT | ControlTypes.MAP
			| ControlTypes.STAGING | ControlTypes.THROTTLE
			| ControlTypes.ACTIONS_ALL | ControlTypes.GROUPS_ALL | ControlTypes.CUSTOM_ACTION_GROUPS;

		private readonly List<CelestialBody> allowedBodies = new List<CelestialBody>();

		private static Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;

		private PopupDialog dialog;
	}

}
