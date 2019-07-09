using System;
using System.ComponentModel;
using System.Windows;
using CoinFlip;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Bot.Rebalance
{
	public partial class ConfigureUI : Window, IDisposable, INotifyPropertyChanged
	{
		private readonly Bot m_bot;
		public ConfigureUI(Window owner, Bot bot)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;
			m_bot = bot;

			ChartSelector = new ExchPairTimeFrame(Model);
			GfxPriceRange = null;//todo new GfxPriceRange(BotData.Id);
			Settings.SettingChange += HandleSettingChange;

			// Commands
			Accept = Command.Create(this, AcceptInternal);
			ShowOnChart = Command.Create(this, ShowOnChartInternal);
			AllocateAllAvailableBase = Command.Create(this, AllocateAllAvailableBaseInternal);
			AllocateAllAvailableQuote = Command.Create(this, AllocateAllAvailableQuoteInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			Settings.SettingChange -= HandleSettingChange;
			GfxPriceRange = null;
			ChartSelector = null;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Dispose();
		}

		/// <summary>Bot user settings</summary>
		public SettingsData Settings => m_bot.Settings;

		/// <summary>Application bot data</summary>
		public BotData BotData => m_bot.BotData;

		/// <summary>App logic</summary>
		public Model Model => m_bot.Model;

		/// <summary>The fund assigned to the bot being configured</summary>
		public Fund Fund => m_bot.Fund;

		/// <summary>Trading instrument selector</summary>
		public ExchPairTimeFrame ChartSelector
		{
			get { return m_chart_selector; }
			private set
			{
				if (m_chart_selector == value) return;
				if (m_chart_selector != null)
				{
					m_chart_selector.PropertyChanged -= HandleInstrumentChanged;
				}
				m_chart_selector = value;
				if (m_chart_selector != null)
				{
					m_chart_selector.Exchange = Model.Exchanges[Settings.Exchange];
					m_chart_selector.Pair = m_chart_selector.Exchange?.Pairs[Settings.Pair];
					m_chart_selector.PropertyChanged += HandleInstrumentChanged;
				}

				// Handler
				void HandleInstrumentChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(SettingsData.Exchange):
						{
							Settings.Exchange = ChartSelector.Exchange.Name;
							break;
						}
					case nameof(SettingsData.Pair):
						{
							Settings.Pair = ChartSelector.Pair.Name;
							break;
						}
					}
				}
			}
		}
		private ExchPairTimeFrame m_chart_selector;

		/// <summary>Validate the current settings</summary>
		public Exception Validate
		{
			get
			{
				// Check the settings are valid
				var err = Settings.Validate(Model);
				if (err != null)
					return err;

				var pair = ChartSelector.Pair;
				if (pair == null)
					return new Exception("No trading pair selected");

				// Check that the balances are available in the fund
				if (Fund == null)
					return new Exception("No fund is assigned");
				if (Fund[pair.Base].Available < Settings.BaseCurrencyBalance._(pair.Base))
					return new Exception($"Fund {Fund.Id} does not have enough {pair.Base.Symbol}");
				if (Fund[pair.Quote].Available < Settings.QuoteCurrencyBalance._(pair.Quote))
					return new Exception($"Fund {Fund.Id} does not have enough {pair.Quote.Symbol}");

				return null;
			}
		}

		/// <summary>Ok Button</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}

		/// <summary>Show the price range on a chart</summary>
		public Command ShowOnChart { get; }
		private void ShowOnChartInternal()
		{
			// Get the active chart
			var chart = Model.Charts.ActiveChart;
			chart.Exchange = ChartSelector.Exchange;
			chart.Pair = ChartSelector.Pair;
			chart.TimeFrame = ETimeFrame.Day1;
			chart.EnsureActiveContent();

			// Add graphics + support for resizing price range
		}

		/// <summary>Use all available base currency in the current fund for the bot</summary>
		public Command AllocateAllAvailableBase { get; }
		private void AllocateAllAvailableBaseInternal()
		{
			var available = Fund[ChartSelector.Pair.Base].Available;
			Settings.BaseCurrencyBalance = available;
		}

		/// <summary>Use all available quote currency in the current fund for the bot</summary>
		public Command AllocateAllAvailableQuote { get; }
		private void AllocateAllAvailableQuoteInternal()
		{
			var available = Fund[ChartSelector.Pair.Quote].Available;
			Settings.QuoteCurrencyBalance = available;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Validate)));
		}

		/// <summary>Graphics object for showing the price range</summary>
		private GfxPriceRange GfxPriceRange
		{
			get { return m_gfx_price_range; }
			set
			{
				if (m_gfx_price_range == value) return;
				Util.Dispose(ref m_gfx_price_range);
				m_gfx_price_range = value;
			}
		}
		private GfxPriceRange m_gfx_price_range;
	}
}
