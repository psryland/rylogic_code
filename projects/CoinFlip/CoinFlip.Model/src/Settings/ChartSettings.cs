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
			NettWorthColour = new Colour32(0xffbbd2eb); // Blueish
			ShowOpenOrders = EShowItems.Disabled;
			ShowCompletedOrders = EShowItems.Disabled;
			ShowVolume = false;
			ShowMarketDepth = false;
			XAxisLabelMode = EXAxisLabelMode.LocalTime;
			ConfettiLabelSize = 10.0;
			ConfettiLabelTransparency = 1.0;
			ConfettiDescriptionsVisible = true;
			ConfettiLabelsToTheLeft = true;
			SelectionDistance = 5.0;
		}
		public ChartSettings(XElement node)
			: base(node)
		{ }

		/// <summary>The colour to draw 'Buy Price, Bid, Long, Highest one on a chart' things</summary>
		public Colour32 Q2BColour
		{
			get => get<Colour32>(nameof(Q2BColour));
			set => set(nameof(Q2BColour), value);
		}

		/// <summary>The colour to draw 'Sell Price, Ask, Short, Lowest one on a chart' things</summary>
		public Colour32 B2QColour
		{
			get => get<Colour32>(nameof(B2QColour));
			set => set(nameof(B2QColour), value);
		}

		/// <summary>The colour to draw the Nett Value region with</summary>
		public Colour32 NettWorthColour
		{
			get => get<Colour32>(nameof(NettWorthColour));
			set => set(nameof(NettWorthColour), value);
		}

		/// <summary>Show current trades</summary>
		public EShowItems ShowOpenOrders
		{
			get => get<EShowItems>(nameof(ShowOpenOrders));
			set => set(nameof(ShowOpenOrders), value);
		}

		/// <summary>Show current trades</summary>
		public EShowItems ShowCompletedOrders
		{
			get => get<EShowItems>(nameof(ShowCompletedOrders));
			set => set(nameof(ShowCompletedOrders), value);
		}

		/// <summary>Show the trade volume</summary>
		public bool ShowVolume
		{
			get => get<bool>(nameof(ShowVolume));
			set => set(nameof(ShowVolume), value);
		}

		/// <summary>Show current market depth</summary>
		public bool ShowMarketDepth
		{
			get => get<bool>(nameof(ShowMarketDepth));
			set => set(nameof(ShowMarketDepth), value);
		}

		/// <summary>The format of tick text on the chart X axis</summary>
		public EXAxisLabelMode XAxisLabelMode
		{
			get => get<EXAxisLabelMode>(nameof(XAxisLabelMode));
			set => set(nameof(XAxisLabelMode), value);
		}

		/// <summary>The font size for confetti labels</summary>
		public double ConfettiLabelSize
		{
			get => get<double>(nameof(ConfettiLabelSize));
			set => set(nameof(ConfettiLabelSize), value);
		}

		/// <summary>How see through the backgrounds of the trade labels are</summary>
		public double ConfettiLabelTransparency
		{
			get => get<double>(nameof(ConfettiLabelTransparency));
			set => set(nameof(ConfettiLabelTransparency), value);
		}

		/// <summary>Show the descriptions next to the confetti icons</summary>
		public bool ConfettiDescriptionsVisible
		{
			get => get<bool>(nameof(ConfettiDescriptionsVisible));
			set => set(nameof(ConfettiDescriptionsVisible), value);
		}

		/// <summary>Show the labels to the left of the trade markers</summary>
		public bool ConfettiLabelsToTheLeft
		{
			get => get<bool>(nameof(ConfettiLabelsToTheLeft));
			set => set(nameof(ConfettiLabelsToTheLeft), value);
		}

		/// <summary>The distance in pixels needed to selected something</summary>
		public double SelectionDistance
		{
			get { return get<double>(nameof(SelectionDistance)); }
			set { set(nameof(SelectionDistance), value); }
		}

	}
}
