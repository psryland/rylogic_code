using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip.Settings
{
	public class ChartSettings :SettingsXml<ChartSettings>
	{
		public ChartSettings()
		{
			Q2BColour = new Colour32(0xff22b14c); // Green
			B2QColour = new Colour32(0xffed1c24); // Red
			ShowOpenOrders = EShowItems.Disabled;
			ShowCompletedOrders = EShowItems.Disabled;
			ShowMarketDepth = false;
			XAxisLabelMode = EXAxisLabelMode.LocalTime;
			TradeLabelSize = 10.0;
			TradeLabelTransparency = 1.0;
			ShowTradeDescriptions = true;
		}
		public ChartSettings(XElement node)
			: base(node)
		{}

		/// <summary>The colour to draw 'Buy Price, Bid, Long, Highest one on a chart' things</summary>
		public Colour32 Q2BColour
		{
			get { return get<Colour32>(nameof(Q2BColour)); }
			set { set(nameof(Q2BColour), value); }
		}

		/// <summary>The colour to draw 'Sell Price, Ask, Short, Lowest one on a chart' things</summary>
		public Colour32 B2QColour
		{
			get { return get<Colour32>(nameof(B2QColour)); }
			set { set(nameof(B2QColour), value); }
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

		/// <summary>The font size for trade labels</summary>
		public double TradeLabelSize
		{
			get { return get<double>(nameof(TradeLabelSize)); }
			set { set(nameof(TradeLabelSize), value); }
		}

		/// <summary>How see through the backgrounds of the trade labels are</summary>
		public double TradeLabelTransparency
		{
			get { return get<double>(nameof(TradeLabelTransparency)); }
			set { set(nameof(TradeLabelTransparency), value); }
		}

		/// <summary>Show the descriptions next to the trade markers</summary>
		public bool ShowTradeDescriptions
		{
			get { return get<bool>(nameof(ShowTradeDescriptions)); }
			set { set(nameof(ShowTradeDescriptions), value); }
		}
	}
}
