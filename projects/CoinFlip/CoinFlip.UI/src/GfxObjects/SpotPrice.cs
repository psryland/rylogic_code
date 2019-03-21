using Rylogic.Gfx;

namespace CoinFlip.UI.GfxObjects
{
	public class SpotPrice : View3d.Object
	{
		public SpotPrice(Colour32 colour)
			:base($"*Line Ask {colour.ARGB:X8} {{ 0 0 0 1 0 0 }}", false, CandleChart.CtxId, null)
		{ }
	}
}
