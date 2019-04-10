using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip.Settings
{
	public class ChartSettings :SettingsXml<ChartSettings>
	{
		public ChartSettings()
		{
			AskColour = new Colour32(0xff22b14c); // Green
			BidColour = new Colour32(0xffed1c24); // Red
			ShowOpenOrders = EShowItems.Disabled;
			ShowCompletedOrders = EShowItems.Disabled;
			ShowMarketDepth = false;
			XAxisLabelMode = EXAxisLabelMode.LocalTime;
		}
		public ChartSettings(XElement node)
			: base(node)
		{}

		/// <summary>The colour to draw 'Bullish/Ask/Buy' things</summary>
		public Colour32 AskColour
		{
			get { return get<Colour32>(nameof(AskColour)); }
			set { set(nameof(AskColour), value); }
		}

		/// <summary>The colour to draw 'Bearish/Bid/Sell' things</summary>
		public Colour32 BidColour
		{
			get { return get<Colour32>(nameof(BidColour)); }
			set { set(nameof(BidColour), value); }
		}

		/// <summary>Show current trades</summary>
		public EShowItems ShowOpenOrders
		{
			get { return get<EShowItems>(nameof(ShowOpenOrders)); }
			set { set(nameof(ShowOpenOrders), value); }
		}

		/// <summary>Show current trades</summary>
		public EShowItems ShowCompletedOrders
		{
			get { return get<EShowItems>(nameof(ShowCompletedOrders)); }
			set { set(nameof(ShowCompletedOrders), value); }
		}

		/// <summary>Show current market depth</summary>
		public bool ShowMarketDepth
		{
			get { return get<bool>(nameof(ShowMarketDepth)); }
			set { set(nameof(ShowMarketDepth), value); }
		}

		/// <summary>The format of tick text on the chart X axis</summary>
		public EXAxisLabelMode XAxisLabelMode
		{
			get { return get<EXAxisLabelMode>(nameof(XAxisLabelMode)); }
			set { set(nameof(XAxisLabelMode), value); }
		}
	}
}
