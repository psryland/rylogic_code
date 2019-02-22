using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;

namespace CoinFlip
{
	[Serializable]
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			LastUser                      = string.Empty;
			MainLoopPeriodMS              = 500;
			PriceDataUpdatePeriodMS       = 500;
			MarketOrderPriceToleranceFrac = 0.0001;
			ShowLivePrices                = false;
			BackTesting                   = new BackTestingSettings();
			ChartTemplate                 = new ChartSettings();
			Equity                        = new EquitySettings();
			Coins                         = new CoinData[0];
			Charts                        = new ChartSettings[0];
			Bots                          = new BotData[0];
			Funds                         = new FundData[1] { new FundData(Fund.Main, new FundData.ExchData[0]) };
			UI                            = new UISettings();
			Cryptopia                     = new CrypotopiaSettings();
			Poloniex                      = new PoloniexSettings();
			Bittrex                       = new BittrexSettings();
			Bitfinex                      = new BitfinexSettings();
			CrossExchange                 = new CrossExchangeSettings();

			// Leave this disabled till everything has been set up (i.e. the Model enables it)
			AutoSaveOnChanges = false;
		}
		public Settings(string filepath)
			:base(filepath, throw_on_error: false)
		{
			// Leave this disabled till everything has been set up (i.e. the Model enables it)
			AutoSaveOnChanges = false;
		}
		public override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>The name of the last user to log on</summary>
		public string LastUser
		{
			get { return get<string>(nameof(LastUser)); }
			set { set(nameof(LastUser), value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int MainLoopPeriodMS
		{
			get { return get<int>(nameof(MainLoopPeriodMS)); }
			set { set(nameof(MainLoopPeriodMS), value); }
		}

		/// <summary>The polling period for reading candle data</summary>
		public int PriceDataUpdatePeriodMS
		{
			get { return get<int>(nameof(PriceDataUpdatePeriodMS)); }
			set { set(nameof(PriceDataUpdatePeriodMS), value); }
		}

		/// <summary>An order is considered 'MarketOrder' if the order price is within this tolerance of the current price to fill the order</summary>
		public double MarketOrderPriceToleranceFrac
		{
			get { return get<double>(nameof(MarketOrderPriceToleranceFrac)); }
			set { set(nameof(MarketOrderPriceToleranceFrac), value); }
		}
		public double MarketOrderPriceTolerancePC
		{
			get { return MarketOrderPriceToleranceFrac * 100.0; }
			set { MarketOrderPriceToleranceFrac = value * 0.01; }
		}

		/// <summary>Display the nett worth as a live price</summary>
		public bool ShowLivePrices
		{
			get { return get<bool>(nameof(ShowLivePrices)); }
			set { set(nameof(ShowLivePrices), value); }
		}

		/// <summary>Settings related to back testing</summary>
		public BackTestingSettings BackTesting
		{
			get { return get<BackTestingSettings>(nameof(BackTesting)); }
			set { set(nameof(BackTesting), value); }
		}

		/// <summary>Default settings for charts</summary>
		public ChartSettings ChartTemplate
		{
			get { return get<ChartSettings>(nameof(ChartTemplate)); }
			set { set(nameof(ChartTemplate), value); }
		}

		/// <summary>Settings for the equity plot</summary>
		public EquitySettings Equity
		{
			get { return get<EquitySettings>(nameof(Equity)); }
			set { set(nameof(Equity), value); }
		}

		/// <summary>Meta data for known coins</summary>
		public CoinData[] Coins
		{
			get { return get<CoinData[]>(nameof(Coins)); }
			set { set(nameof(Coins), value); }
		}

		/// <summary>The chart instance settings</summary>
		public ChartSettings[] Charts
		{
			get { return get<ChartSettings[]>(nameof(Charts)); }
			set { set(nameof(Charts), value); }
		}

		/// <summary>The bot instance settings</summary>
		public BotData[] Bots
		{
			get { return get<BotData[]>(nameof(Bots)); }
			set { set(nameof(Bots), value); }
		}

		/// <summary>The partitions of user funds</summary>
		public FundData[] Funds
		{
			get { return get<FundData[]>(nameof(Funds)); }
			set { set(nameof(Funds), value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get<UISettings>(nameof(UI)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(UI), value);
			}
		}

		/// <summary>Cryptopia exchange settings</summary>
		public CrypotopiaSettings Cryptopia
		{
			get { return get<CrypotopiaSettings>(nameof(Cryptopia)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Cryptopia), value);
			}
		}

		/// <summary>Cryptopia exchange settings</summary>
		public PoloniexSettings Poloniex
		{
			get { return get<PoloniexSettings>(nameof(Poloniex)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Poloniex), value);
			}
		}

		/// <summary>Bittrex exchange settings</summary>
		public BittrexSettings Bittrex
		{
			get { return get<BittrexSettings>(nameof(Bittrex)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Bittrex), value);
			}
		}

		/// <summary>Bittrex exchange settings</summary>
		public BitfinexSettings Bitfinex
		{
			get { return get<BitfinexSettings>(nameof(Bitfinex)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Bitfinex), value);
			}
		}

		/// <summary>CrossExchange exchange settings</summary>
		public CrossExchangeSettings CrossExchange
		{
			get { return get<CrossExchangeSettings>(nameof(CrossExchange)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(CrossExchange), value);
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
				ToolStripLayout = new ToolStripLocations();
				WindowPosition  = Rectangle.Empty;
				WindowMaximised = false;
			}

			/// <summary>The dock panel layout</summary>
			public XElement UILayout
			{
				get { return get<XElement>(nameof(UILayout)); }
				set { set(nameof(UILayout), value); }
			}

			/// <summary>The layout of main window tool bars</summary>
			public ToolStripLocations ToolStripLayout
			{
				get { return get<ToolStripLocations>(nameof(ToolStripLayout)); }
				set { set(nameof(ToolStripLayout), value); }
			}

			/// <summary>The last position on screen</summary>
			public Rectangle WindowPosition
			{
				get { return get<Rectangle>(nameof(WindowPosition)); }
				set { set(nameof(WindowPosition), value); }
			}
			public bool WindowMaximised
			{
				get { return get<bool>(nameof(WindowMaximised)); }
				set { set(nameof(WindowMaximised), value); }
			}

			private class TyConv :GenericTypeConverter<UISettings> {}
		}

		/// <summary>A balance partition, a.k.a fund</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		[DebuggerDisplay("{Id}")]
		public class FundData :SettingsXml<FundData>
		{
			public FundData()
				:this(string.Empty, new ExchData[0])
			{}
			public FundData(string id, ExchData[] exch_data)
			{
				Id = id ?? string.Empty;
				Exchanges = exch_data;
			}
			public FundData(XElement node)
				:base(node)
			{}

			/// <summary>The unique id for this fund</summary>
			public string Id
			{
				get { return get<string>(nameof(Id)); }
				set { set(nameof(Id), value); }
			}

			/// <summary>The balances on each exchange</summary>
			public ExchData[] Exchanges
			{
				get { return get<ExchData[]>(nameof(Exchanges)); }
				set { set(nameof(Exchanges), value); }
			}

			/// <summary>The balances assigned to this fund within each exchange</summary>
			[DebuggerDisplay("{Name}")]
			public class ExchData
			{
				public ExchData(string name, BalData[] bal_data)
				{
					Name = name;
					Balances = bal_data;
				}
				public ExchData(XElement node)
				{
					Name = node.Element(nameof(Name)).OrDefault(Name);
					Balances = node.Element(nameof(Balances)).OrDefault(Balances);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(Name), Name, false);
					node.Add2(nameof(Balances), Balances, false);
					return node;
				}

				/// <summary>The name of the exchange</summary>
				public string Name { get; set; }

				/// <summary>Balances for each currency on this exchange in this fund</summary>
				public BalData[] Balances { get; set; }
			}
			[DebuggerDisplay("{Symbol} {Total}")]
			public class BalData
			{
				public BalData(string sym, decimal total)
				{
					Symbol = sym;
					Total  = total;
				}
				public BalData(XElement node)
				{
					Symbol = node.Element(nameof(Symbol)).OrDefault(Symbol);
					Total  = node.Element(nameof(Total )).OrDefault(Total);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(Symbol), Symbol, false);
					node.Add2(nameof(Total), Total, false);
					return node;
				}

				/// <summary>The currency that this balance is for</summary>
				public string Symbol { get; set; }

				/// <summary>The total amount of 'Symbol' from this exchange in this fund</summary>
				public decimal Total { get; set; }
			}
			private class TyConv :GenericTypeConverter<FundData> {}
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
		public class BitfinexSettings :ExchangeSettings<BitfinexSettings>
		{
			public BitfinexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
				MarketDepth = 20;
				ServerRequestRateLimit = 10f;
			}
			private class TyConv :GenericTypeConverter<BitfinexSettings> {}
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
		[TypeConverter(typeof(TyConv))]
		[DebuggerDisplay("{Symbol} {Value} {OfInterest}")]
		public class CoinData :SettingsXml<CoinData>
		{
			public CoinData()
				:this(string.Empty, 1m)
			{}
			public CoinData(string symbol, decimal value)
			{
				Symbol = symbol;
				AssignedValue = value;
				Order = 0;
				OfInterest = false;
				AutoTradingLimit = 1m;
				LivePriceSymbols = "USDT";
				DefaultTradeVolume = 1m;
				BackTestingInitialBalance = 1m;
			}
			public CoinData(XElement node)
				:base(node)
			{ }
			
			/// <summary>The symbol name for the coin</summary>
			public string Symbol
			{
				get { return get<string>(nameof(Symbol)); }
				private set { set(nameof(Symbol), value); }
			}

			/// <summary>Value assigned to this coin</summary>
			public decimal AssignedValue
			{
				get { return get<decimal>(nameof(AssignedValue)); }
				set { set(nameof(AssignedValue), value); }
			}

			/// <summary>The order to display the coins in</summary>
			public int Order
			{
				get { return get<int>(nameof(Order)); }
				set { set(nameof(Order), value); }
			}

			/// <summary>True if coins of this type should be included in loops</summary>
			public bool OfInterest
			{
				get { return get<bool>(nameof(OfInterest)); }
				set { set(nameof(OfInterest), value); }
			}

			/// <summary>The maximum amount of this coin to automatically trade in one go</summary>
			public decimal AutoTradingLimit
			{
				get { return get<decimal>(nameof(AutoTradingLimit)); }
				set { set(nameof(AutoTradingLimit), value); }
			}

			/// <summary>A comma separated list of currencies used to convert this coin to a live price value</summary>
			public string LivePriceSymbols
			{
				get { return get<string>(nameof(LivePriceSymbols)); }
				set { set(nameof(LivePriceSymbols), value); }
			}

			/// <summary>The amount to initialise trades with</summary>
			public decimal DefaultTradeVolume
			{
				get { return get<decimal>(nameof(DefaultTradeVolume)); }
				set { set(nameof(DefaultTradeVolume), value); }
			}

			/// <summary>The initial balance for this currency when back testing</summary>
			public decimal BackTestingInitialBalance
			{
				get { return get<decimal>(nameof(BackTestingInitialBalance)); }
				set { set(nameof(BackTestingInitialBalance), value); }
			}

			private class TyConv :GenericTypeConverter<CoinData> {}
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
				Load = false;
			}
			public BotData(IBot bot)
			{
				BotType = bot.GetType().FullName;
				Config = bot.Settings.ToXml(nameof(Config), true);
				Load = true;
			}
			public BotData(XElement node)
				:base(node)
			{}

			/// <summary>The full name of the bot instance type</summary>
			public string BotType
			{
				get { return get<string>(nameof(BotType)); }
				set { set(nameof(BotType), value); }
			}

			/// <summary>True if this bot should be loaded at startup</summary>
			public bool Load
			{
				get { return get<bool>(nameof(Load)); }
				set { set(nameof(Load), value); }
			}

			/// <summary>Configuration data for the bot instance</summary>
			public XElement Config
			{
				// This is stored as XML because on startup the types used in the settings
				// data for a bot have not been loaded. The settings loader throws if it hits
				// a type it doesn't know about.
				get { return get<XElement>(nameof(Config)); }
				set { set(nameof(Config), value); }
			}

			private class TyConv :GenericTypeConverter<BotData> {}
		}

		/// <summary>Settings for a chart</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class ChartSettings :INotifyPropertyChanged
		{
			// Notes:
			//  - Using inheritance for some settings
			//  - When setting a property, if the value is the same as the inherited value
			//    then the local value is set to null so that the inherited value is used.

			public ChartSettings()
				:this(string.Empty)
			{}
			public ChartSettings(string symbol_code)
			{
				SymbolCode           = symbol_code ?? string.Empty;
				TimeFrame            = ETimeFrame.Hour12;
				m_style              = null;
				m_ask_colour         = null;
				m_bid_colour         = null;
				m_show_positions     = null;
				m_show_trade_history = null;
				m_show_market_depth  = null;
				Indicators           = null;
				ToolStripLayout      = null;
			}
			public ChartSettings(ChartSettings rhs)
			{
				Inherit              = rhs.Inherit;
				SymbolCode           = rhs.SymbolCode;
				TimeFrame            = rhs.TimeFrame;
				m_style              = rhs.m_style;
				m_ask_colour         = rhs.m_ask_colour;
				m_bid_colour         = rhs.m_bid_colour;
				m_show_positions     = rhs.m_show_positions;
				m_show_trade_history = rhs.m_show_trade_history;
				m_show_market_depth  = rhs.m_show_market_depth;
				Indicators           = new XElement(rhs.Indicators);
				ToolStripLayout      = new ToolStripLocations(rhs.ToolStripLayout);
			}
			public ChartSettings(XElement node)
			{
				SymbolCode           = node.Element(nameof(SymbolCode      )).As(SymbolCode          );
				TimeFrame            = node.Element(nameof(TimeFrame       )).As(TimeFrame           );
				m_style              = node.Element(nameof(Style           )).As(m_style             );
				m_ask_colour         = node.Element(nameof(AskColour       )).As(m_ask_colour        );
				m_bid_colour         = node.Element(nameof(BidColour       )).As(m_bid_colour        );
				m_show_positions     = node.Element(nameof(ShowPositions   )).As(m_show_positions    );
				m_show_trade_history = node.Element(nameof(ShowTradeHistory)).As(m_show_trade_history);
				m_show_market_depth  = node.Element(nameof(ShowMarketDepth )).As(m_show_market_depth );
				Indicators           = node.Element(nameof(Indicators      )).As(Indicators          );
				ToolStripLayout      = node.Element(nameof(ToolStripLayout )).As(ToolStripLayout     );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(SymbolCode      ), SymbolCode           , false);
				node.Add2(nameof(TimeFrame       ), TimeFrame            , false);
				node.Add2(nameof(Style           ), m_style              , false);
				node.Add2(nameof(AskColour       ), m_ask_colour         , false);
				node.Add2(nameof(BidColour       ), m_bid_colour         , false);
				node.Add2(nameof(ShowPositions   ), m_show_positions     , false);
				node.Add2(nameof(ShowTradeHistory), m_show_trade_history , false);
				node.Add2(nameof(ShowMarketDepth ), m_show_market_depth  , false);
				node.Add2(Indicators);
				node.Add2(ToolStripLayout);
				return node;
			}

			/// <summary>Setting to inherit</summary>
			public ChartSettings Inherit
			{
				get { return m_inherit; }
				set
				{
					if (m_inherit == value) return;
					if (m_inherit != null)
					{
						m_inherit.PropertyChanged -= HandlePropertyChanged;
					}
					m_inherit = value;
					if (m_inherit != null)
					{
						m_inherit.PropertyChanged += HandlePropertyChanged;
					}

					// Handlers
					void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
					{
						// If a property on our parent changes, and now has the same value as this object,
						// set our property to null (by settings it to the inherited value).
						var pi = GetType().GetProperty(e.PropertyName, BindingFlags.Public|BindingFlags.Instance);
						var local_value = pi.GetValue(this);
						var inheritted_value = pi.GetValue(Inherit);
						if (Equals(local_value, inheritted_value))
							pi.SetValue(this, inheritted_value);
					}
				}
			}
			private ChartSettings m_inherit;

			/// <summary>The symbol code for the chart these settings are for</summary>
			public string SymbolCode
			{
				get { return m_symbol_code; }
				set { SetProp(ref m_symbol_code, value, nameof(SymbolCode)); }
			}
			private string m_symbol_code;

			/// <summary>The time frame displayed</summary>
			public ETimeFrame TimeFrame
			{
				get { return m_time_frame; }
				set { SetProp(ref m_time_frame, value, nameof(TimeFrame)); }
			}
			private ETimeFrame m_time_frame;

			/// <summary>Chart style options</summary>
			public ChartControl.RdrOptions Style
			{
				get { return m_style ?? Inherit?.m_style ?? m_def_style; }
				set
				{
					// Enforce some settings
					value.NavigationMode = ChartControl.ENavMode.Chart2D;
					value.Orthographic = true;
					SetProp(nameof(m_style), value, m_def_style, nameof(Style));
				}
			}
			private ChartControl.RdrOptions m_style;
			private static readonly ChartControl.RdrOptions m_def_style = new ChartControl.RdrOptions{ NavigationMode = ChartControl.ENavMode.Chart2D, Orthographic = true, AntiAliasing = false };

			/// <summary>The colour to draw 'Bullish/Ask/Buy' things</summary>
			public Color AskColour
			{
				get { return m_ask_colour ?? Inherit?.AskColour ?? m_def_ask_colour; }
				set { SetProp(nameof(m_ask_colour), value, m_def_ask_colour, nameof(AskColour)); }
			}
			private static readonly Color m_def_ask_colour = Color_.FromArgb(0xff22b14c);// Green
			private Color? m_ask_colour;

			/// <summary>The colour to draw 'Bearish/Bid/Sell' things</summary>
			public Color BidColour
			{
				get { return m_bid_colour ?? Inherit?.BidColour ?? m_def_bid_colour; }
				set { SetProp(nameof(m_bid_colour), value, m_def_bid_colour, nameof(BidColour)); }
			}
			private static readonly Color m_def_bid_colour = Color_.FromArgb(0xffed1c24); // Red
			private Color? m_bid_colour;

			/// <summary>Show current trades</summary>
			public bool ShowPositions
			{
				get { return m_show_positions ?? Inherit?.ShowPositions ?? m_def_show_positions; }
				set { SetProp(nameof(m_show_positions), value, m_def_show_positions, nameof(ShowPositions)); }
			}
			private const bool m_def_show_positions = false;
			private bool? m_show_positions;

			/// <summary>Show trade history graphics</summary>
			public EShowTradeHistory ShowTradeHistory
			{
				get { return m_show_trade_history ?? Inherit?.ShowTradeHistory ?? m_def_show_trade_history; }
				set { SetProp(nameof(m_show_trade_history), value, m_def_show_trade_history, nameof(ShowTradeHistory)); }
			}
			private const EShowTradeHistory m_def_show_trade_history = EShowTradeHistory.Disabled;
			private EShowTradeHistory? m_show_trade_history;
			public enum EShowTradeHistory { Disabled, Selected, All }

			/// <summary>Show current market depth</summary>
			public bool ShowMarketDepth
			{
				get { return m_show_market_depth ?? Inherit?.ShowMarketDepth ?? m_def_show_market_depth; }
				set { SetProp(nameof(m_show_market_depth), value, m_def_show_market_depth, nameof(ShowMarketDepth)); }
			}
			private const bool m_def_show_market_depth = false;
			private bool? m_show_market_depth;

			/// <summary>Indicators on this chart</summary>
			public XElement Indicators
			{
				get { return m_indicators; }
				set { SetProp(ref m_indicators, value, nameof(Indicators)); }
			}
			private XElement m_indicators;

			/// <summary>The layout of main window tool bars</summary>
			public ToolStripLocations ToolStripLayout
			{
				get { return m_ts_layout; }
				set { SetProp(ref m_ts_layout, value, nameof(ToolStripLayout)); }
			}
			private ToolStripLocations m_ts_layout;

			/// <summary>Helper for setting inherited fields</summary>
			private void SetProp<T>(string field, T value, T def, string prop_name)
			{
				// If the value we're setting 'field' to is the same as the inherited value
				// set 'field' to null instead, so that the inherited value is used.
				var fi = GetType().GetField(field, BindingFlags.NonPublic|BindingFlags.Instance);

				// If there is no inherited object, this is the root.
				if (Inherit == null)
				{
					// Compare to the default value, if the same set ours to null
					if (Equals(value, def))
						fi.SetValue(this, null);
					else
						fi.SetValue(this, value);
				}
				// Otherwise, there is an inherited object
				else
				{
					// Find the first non-null inherited value
					var inherited_value = (object)null;
					for (var p = Inherit; p != null && inherited_value == null; p = p.Inherit)
						inherited_value = fi.GetValue(p);

					// If the inherited value is null, use the default
					// If 'value' is the same as the inherited value, then set ours to null
					// If not, then set our field to 'value'
					if (Equals(value, inherited_value ?? def))
						fi.SetValue(this, null);
					else
						fi.SetValue(this, value);
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}
			private void SetProp<T>(ref T field, T value, string prop_name)
			{
				// This one is for non-inherited properties
				if (Equals(field, value)) return;
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}
			public event PropertyChangedEventHandler PropertyChanged;

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
				StepsPerCandle = 10.0;
				SpreadFrac = 0.001;
				OrderValueRange = new RangeF(0.1, 1000.0);
				OrdersPerBook = 50;
			}
			public BackTestingSettings(XElement node)
				:base(node)
			{}

			/// <summary>The time frame displayed</summary>
			public ETimeFrame TimeFrame
			{
				get { return get<ETimeFrame>(nameof(TimeFrame)); }
				set { set(nameof(TimeFrame), value); }
			}

			/// <summary>The number of time frame steps backwards in time to start from</summary>
			public int Steps
			{
				get { return get<int>(nameof(Steps)); }
				set { set(nameof(Steps), value); }
			}

			/// <summary>The maximum number of steps backwards in time to start back testing from</summary>
			public int MaxSteps
			{
				get { return get<int>(nameof(MaxSteps)); }
				set { set(nameof(MaxSteps), value); }
			}

			/// <summary>How fast the simulation runs through the back testing candle data (in candles/second)</summary>
			public double StepsPerCandle
			{
				get { return get<double>(nameof(StepsPerCandle)); }
				set { set(nameof(StepsPerCandle), value); }
			}

			/// <summary>The price spread to use when back testing (as a fraction)</summary>
			public double SpreadFrac
			{
				get { return get<double>(nameof(SpreadFrac)); }
				set { set(nameof(SpreadFrac), value); }
			}
			public double SpreadPC
			{
				get { return SpreadFrac * 100.0; }
				set { SpreadFrac = value * 0.01; }
			}

			/// <summary>The value in USD of generated orders</summary>
			public RangeF OrderValueRange
			{
				get { return get<RangeF>(nameof(OrderValueRange)); }
				set { set(nameof(OrderValueRange), value); }
			}

			/// <summary>The number of offers on each side of the order book for pairs</summary>
			public int OrdersPerBook
			{
				get { return get<int>(nameof(OrdersPerBook)); }
				set { set(nameof(OrdersPerBook), value); }
			}

			private class TyConv :GenericTypeConverter<BackTestingSettings> {}
		}

		/// <summary>Equity plot settings</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class EquitySettings :SettingsXml<EquitySettings>
		{
			public EquitySettings()
			{
				CommonValues = false;
			}
			public EquitySettings(XElement node)
				:base(node)
			{}

			/// <summary>The time scale to use for the equity plot</summary>
			public bool CommonValues
			{
				get { return get<bool>(nameof(CommonValues)); }
				set { set(nameof(CommonValues), value); }
			}

			private class TyConv :GenericTypeConverter<EquitySettings> {}
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
			get { return get<bool>(nameof(Active)); }
			set { set(nameof(Active), value); }
		}

		/// <summary>Data polling rate (in ms)</summary>
		public int PollPeriod
		{
			get { return get<int>(nameof(PollPeriod)); }
			set { set(nameof(PollPeriod), value); }
		}

		/// <summary>The fee charged per trade</summary>
		public decimal TransactionFee
		{
			get { return get<decimal>(nameof(TransactionFee)); }
			set { set(nameof(TransactionFee), value); }
		}

		/// <summary>The market depth to retrieve</summary>
		public int MarketDepth
		{
			get { return get<int>(nameof(MarketDepth)); }
			set { set(nameof(MarketDepth), value); }
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit
		{
			get { return get<float>(nameof(ServerRequestRateLimit)); }
			set { set(nameof(ServerRequestRateLimit), value); }
		}
	}
}
