using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;

namespace Tradee
{
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			General = new GeneralSettings();
			UI      = new UISettings();
			Chart   = new ChartSettings();
		}
		public Settings(string filepath)
			:base(filepath)
		{ }

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>General application settings</summary>
		public GeneralSettings General
		{
			get { return get(x => x.General); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(Settings.General)));
				set(x => x.General, value);
			}
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(UI)));
				set(x => x.UI, value);
			}
		}

		/// <summary>Default settings for charts</summary>
		public ChartSettings Chart
		{
			get { return get(x => x.Chart); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(Chart)));
				set(x => x.Chart, value);
			}
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(GeneralSettings.TyConv))]
	public class GeneralSettings :SettingsSet<GeneralSettings>
	{
		public GeneralSettings()
		{
			DefaultTimeFrame  = ETimeFrame.Hour12;
			AcctDataCacheDir  = Util.ResolveAppPath(".\\DataCacheAcct");
			PriceDataCacheDir = Util.ResolveAppPath(".\\DataCachePrice");
			HistoryLength     = 100000;
		}

		/// <summary>The time frame to open new charts at</summary>
		public ETimeFrame DefaultTimeFrame
		{
			get { return get(x => x.DefaultTimeFrame); }
			set { set(x => x.DefaultTimeFrame, value); }
		}

		/// <summary>The directory for account database files</summary>
		public string AcctDataCacheDir
		{
			get { return get(x => x.AcctDataCacheDir); }
			set { set(x => x.AcctDataCacheDir, value); }
		}

		/// <summary>The directory for price database files</summary>
		public string PriceDataCacheDir
		{
			get { return get(x => x.PriceDataCacheDir); }
			set { set(x => x.PriceDataCacheDir, value); }
		}

		/// <summary>The number of historic candles to request from the trade data source</summary>
		public int HistoryLength
		{
			get { return get(x => x.HistoryLength); }
			set { set(x => x.HistoryLength, value); }
		}

		private class TyConv :GenericTypeConverter<GeneralSettings> {}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(UISettings.TyConv))]
	public class UISettings :SettingsSet<UISettings>
	{
		public UISettings()
		{
			UILayout             = null;
			BullishColour        = Color.FromArgb(0xff, 0x00, 0x84, 0x3b);
			BearishColour        = Color.FromArgb(0xff, 0xf1, 0x59, 0x23);
			VisualisingColour    = Color.FromArgb(0xff, 0xb7, 0xea, 0xff);
			PendingOrderColour   = Color.FromArgb(0xff, 0x9e, 0xc0, 0xff);
			ActivePositionColour = Color.FromArgb(0xff, 0xce, 0xb2, 0xff);
			ClosedPositionColour = Color.FromArgb(0xff, 0xff, 0xe5, 0xdb);
			AggregateTrades      = true;
			WindowPosition       = Rectangle.Empty;
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		/// <summary>The colour to draw 'Bullish/Ask/Buy' things</summary>
		public Color BullishColour
		{
			get { return get(x => x.BullishColour); }
			set { set(x => x.BullishColour, value); }
		}
		public Color AskColour
		{
			get { return BearishColour; }
		}

		/// <summary>The colour to draw 'Bearist/Bid/Sell' things</summary>
		public Color BearishColour
		{
			get { return get(x => x.BearishColour); }
			set { set(x => x.BearishColour, value); }
		}
		public Color BidColour
		{
			get { return BullishColour; }
		}

		/// <summary>The colour of trades/orders in the 'Visualising' state</summary>
		public Color VisualisingColour
		{
			get { return get(x => x.VisualisingColour); }
			set { set(x => x.VisualisingColour, value); }
		}

		/// <summary>The colour of trades/orders in the 'PendingOrder' state</summary>
		public Color PendingOrderColour
		{
			get { return get(x => x.PendingOrderColour); }
			set { set(x => x.PendingOrderColour, value); }
		}

		/// <summary>The colour of trades/orders in the 'ActivePosition' state</summary>
		public Color ActivePositionColour
		{
			get { return get(x => x.ActivePositionColour); }
			set { set(x => x.ActivePositionColour, value); }
		}

		/// <summary>The colour of trades/orders in the 'Closed' state</summary>
		public Color ClosedPositionColour
		{
			get { return get(x => x.ClosedPositionColour); }
			set { set(x => x.ClosedPositionColour, value); }
		}

		/// <summary>Combine trades of the same instrument into one</summary>
		public bool AggregateTrades
		{
			get { return get(x => x.AggregateTrades); }
			set { set(x => x.AggregateTrades, value); }
		}

		/// <summary>The last position on screen</summary>
		public Rectangle WindowPosition
		{
			get { return get(x => x.WindowPosition); }
			set { set(x => x.WindowPosition, value); }
		}

		private class TyConv :GenericTypeConverter<UISettings> {}
	}

	/// <summary>Settings for a single chart</summary>
	[TypeConverter(typeof(ChartSettings.TyConv))]
	public class ChartSettings :SettingsSet<ChartSettings>
	{
		// Note:
		// Colour/Style settings are in the main settings. These are per-chart
		// settings that will get saved with Orders

		public ChartSettings()
		{
			Style                  = new ChartControl.RdrOptions();
			Style.AntiAliasing     = false;
			Style.ChartBkColour    = Color.FromArgb(0x00,0x00,0x00);
			Style.XAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);
			Style.YAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);

			TimeFrame        = ETimeFrame.Hour12;
			ViewCandleCount  = 100;
			ViewCandlesAhead = 20;
			TimeFrameBtns    = new [] { ETimeFrame.Min1, ETimeFrame.Hour1, ETimeFrame.Day1 };

			TradeProfitColour = Color.FromArgb(0xFF, 0xaf, 0xfe, 0xaf);
			TradeLossColour   = Color.FromArgb(0xFF, 0xfe, 0xb4, 0xb4);
		}

		/// <summary>Style options for charts</summary>
		public ChartControl.RdrOptions Style
		{
			get { return get(x => x.Style); }
			set { set(x => x.Style, value); }
		}

		/// <summary>The time frame displayed</summary>
		public ETimeFrame TimeFrame
		{
			get { return get(x => x.TimeFrame); }
			set { set(x => x.TimeFrame, value); }
		}

		/// <summary>The number of candles to display in one view of the chart</summary>
		public int ViewCandleCount
		{
			get { return get(x => x.ViewCandleCount); }
			set { set(x => x.ViewCandleCount, value); }
		}

		/// <summary>The number of candles space ahead of the now position when default ranging</summary>
		public int ViewCandlesAhead
		{
			get { return get(x => x.ViewCandlesAhead); }
			set { set(x => x.ViewCandlesAhead, value); }
		}

		/// <summary>The colour of the profit region of a trade in the chart</summary>
		public Color TradeProfitColour
		{
			get { return get(x => x.TradeProfitColour); }
			set { set(x => x.TradeProfitColour, value); }
		}

		/// <summary>The colour of the profit region of a trade in the chart</summary>
		public Color TradeLossColour
		{
			get { return get(x => x.TradeLossColour); }
			set { set(x => x.TradeLossColour, value); }
		}

		/// <summary>The time frames that have buttons on the tool bar</summary>
		public ETimeFrame[] TimeFrameBtns
		{
			get { return get(x => x.TimeFrameBtns); }
			set { set(x => x.TimeFrameBtns, value); }
		}

		private class TyConv :GenericTypeConverter<ChartSettings> {}
	}

}
