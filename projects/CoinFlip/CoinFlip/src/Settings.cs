using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	[Serializable]
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			LastUser          = string.Empty;
			MaximumLoopCount  = 5;
			MainLoopPeriod    = 500;
			ShowLivePrices    = false;
			ChartTemplate     = new ChartSettings();
			Coins             = new CoinData[0];
			Fishing           = new FishingData[0];
			Charts            = new ChartSettings[0];
			UI                = new UISettings();
			Cryptopia         = new CrypotopiaSettings();
			Poloniex          = new PoloniexSettings();
			Bittrex           = new BittrexSettings();
			CrossExchange     = new CrossExchangeSettings();
			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The name of the last user to log on</summary>
		public string LastUser
		{
			get { return get(x => x.LastUser); }
			set { set(x => x.LastUser, value); }
		}

		/// <summary>The maximum number of hops in a loop</summary>
		public int MaximumLoopCount
		{
			get { return get(x => x.MaximumLoopCount); }
			set { set(x => x.MaximumLoopCount, value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int MainLoopPeriod
		{
			get { return get(x => x.MainLoopPeriod); }
			set { set(x => x.MainLoopPeriod, value); }
		}

		/// <summary>Display the nett worth as a live price</summary>
		public bool ShowLivePrices
		{
			get { return get(x => x.ShowLivePrices); }
			set { set(x => x.ShowLivePrices, value); }
		}

		/// <summary>Default settings for charts</summary>
		public ChartSettings ChartTemplate
		{
			get { return get(x => x.ChartTemplate); }
			set { set(x => x.ChartTemplate, value); }
		}

		/// <summary>Meta data for known coins</summary>
		public CoinData[] Coins
		{
			get { return get(x => x.Coins); }
			set { set(x => x.Coins, value); }
		}

		/// <summary>The fishing instances</summary>
		public FishingData[] Fishing
		{
			get { return get(x => x.Fishing); }
			set { set(x => x.Fishing, value); }
		}

		/// <summary>The chart instance settings</summary>
		public ChartSettings[] Charts
		{
			get { return get(x => x.Charts); }
			set { set(x => x.Charts, value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.UI, value);
			}
		}

		/// <summary>Cryptopia exchange settings</summary>
		public CrypotopiaSettings Cryptopia
		{
			get { return get(x => x.Cryptopia); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Cryptopia, value);
			}
		}

		/// <summary>Cryptopia exchange settings</summary>
		public PoloniexSettings Poloniex
		{
			get { return get(x => x.Poloniex); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Poloniex, value);
			}
		}

		/// <summary>Bittrex exchange settings</summary>
		public BittrexSettings Bittrex
		{
			get { return get(x => x.Bittrex); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Bittrex, value);
			}
		}

		/// <summary>CrossExchange exchange settings</summary>
		public CrossExchangeSettings CrossExchange
		{
			get { return get(x => x.CrossExchange); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.CrossExchange, value);
			}
		}

		/// <summary>Settings associated with a connection to Rex via RexLink</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class UISettings :SettingsSet<UISettings>
		{
			public UISettings()
			{
				UILayout        = null;
				WindowPosition  = Rectangle.Empty;
				WindowMaximised = false;
			}

			/// <summary>The dock panel layout</summary>
			public XElement UILayout
			{
				get { return get(x => x.UILayout); }
				set { set(x => x.UILayout, value); }
			}

			/// <summary>The last position on screen</summary>
			public Rectangle WindowPosition
			{
				get { return get(x => x.WindowPosition); }
				set { set(x => x.WindowPosition, value); }
			}
			public bool WindowMaximised
			{
				get { return get(x => x.WindowMaximised); }
				set { set(x => x.WindowMaximised, value); }
			}

			private class TyConv :GenericTypeConverter<UISettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrypotopiaSettings :SettingsSet<CrypotopiaSettings> ,IExchangeSettings
		{
			public CrypotopiaSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.002m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<CrypotopiaSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class PoloniexSettings :SettingsSet<PoloniexSettings> ,IExchangeSettings
		{
			public PoloniexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<PoloniexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class BittrexSettings :SettingsSet<BittrexSettings> ,IExchangeSettings
		{
			public BittrexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<BittrexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrossExchangeSettings :SettingsSet<CrossExchangeSettings> ,IExchangeSettings
		{
			public CrossExchangeSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<CrossExchangeSettings> {}
		}

		/// <summary>Meta data for a coin</summary>
		[Serializable]
		[DebuggerDisplay("{Symbol} {Value} {OfInterest}")]
		public class CoinData :INotifyPropertyChanged
		{
			public CoinData(string symbol, decimal value)
			{
				Symbol = symbol;
				Value = value;
				OfInterest = false;
				AutoTradingLimit = 1m;
				LivePriceSymbols = "USDT";
			}
			public CoinData(XElement node)
			{
				Symbol           = node.Element(nameof(Symbol)).As(Symbol);
				Value            = node.Element(nameof(Value)).As(Value);
				OfInterest       = node.Element(nameof(OfInterest)).As(OfInterest);
				AutoTradingLimit = node.Element(nameof(AutoTradingLimit)).As(AutoTradingLimit);
				LivePriceSymbols = node.Element(nameof(LivePriceSymbols)).As(LivePriceSymbols);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Symbol), Symbol, false);
				node.Add2(nameof(Value), Value, false);
				node.Add2(nameof(OfInterest), OfInterest, false);
				node.Add2(nameof(AutoTradingLimit), AutoTradingLimit, false);
				node.Add2(nameof(LivePriceSymbols), LivePriceSymbols, false);
				return node;
			}

			/// <summary>The symbol name for the coin</summary>
			public string Symbol { get; private set; }

			/// <summary>Value assigned to this coin</summary>
			public decimal Value
			{
				get { return m_value; }
				set { SetProp(ref m_value, value, nameof(Value)); }
			}
			private decimal m_value;

			/// <summary>True if coins of this type should be included in loops</summary>
			public bool OfInterest
			{
				get { return m_of_interest; }
				set { SetProp(ref m_of_interest, value, nameof(OfInterest)); }
			}
			private bool m_of_interest;

			/// <summary>The maximum amount of this coin to automatically trade in one go</summary>
			public decimal AutoTradingLimit
			{
				get { return m_auto_trade_limit; }
				set { SetProp(ref m_auto_trade_limit, value, nameof(AutoTradingLimit)); }
			}
			private decimal m_auto_trade_limit;

			/// <summary>A comma separated list of currencies used to convert this coin to a live price value</summary>
			public string LivePriceSymbols
			{
				get { return m_live_price_symbols ?? string.Empty; }
				set { SetProp(ref m_live_price_symbols, value, nameof(LivePriceSymbols)); }
			}
			private string m_live_price_symbols;

			/// <summary>Property changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
			}
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[Serializable]
		[DebuggerDisplay("{Pair} {Exch0} {Exch1}")]
		[TypeConverter(typeof(TyConv))]
		public class FishingData :SettingsXml<FishingData>
		{
			public FishingData()
			{
				Pair         = string.Empty;
				Exch0        = string.Empty;
				Exch1        = string.Empty;
				PriceOffset  = 0m;
				Direction    = ETradeDirection.None;
			}
			public FishingData(string pair, string exch0, string exch1, decimal price_offset, ETradeDirection direction)
			{
				Pair         = pair;
				Exch0        = exch0;
				Exch1        = exch1;
				PriceOffset  = price_offset;
				Direction    = direction;
			}
			public FishingData(FishingData rhs)
			{
				Pair         = rhs.Pair;
				Exch0        = rhs.Exch0;
				Exch1        = rhs.Exch1;
				PriceOffset  = rhs.PriceOffset;
				Direction    = rhs.Direction;
			}
			public FishingData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the pair to trade</summary>
			public string Pair
			{
				get { return get(x => x.Pair); }
				set { set(x => x.Pair, value); }
			}

			/// <summary>The name of the reference exchange</summary>
			public string Exch0
			{
				get { return get(x => x.Exch0); }
				set { set(x => x.Exch0, value); }
			}

			/// <summary>The name of the target exchange</summary>
			public string Exch1
			{
				get { return get(x => x.Exch1); }
				set { set(x => x.Exch1, value); }
			}

			/// <summary>The price offset range (as a fraction of the reference price)</summary>
			public decimal PriceOffset
			{
				get { return get(x => x.PriceOffset); }
				set { set(x => x.PriceOffset, value); }
			}

			/// <summary>The directions to fish in</summary>
			public ETradeDirection Direction
			{
				get { return get(x => x.Direction); }
				set { set(x => x.Direction, value); }
			}

			/// <summary>An identifying name for this fishing instance</summary>
			public string Name
			{
				get { return $"{Pair.Replace("/",string.Empty)}-{Exch0}-{Exch1}"; }
			}

			/// <summary>True if this object contains valid data</summary>
			public bool Valid
			{
				get
				{
					return 
						Exch0 != Exch1 &&
						Pair.HasValue() &&
						PriceOffset > 0 &&
						Direction != ETradeDirection.None;
				}
			}

			/// <summary>Return the string description of why 'Valid' is false</summary>
			public string ReasonInvalid
			{
				get
				{
					return Str.Build(
						Exch0 == Exch1                    ? "Exchanges are the same\r\n"          : string.Empty,
						!Pair.HasValue()                  ? "No trading pair\r\n"                 : string.Empty,
						PriceOffset <= 0                  ? "Price offset invalid\r\n"            : string.Empty,
						Direction == ETradeDirection.None ? "No trading direction\r\n"            : string.Empty);
				}
			}

			private class TyConv :GenericTypeConverter<FishingData> {}
		}

		/// <summary>Settings for a chart</summary>
		[Serializable]
		public class ChartSettings
		{
			public ChartSettings(string symbol_code = null)
			{
				SymbolCode       = symbol_code ?? string.Empty;
				TimeFrame        = ETimeFrame.Hour12;
				m_style          = null;
				m_ask_colour     = null;
				m_bid_colour     = null;
				m_show_positions = null;
				Indicators       = null;
			}
			public ChartSettings(ChartSettings rhs)
			{
				Inherit          = rhs.Inherit;
				SymbolCode       = rhs.SymbolCode;
				TimeFrame        = rhs.TimeFrame;
				m_style          = rhs.m_style;
				m_ask_colour     = rhs.m_ask_colour;
				m_bid_colour     = rhs.m_bid_colour;
				m_show_positions = rhs.m_show_positions;
				Indicators       = new XElement(rhs.Indicators);
			}
			public ChartSettings(XElement node)
			{
				SymbolCode       = node.Element(nameof(SymbolCode   )).As(SymbolCode      );
				TimeFrame        = node.Element(nameof(TimeFrame    )).As(TimeFrame       );
				m_style          = node.Element(nameof(Style        )).As(m_style         );
				m_ask_colour     = node.Element(nameof(AskColour    )).As(m_ask_colour    );
				m_bid_colour     = node.Element(nameof(BidColour    )).As(m_bid_colour    );
				m_show_positions = node.Element(nameof(ShowPositions)).As(m_show_positions);
				Indicators       = node.Element(nameof(Indicators   )).As(Indicators      );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(SymbolCode   ), SymbolCode      , false);
				node.Add2(nameof(TimeFrame    ), TimeFrame       , false);
				node.Add2(nameof(Style        ), m_style         , false);
				node.Add2(nameof(AskColour    ), m_ask_colour    , false);
				node.Add2(nameof(BidColour    ), m_bid_colour    , false);
				node.Add2(nameof(ShowPositions), m_show_positions, false);
				node.Add2(Indicators);
				return node;
			}

			/// <summary>Setting to inherit</summary>
			public ChartSettings Inherit { get; set; }

			/// <summary>The symbol code for the chart these settings are for</summary>
			public string SymbolCode { get; set; }

			/// <summary>The time frame displayed</summary>
			public ETimeFrame TimeFrame { get; set; }

			/// <summary>Chart style options</summary>
			public ChartControl.RdrOptions Style
			{
				get { return m_style ?? Inherit?.m_style ?? m_def_style; }
				set { SetProp(nameof(m_style), value, m_def_style); }
			}
			private ChartControl.RdrOptions m_style;
			private static readonly ChartControl.RdrOptions m_def_style = new ChartControl.RdrOptions{AntiAliasing = false};

			/// <summary>The colour to draw 'Bullish/Ask/Buy' things</summary>
			public Color AskColour
			{
				get { return BullishColour; }
			}
			public Color BullishColour
			{
				get { return m_ask_colour ?? Inherit?.m_ask_colour ?? m_def_ask_colour; }
				set { SetProp(nameof(m_ask_colour), value, m_def_ask_colour); }
			}
			private Color? m_ask_colour;
			private static readonly Color m_def_ask_colour = Color_.FromArgb(0xff22b14c);// Green

			/// <summary>The colour to draw 'Bearish/Bid/Sell' things</summary>
			public Color BidColour
			{
				get { return BearishColour; }
			}
			public Color BearishColour
			{
				get { return m_bid_colour ?? Inherit?.m_bid_colour ?? m_def_bid_colour; }
				set { SetProp(nameof(m_bid_colour), value, m_def_bid_colour); }
			}
			private Color? m_bid_colour;
			private static readonly Color m_def_bid_colour = Color_.FromArgb(0xffed1c24); // Red

			/// <summary>Show current trades</summary>
			public bool ShowPositions
			{
				get { return m_show_positions ?? Inherit?.m_show_positions ?? m_def_show_positions; }
				set { SetProp(nameof(m_show_positions), value, m_def_show_positions); }
			}
			private bool? m_show_positions;
			private const bool m_def_show_positions = false;

			/// <summary>Indicators on this chart</summary>
			public XElement Indicators { get; set; }

			/// <summary>Helper for setting inherited fields</summary>
			private void SetProp<T>(string field, T value, T def)
			{
				var fi = GetType().GetField(field, BindingFlags.NonPublic|BindingFlags.Instance);
				if (Inherit != null && Equals(fi.GetValue(Inherit), value))
				{
					fi.SetValue(this, null);
					Inherit.SetProp(field, value, def);
				}
				else if (Equals(value, def))
				{
					fi.SetValue(this, null);
				}
				else
				{
					fi.SetValue(this, value);
				}
			}
		}
	}

	public interface IExchangeSettings
	{
		/// <summary>True if the exchange is active</summary>
		bool Active { get; set; }

		/// <summary>Data polling rate (in ms)</summary>
		int PollPeriod { get; set; }

		/// <summary>The fee charged per trade</summary>
		decimal TransactionFee { get; set; }
	}
}
