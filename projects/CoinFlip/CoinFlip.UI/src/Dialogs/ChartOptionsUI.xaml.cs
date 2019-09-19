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
			PinState = new PinData(this);
			DataContext = this;

			SettingsData.Settings.Chart.SettingChange += HandleSettingChange;
			Closed += delegate { SettingsData.Settings.Chart.SettingChange += HandleSettingChange; };
			void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				switch (e.Key)
				{
				case nameof(ChartSettings.ConfettiLabelSize):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TradeLabelSize)));
					break;
				case nameof(ChartSettings.ConfettiLabelTransparency):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TradeLabelTransparency)));
					break;
				case nameof(ChartSettings.ConfettiDescriptionsVisible):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTradeDescriptions)));
					break;
				case nameof(ChartSettings.ConfettiLabelsToTheLeft):
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelsToTheLeft)));
					break;
				}
			}
		}

		/// <summary>Show the singleton instance of this dialog</summary>
		public static void Show(Window owner, Point pt)
		{
			if (m_dlg_chart_options == null)
			{
				m_dlg_chart_options = new ChartOptionsUI(owner);
				m_dlg_chart_options.Closed += delegate { m_dlg_chart_options = null; };
				m_dlg_chart_options.Show();
				m_dlg_chart_options.SetLocation(pt.X - m_dlg_chart_options.DesiredSize.Width, pt.Y + 20).OnScreen();
			}
			else
			{
				m_dlg_chart_options.Close();
			}
		}
		private static ChartOptionsUI m_dlg_chart_options;

		/// <summary>Pin window support</summary>
		private PinData PinState { get; }

		/// <summary>The size of trade labels</summary>
		public double TradeLabelSize
		{
			get => SettingsData.Settings.Chart.ConfettiLabelSize;
			set => SettingsData.Settings.Chart.ConfettiLabelSize = value;
		}

		/// <summary>The transparency of trade label backgrounds</summary>
		public double TradeLabelTransparency
		{
			get => SettingsData.Settings.Chart.ConfettiLabelTransparency * 100.0;
			set => SettingsData.Settings.Chart.ConfettiLabelTransparency = value / 100.0;
		}

		/// <summary>Show the descriptions next to the trade markers</summary>
		public bool ShowTradeDescriptions
		{
			get => SettingsData.Settings.Chart.ConfettiDescriptionsVisible;
			set => SettingsData.Settings.Chart.ConfettiDescriptionsVisible = value;
		}

		/// <summary>Show the labels to the left of the trade markers</summary>
		public bool LabelsToTheLeft
		{
			get => SettingsData.Settings.Chart.ConfettiLabelsToTheLeft;
			set => SettingsData.Settings.Chart.ConfettiLabelsToTheLeft = value;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
