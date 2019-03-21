using Rylogic.Gfx;

namespace CoinFlip.UI.GfxObjects
{
	public class UpdatingText : View3d.Object
	{
		public UpdatingText()
			:base("*Text { *Font{*Colour{FF000000}} \"...updating...\" *ScreenSpace *Anchor {+1 +1} *o2w{*pos{+1, +1, 0}} *NoZTest }", false, CandleChart.CtxId, null)
		{}
	}
}
