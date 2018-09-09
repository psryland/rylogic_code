using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using RyLogViewer.Options;

namespace RyLogViewer
{
	/// <summary>Interaction logic for OptionsUI.xaml</summary>
	public partial class OptionsUI : Window
	{
		private readonly Main m_main;
		private readonly Settings m_settings;
		private readonly IReport m_report;

		static OptionsUI()
		{
			SelectedPageProperty = Gui_.DPRegister<OptionsUI>(nameof(SelectedPage), def: EOptionsPage.General);
		}
		public OptionsUI(Main main, Settings settings, IReport report)
		{
			InitializeComponent();
			m_main = main;
			m_settings = settings;
			m_report = report;

			// Bind data
			m_root.DataContext = this;
			m_grp_settingsfile.DataContext = settings;
			m_grp_startup.DataContext = settings.General;
			m_grp_logdata.DataContext = settings.LogData;
			m_grp_format.DataContext = settings.Format;
			m_grp_rowprops.DataContext = settings.Format;
			m_grp_colours.DataContext = settings.Format;
			m_cb_formatter.ItemsSource = main.AvailableFormatters;
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;
			Visibility = Visibility.Hidden;
		}

		/// <summary>The selected tab page in the options dialog</summary>
		public EOptionsPage SelectedPage
		{
			get { return (EOptionsPage)GetValue(SelectedPageProperty); }
			set { SetValue(SelectedPageProperty, value); }
		}
		public static readonly DependencyProperty SelectedPageProperty;

		/// <summary>Handle the Reset to Defaults button</summary>
		private void HandleResetToDefaults(object sender, RoutedEventArgs e)
		{
			// Confirm first...
			var res = MsgBox.Show(this,
				"This will reset all current settings to their default values\r\n\r\nContinue?",
				"Confirm Reset Settings",
				MsgBox.EButtons.YesNo,
				MsgBox.EIcon.Question);
			if (res != true)
				return;

			// Reset the settings
			try
			{
				m_settings.Reset();
			}
			catch (Exception ex)
			{
				m_report.ErrorPopup("Failed to reset settings to their default values.", ex);
			}
		}

		/// <summary>Handle the Load button</summary>
		private void HandleLoad(object sender, RoutedEventArgs e)
		{
			// Prompt for a settings file
			var fd = new OpenFileDialog
			{
				Title = "Choose a Settings file to load",
				Filter = Constants.SettingsFileFilter,
				CheckFileExists = true,
				Multiselect = false,
				InitialDirectory = Path_.Directory(m_settings.Filepath)
			};
			if (fd.ShowDialog(this) != true) return;
			var filepath = fd.FileName;

			try
			{
				m_settings.Filepath = filepath;
				m_settings.Reload();
			}
			catch (Exception ex)
			{
				m_report.ErrorPopup($"Failed to open settings file {filepath} due to an error.", ex);
			}
		}

		/// <summary>Handle the Reset to Defaults button</summary>
		private void HandleSaveAs(object sender, RoutedEventArgs e)
		{
			// Prompt for where to save the settings
			var fd = new SaveFileDialog
			{
				Title = "Save current Settings",
				Filter = Constants.SettingsFileFilter,
				InitialDirectory = Path_.Directory(m_settings.Filepath)
			};
			if (fd.ShowDialog(this) != true) return;
			var filepath = fd.FileName;

			try
			{
				m_settings.Filepath = filepath;
				m_settings.Save();
			}
			catch (Exception ex)
			{
				m_report.ErrorPopup($"Failed to save settings file {filepath} due to an error.", ex);
			}
		}

		/// <summary>Handle the mouse event for editing the selection colour</summary>
		private void HandleEditSelectionColor(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			MsgBox.Show(this, "todo");
		}
	}
}
