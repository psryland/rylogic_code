using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
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
			m_bot.BotData.SettingChange += HandleSettingChange;

			// Commands
			Accept = Command.Create(this, AcceptInternal);
			ShowOnChart = Command.Create(this, ShowOnChartInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			m_bot.BotData.SettingChange -= HandleSettingChange;
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
		public Fund Fund
		{
			get => m_bot.Fund;
			set => m_bot.Fund = value;
		}

		/// <summary>The amount held in base currency</summary>
		public Unit<decimal> HoldingsBase => ChartSelector.Pair != null ? Fund[ChartSelector.Pair.Base].Total : 0m._();

		/// <summary>The amount held in quote currency</summary>
		public Unit<decimal> HoldingsQuote => ChartSelector.Pair != null ? Fund[ChartSelector.Pair.Quote].Total : 0m._();

		/// <summary>The available funds</summary>
		public ICollectionView Funds => CollectionViewSource.GetDefaultView(m_bot.Model.Funds);

		/// <summary>Trading instrument selector</summary>
		public ExchPairTimeFrame ChartSelector
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					field.PropertyChanged -= HandleInstrumentChanged;
				}
				field = value;
				if (field != null)
				{
					field.Exchange = Model.Exchanges[Settings.Exchange];
					field.Pair = field.Exchange?.Pairs[Settings.Pair];
					field.PropertyChanged += HandleInstrumentChanged;
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

		/// <summary>Validate the current settings</summary>
		public Exception Validate
		{
			get
			{
				var pair = ChartSelector.Pair;
				if (pair == null)
					return new Exception("No trading pair selected");

				// Check that the balances are available in the fund
				if (Fund == null)
					return new Exception("No fund is assigned");
				//if (Fund[pair.Base].Available < Settings.BaseCurrencyBalance._(pair.Base))
				//	return new Exception($"Fund {Fund.Id} does not have enough {pair.Base.Symbol}");
				//if (Fund[pair.Quote].Available < Settings.QuoteCurrencyBalance._(pair.Quote))
				//	return new Exception($"Fund {Fund.Id} does not have enough {pair.Quote.Symbol}");

				// Check the settings are valid
				var err = Settings.Validate(Model, Fund);
				if (err != null)
					return err;

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

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Validate)));
		}

		/// <summary>Graphics object for showing the price range</summary>
		private GfxPriceRange GfxPriceRange
		{
			get;
			set
			{
				if (field == value) return;
				Util.Dispose(ref field);
				field = value;
			}
		}
	}
}
