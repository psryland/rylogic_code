using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Services.Organization.Client;
using Rylogic.Gui.WPF;

namespace ADUFO;

public partial class MainWindow : Window
{
	public MainWindow(Settings settings, Model model)
	{
		Settings = settings;
		Model = model;

		InitializeComponent();
		Loaded += (o,s) =>
		{
			if (Settings.Organization.Length == 0 || Settings.PersonalAccessToken.Length == 0)
				PromptConnectionSettings();
		};
	}

	/// <summary>Application settings</summary>
	private Settings Settings { get; }

	/// <summary>Application logic</summary>
	private Model Model { get; }

	/// <summary>Update the connection settings</summary>
	private void PromptConnectionSettings()
	{
		// Prompt for the ADO connection and PAT if not in the settings
		var dlg = new ConnectUI(this)
		{
			Organization = Settings.Organization,
			PersonalAccessToken = Settings.PersonalAccessToken,
		};
		if (dlg.ShowDialog() != true)
		{
			// Terminate the application
			Close();
			return;
		}
		Settings.Organization = dlg.Organization;
		Settings.PersonalAccessToken = dlg.PersonalAccessToken;
	}

}
