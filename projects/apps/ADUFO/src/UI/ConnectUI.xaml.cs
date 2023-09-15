using System.ComponentModel;
using System.Windows;
using Rylogic.Gui.WPF;

namespace ADUFO;

public partial class ConnectUI : Window, INotifyPropertyChanged
{
	public ConnectUI(Window owner)
	{
		Owner = owner;

		Accept = Command.Create(this, AcceptInternal);

		InitializeComponent();
		DataContext = this;
	}

	/// <summary>The ADO organisation</summary>
	public string Organization
	{
		get => m_organization;
		set
		{
			if (m_organization == value) return;
			m_organization = value;
			NotifyPropertyChanged(nameof(Organization));
			NotifyPropertyChanged(nameof(AdoUrl));
		}
	}
	private string m_organization = string.Empty;

	/// <summary>The ADO project</summary>
	public string Project
	{
		get => m_project;
		set
		{
			if (m_project == value) return;
			m_project = value;
			NotifyPropertyChanged(nameof(Project));
			NotifyPropertyChanged(nameof(AdoUrl));
		}
	}
	private string m_project = string.Empty;

	/// <summary>The access token for ADO</summary>
	public string PersonalAccessToken
	{
		get => m_personal_access_token;
		set
		{
			if (m_personal_access_token == value) return;
			m_personal_access_token = value;
			NotifyPropertyChanged(nameof(PersonalAccessToken));
		}
	}
	private string m_personal_access_token = string.Empty;

	/// <summary>The ADO URL</summary>
	public string AdoUrl => $"https://dev.azure.com/{Organization}/{Project}";

	/// <summary>Accept action</summary>
	public Command Accept { get; }
	private void AcceptInternal()
	{
		DialogResult = true;
		Close();
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}
