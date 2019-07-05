using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CoinFlip;
using Rylogic.Gui.WPF;

namespace Bot.Rebalance
{
	public partial class ConfigureUI : Window, INotifyPropertyChanged
	{
		public ConfigureUI(Window owner, Model model, SettingsData settings)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;

			Settings = settings;
			ChartSelector = new ExchPairTimeFrame(model);
			ChartSelector.PropertyChanged += HandleInstrumentChanged;
			Funds = new ListCollectionView(model.Funds);
			Funds.CurrentChanged += delegate { settings.FundId = ((Fund)Funds.CurrentItem)?.Id; };

			// Commands
			Accept = Command.Create(this, AcceptInternal);
			ShowOnChart = Command.Create(this, ShowOnChartInternal);

			DataContext = this;
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = !IsValid;
			base.OnClosing(e);
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings { get; }

		/// <summary>Trading instrument selector</summary>
		public ExchPairTimeFrame ChartSelector { get; }

		/// <summary>The funds to choose from</summary>
		public ICollectionView Funds { get; }

		/// <summary>True if the configuration is valid</summary>
		public bool IsValid
		{
			get
			{
				return 
					Settings.AllInPrice < Settings.AllOutPrice &&
					true;
			}
		}

		/// <summary>Ok Button</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (!IsValid) return;
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
		private void HandleInstrumentChanged(object sender, PropertyChangedEventArgs e)
		{
			
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
