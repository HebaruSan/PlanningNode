namespace PlanningNode {

	/// <summary>
	/// A substitute for the missing UISkinDef.Clone() and UISkinDef.ctor(UISkinDef).
	/// </summary>
	public class CloneableUISkinDef : UISkinDef {

		/// <summary>
		/// Make a new UISkinDef based on a previous instance.
		/// Can't make an extension constructor, but it's not sealed.
		/// </summary>
		/// <param name="prevInst">A skin we already have that we want to tweak</param>
		public CloneableUISkinDef(UISkinDef prevInst) : base()
		{
			// Set ALL properties to what they were in the previous instance
			// Calling code can then override in an object initializer
			box                            = prevInst.box;
			button                         = prevInst.button;
			customStyles                   = prevInst.customStyles;
			font                           = prevInst.font;
			horizontalScrollbar            = prevInst.horizontalScrollbar;
			horizontalScrollbarLeftButton  = prevInst.horizontalScrollbarLeftButton;
			horizontalScrollbarRightButton = prevInst.horizontalScrollbarRightButton;
			horizontalScrollbarThumb       = prevInst.horizontalScrollbarThumb;
			horizontalSlider               = prevInst.horizontalSlider;
			horizontalSliderThumb          = prevInst.horizontalSliderThumb;
			label                          = prevInst.label;
			scrollView                     = prevInst.scrollView;
			textArea                       = prevInst.textArea;
			textField                      = prevInst.textField;
			toggle                         = prevInst.toggle;
			verticalScrollbar              = prevInst.verticalScrollbar;
			verticalScrollbarDownButton    = prevInst.verticalScrollbarDownButton;
			verticalScrollbarUpButton      = prevInst.verticalScrollbarUpButton;
			verticalScrollbarThumb         = prevInst.verticalScrollbarThumb;
			verticalSlider                 = prevInst.verticalSlider;
			verticalSliderThumb            = prevInst.verticalSliderThumb;
			window                         = prevInst.window;
		}
	}

}
