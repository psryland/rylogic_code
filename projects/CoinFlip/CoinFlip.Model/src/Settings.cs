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
			LastUser                = string.Empty;
			MainLoopPeriodMS        = 500;
			PriceDataUpdatePeriodMS = 500;
			ShowLivePrices          = false;
			BackTesting             = new BackTestingSettings();
			ChartTemplate           = new ChartSettings();
			Coins                   = new CoinData[0];
			Charts                  = new ChartSettings[0];
			Bots                    = new BotData[0];
			UI                      = new UISettings();
			Cryptopia               = new CrypotopiaSettings();
			Poloniex                = new PoloniexSettings();
			Bittrex                 = new BittrexSettings();
			CrossExchange           = new CrossExchangeSettings();
			AutoSaveOnChanges       = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>The name of the last user to log on</summary>
		public string LastUser
		{
			get { return get(x => x.LastUser); }
			set { set(x => x.LastUser, value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int MainLoopPeriodMS
		{
			get { return get(x => x.MainLoopPeriodMS); }
			set { set(x => x.MainLoopPeriodMS, value); }
		}

		/// <summary>The polling period for reading candle data</summary>
		public int PriceDataUpdatePeriodMS
		{
			get { return get(x => x.PriceDataUpdatePeriodMS); }
			set { set(x => x.PriceDataUpdatePeriodMS, value); }
		}

		/// <summary>Display the nett worth as a live price</summary>
		public bool ShowLivePrices
		{
			get { return get(x => x.ShowLivePrices); }
			set { set(x => x.ShowLivePrices, value); }
		}

		/// <summary>Settings related to back testing</summary>
		public BackTestingSettings BackTesting
		{
			get { return get(x => x.BackTesting); }
			set { set(x => x.BackTesting, value); }
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

		/// <summary>The chart instance settings</summary>
		public ChartSettings[] Charts
		{
			get { return get(x => x.Charts); }
			set { set(x => x.Charts, value); }
		}

		/// <summary>The bot instance settings</summary>
		public BotData[] Bots
		{
			get { return get(x => x.Bots); }
			set { set(x => x.Bots, value); }
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
		public class CrypotopiaSettings :ExchangeSettings<CrypotopiaSettings>
		{
			public CrypotopiaSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.002m;
				MarketDepth = 20;
				ServerRequestRateLimit = 10f;
			}
			private class TyConv :GenericTypeConverter<CrypotopiaSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class PoloniexSettings :ExchangeSettings<PoloniexSettings>
		{
			public PoloniexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
				MarketDepth = 20;
				ServerRequestRateLimit = 6f;
			}
			private class TyConv :GenericTypeConverter<PoloniexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class BittrexSettings :ExchangeSettings<BittrexSettings>
		{
			public BittrexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
				MarketDepth = 20;
				ServerRequestRateLimit = 10f;
			}
			private class TyConv :GenericTypeConverter<BittrexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrossExchangeSettings :ExchangeSettings<CrossExchangeSettings>
		{
			public CrossExchangeSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0m;
				MarketDepth = 20;
				ServerRequestRateLimit = 1_000_000f;
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

		/// <summary>Data for each loaded bot instance</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class BotData :SettingsXml<BotData>
		{
			public BotData()
			{
				BotType = string.Empty;
				Config = null;
			}
			public BotData(IBot bot)
			{
				BotType = bot.GetType().FullName;
				Config = bot.Settings.ToXml(nameof(Config), true);
			}
			public BotData(XElement node)
				:base(node)
			{}

			/// <summary>The full name of the bot instance type</summary>
			public string BotType
			{
				get { return get(x => x.BotType); }
				set { set(x => x.BotType, value); }
			}

			/// <summary>Configuration data for the bot instance</summary>
			public XElement Config
			{
				// This is stored as XML because on startup the types used in the settings
				// data for a bot have not been loaded. The settings loader throws if it hits
				// a type it doesn't know about.
				get { return get(x => x.Config); }
				set { set(x => x.Config, value); }
			}

			private class TyConv :GenericTypeConverter<BotData> {}
		}

		/// <summary>Settings for a chart</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class ChartSettings
		{
			public ChartSettings()
				:this(string.Empty)
			{}
			public ChartSettings(string symbol_code)
			{
				SymbolCode          = symbol_code ?? string.Empty;
				TimeFrame           = ETimeFrame.Hour12;
				m_style             = null;
				m_ask_colour        = null;
				m_bid_colour        = null;
				m_show_positions    = null;
				m_show_market_depth = null;
				Indicators          = null;
			}
			public ChartSettings(ChartSettings rhs)
			{
				Inherit             = rhs.Inherit;
				SymbolCode          = rhs.SymbolCode;
				TimeFrame           = rhs.TimeFrame;
				m_style             = rhs.m_style;
				m_ask_colour        = rhs.m_ask_colour;
				m_bid_colour        = rhs.m_bid_colour;
				m_show_positions    = rhs.m_show_positions;
				m_show_market_depth = rhs.m_show_market_depth;
				Indicators          = new XElement(rhs.Indicators);
			}
			public ChartSettings(XElement node)
			{
				SymbolCode          = node.Element(nameof(SymbolCode     )).As(SymbolCode         );
				TimeFrame           = node.Element(nameof(TimeFrame      )).As(TimeFrame          );
				m_style             = node.Element(nameof(Style          )).As(m_style            );
				m_ask_colour        = node.Element(nameof(AskColour      )).As(m_ask_colour       );
				m_bid_colour        = node.Element(nameof(BidColour      )).As(m_bid_colour       );
				m_show_positions    = node.Element(nameof(ShowPositions  )).As(m_show_positions   );
				m_show_market_depth = node.Element(nameof(ShowMarketDepth)).As(m_show_market_depth);
				Indicators          = node.Element(nameof(Indicators     )).As(Indicators         );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(SymbolCode     ), SymbolCode         , false);
				node.Add2(nameof(TimeFrame      ), TimeFrame          , false);
				node.Add2(nameof(Style          ), m_style            , false);
				node.Add2(nameof(AskColour      ), m_ask_colour       , false);
				node.Add2(nameof(BidColour      ), m_bid_colour       , false);
				node.Add2(nameof(ShowPositions  ), m_show_positions   , false);
				node.Add2(nameof(ShowMarketDepth), m_show_market_depth, false);
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

			/// <summary>Show current market depth</summary>
			public bool ShowMarketDepth
			{
				get { return m_show_market_depth ?? Inherit?.m_show_market_depth ?? m_def_show_market_depth; }
				set { SetProp(nameof(m_show_market_depth), value, m_def_show_market_depth); }
			}
			private bool? m_show_market_depth;
			private const bool m_def_show_market_depth = false;

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

			private class TyConv :GenericTypeConverter<ChartSettings> {}
		}

		/// <summary>Settings related to back testing</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class BackTestingSettings :SettingsXml<BackTestingSettings>
		{
			public BackTestingSettings()
			{
				TimeFrame = ETimeFrame.Day1;
				MaxSteps = 30_000;
				Steps = 0;
				StepRate = 1.0;
				SubStepsPerCandle = 10;
				SpreadFrac = 0.001;
			}
			public BackTestingSettings(XElement node)
				:base(node)
			{}

			/// <summary>The time frame displayed</summary>
			public ETimeFrame TimeFrame
			{
				get { return get(x => x.TimeFrame); }
				set { set(x => x.TimeFrame, value); }
			}

			/// <summary>The number of time frame steps backwards in time to start from</summary>
			public int Steps
			{
				get { return get(x => x.Steps); }
				set { set(x => x.Steps, value); }
			}

			/// <summary>The maximum number of steps backwards in time to start back testing from</summary>
			public int MaxSteps
			{
				get { return get(x => x.MaxSteps); }
				set { set(x => x.MaxSteps, value); }
			}

			/// <summary>How fast the simulation runs through the back testing candle data (in candle updates/second)</summary>
			public double StepRate
			{
				get { return get(x => x.StepRate); }
				set { set(x => x.StepRate, value); }
			}
			
			/// <summary>Sub divisions of each candle when stepping the simulation</summary>
			public int SubStepsPerCandle
			{
				get { return get(x => x.SubStepsPerCandle); }
				set { set(x => x.SubStepsPerCandle, value); }
			}

			/// <summary>The price spread to use when back testing (as a fraction)</summary>
			public double SpreadFrac
			{
				get { return get(x => x.SpreadFrac); }
				set { set(x => x.SpreadFrac, value); }
			}
			public double SpreadPC
			{
				get { return SpreadFrac * 100.0; }
				set { SpreadFrac = value * 0.01; }
			}

			public class PairData
			{
				public bool Active { get; set; }
			}
			public class CoinData
			{
				public decimal InitialBalance { get; set; }
			}
			private class TyConv :GenericTypeConverter<ChartSettings> {}
		}

		/// <summary></summary>
		public override void Upgrade(XElement old_settings, string from_version)
		{
			for (var vers = from_version; vers != Version;)
			{
				switch (vers)
				{
				default:
					{
						base.Upgrade(old_settings, from_version);
						break;
					}
				case "v1.0":
					{
						break;
					}
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

		/// <summary>The market depth to retrieve</summary>
		int MarketDepth { get; set; }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		float ServerRequestRateLimit { get; set; }
	}
	public class ExchangeSettings<T> :SettingsSet<T> ,IExchangeSettings where T:SettingsSet<T>, IExchangeSettings, new()
	{
		public ExchangeSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025m;
			MarketDepth = 20;
			ServerRequestRateLimit = 10f;
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

		/// <summary>The market depth to retrieve</summary>
		public int MarketDepth
		{
			get { return get(x => x.MarketDepth); }
			set { set(x => x.MarketDepth, value); }
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit
		{
			get { return get(x => x.ServerRequestRateLimit); }
			set { set(x => x.ServerRequestRateLimit, value); }
		}
	}
}
