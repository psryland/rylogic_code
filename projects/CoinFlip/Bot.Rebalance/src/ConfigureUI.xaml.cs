using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CoinFlip;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace Bot.Rebalance
{
	public partial class ConfigureUI : Window, IDisposable, INotifyPropertyChanged
	{
		public ConfigureUI(Window owner, Model model, SettingsData settings)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;

			Model = model;
			Settings = settings;
			ChartSelector = new ExchPairTimeFrame(model);
			AvailableFunds = new ListCollectionView(model.Funds);
			
			// Commands
			Accept = Command.Create(this, AcceptInternal);
			ShowOnChart = Command.Create(this, ShowOnChartInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			ChartSelector = null;
			AvailableFunds = null;
			Settings = null;
			Model = null;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Dispose();
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings
		{
			get { return m_settings; }
			private set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
				}

				// Handler
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Validate)));
				}
			}
		}
		private SettingsData m_settings;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;

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

		/// <summary>The funds to choose from</summary>
		public ICollectionView AvailableFunds
		{
			get { return m_available_funds; }
			private set
			{
				if (m_available_funds == value) return;
				if (m_available_funds != null)
				{
					m_available_funds.CurrentChanged -= HandleCurrentChanged;
				}
				m_available_funds = value;
				if (m_available_funds != null)
				{
					m_available_funds.MoveCurrentTo(Model.Funds[Settings.FundId]);
					m_available_funds.CurrentChanged += HandleCurrentChanged;
				}

				// Handler
				void HandleCurrentChanged(object sender, EventArgs args)
				{
					var fund = (Fund)AvailableFunds.CurrentItem;
					Settings.FundId = fund?.Id;
				}
			}
		}
		private ICollectionView m_available_funds;

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

				// Check against available funds
				var fund = Model.Funds[Settings.FundId];
				if (fund == null)
					return new Exception($"Fund {fund} not available");

				// Check that the balances are available in the fund
				if (fund[pair.Base].Available < Settings.BaseCurrencyBalance._(pair.Base))
					return new Exception($"Fund {fund.Id} does not have enough {pair.Base.Symbol}");
				if (fund[pair.Quote].Available < Settings.QuoteCurrencyBalance._(pair.Quote))
					return new Exception($"Fund {fund.Id} does not have enough {pair.Quote.Symbol}");

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
			// Look for a chart for the selected instrument
			// If none found, create one
			// Add graphics + support for resizing price range
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T new_value, string prop_name)
		{
			if (Equals(prop, new_value)) return;
			prop = new_value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
