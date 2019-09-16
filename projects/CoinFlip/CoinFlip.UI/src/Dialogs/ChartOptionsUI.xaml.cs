using System;
using System.ComponentModel;
using System.Windows;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Dialogs
{
	public partial class ChartOptionsUI :Window, INotifyPropertyChanged
	{
		public ChartOptionsUI(Window owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;
			DataContext = this;

			SettingsData.Settings.Chart.SettingChange += HandleSettingChange;
			Closed += delegate { SettingsData.Settings.Chart.SettingChange += HandleSettingChange; };
			void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				switch (e.Key)
				{
				case nameof(ChartSettings.TradeLabelSize):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TradeLabelSize)));
					break;
				case nameof(ChartSettings.TradeLabelTransparency):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TradeLabelTransparency)));
					break;
				case nameof(ChartSettings.ShowTradeDescriptions):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTradeDescriptions)));
					break;
				case nameof(ChartSettings.LabelsToTheLeft):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelsToTheLeft)));
					break;
				}
			}
		}

		/// <summary>Show the singleton instance of this dialog</summary>
		public static void Show(Window owner, Point pt)
		{
			if (m_dlg_chart_options == null || m_dlg_chart_options.Owner != owner)
			{
				m_dlg_chart_options?.Close();
				m_dlg_chart_options = new ChartOptionsUI(owner);
			}
			m_dlg_chart_options.Show();
			m_dlg_chart_options.SetLocation(pt.X - m_dlg_chart_options.DesiredSize.Width, pt.Y).OnScreen();

		}
		private static ChartOptionsUI m_dlg_chart_options;

		/// <summary>The size of trade labels</summary>
		public double TradeLabelSize
		{
			get => SettingsData.Settings.Chart.TradeLabelSize;
			set => SettingsData.Settings.Chart.TradeLabelSize = value;
		}

		/// <summary>The transparency of trade label backgrounds</summary>
		public double TradeLabelTransparency
		{
			get => SettingsData.Settings.Chart.TradeLabelTransparency * 100.0;
			set => SettingsData.Settings.Chart.TradeLabelTransparency = value / 100.0;
		}

		/// <summary>Show the descriptions next to the trade markers</summary>
		public bool ShowTradeDescriptions
		{
			get => SettingsData.Settings.Chart.ShowTradeDescriptions;
			set => SettingsData.Settings.Chart.ShowTradeDescriptions = value;
		}

		/// <summary>Show the labels to the left of the trade markers</summary>
		public bool LabelsToTheLeft
		{
			get => SettingsData.Settings.Chart.LabelsToTheLeft;
			set => SettingsData.Settings.Chart.LabelsToTheLeft = value;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
