using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Utility;

namespace ADUFO;

public partial class MainWindow : Window, INotifyPropertyChanged
{
	public MainWindow(Settings settings, Model model)
	{
		Shutdown = new CancellationTokenSource();
		Settings = settings;
		Model = model;
		Diagram = new Diagram();

		// Commands
		DoTest = Command.Create(this, DoTestInternal);
		ShowConnectionUI = Command.Create(this, ShowConnectionUIInternal);
		ToggleConnection = Command.Create(this, ToggleConnectionInternal);
		Refresh = Command.Create(this, RefreshInternal, RefreshAvailable);
		ToggleScattering = Command.Create(this, ToggleScatteringInternal);
		ShowAboutUI = Command.Create(this, ShowAboutUIInternal);
		Exit = Command.Create(this, ExitInternal);

		InitializeComponent();
		InitDockContainer();
		DataContext = this;
		Loaded += (o,s) =>
		{
			ToggleConnectionInternal();
		};
	}
	protected override void OnClosing(CancelEventArgs e)
	{
		if (!Shutdown.IsCancellationRequested)
		{
			Shutdown.Cancel();
			Dispatcher.BeginInvoke(Close);
			return;
		}
		base.OnClosing(e);
	}
	protected override void OnClosed(EventArgs e)
	{
		Model = null!;
		Util.DisposeRange(m_dc.AllContent.OfType<IDisposable>());
		base.OnClosed(e);
	}
	
	/// <summary>Async app shutdown</summary>
	private CancellationTokenSource Shutdown { get; }

	/// <summary>Application settings</summary>
	private Settings Settings { get; }

	/// <summary>Application logic</summary>
	private Model Model
	{
		get => m_model;
		set
		{
			if (m_model == value) return;
			if (m_model != null)
			{
				m_model.PropertyChanged -= HandlePropertyChanged;
			}
			m_model = value;
			if (m_model != null)
			{
				m_model.PropertyChanged += HandlePropertyChanged;
			}

			// Handler
			void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(Model.Ado):
					{
						NotifyPropertyChanged(nameof(IsConnected));
						break;
					}
				}
			}
		}
	}
	private Model m_model = null!;

	/// <summary>The diagram UI element</summary>
	private Diagram Diagram { get; }

	/// <summary>True if there is a connection to ADO</summary>
	public bool IsConnected => Model.Ado != null;

	/// <summary>Enable scattering of diagram items</summary>
	public bool IsScattering
	{
		get => m_scatterer != null;
		set
		{
			if (IsScattering == value) return;
			Util.Dispose(ref m_scatterer);
			m_scatterer = value ? new NodeScatterer(Diagram.Chart, (Diagram_.Options)Diagram.Chart.Options) : null;
			NotifyPropertyChanged(nameof(IsScattering));
			Diagram.Chart.Invalidate();
		}
	}
	private NodeScatterer? m_scatterer;

	/// <summary>The camera location</summary>
	public string CameraDescription => $"Camera: {Diagram.Chart.Camera.Description}";

	/// <summary>Add to the dock container</summary>
	private void InitDockContainer()
	{
		m_dc.Options.AlwaysShowTabs = true;
	
		// Add the diagram
		m_dc.Add(Diagram, EDockSite.Centre);
		m_dc.ActiveContent = Diagram.DockControl;

		// Restore the layout
		m_dc.LoadLayout(Settings.UILayout);
		m_dc.LayoutChanged += SaveLayout;
		void SaveLayout(object? sender, EventArgs args)
		{
			Settings.UILayout = m_dc.SaveLayout();
		}

		// Add the menu for dock container windows
		m_menu.Items.Insert(m_menu.Items.Count - 1, m_dc.WindowsMenu());
	}

	/// <summary></summary>
	public Command DoTest { get; }
	private async void DoTestInternal()
	{
		if (Model.Ado != null)
			await Model.Ado.WorkStreams();
	}

	/// <summary></summary>
	public Command ShowConnectionUI { get; }
	private void ShowConnectionUIInternal()
	{
		try
		{
			// Prompt for the ADO connection and PAT if not in the settings
			var dlg = new ConnectUI(this)
			{
				Organization = Settings.Organization,
				PersonalAccessToken = Settings.PersonalAccessToken,
			};
			dlg.ShowDialog();
			Settings.Organization = dlg.Organization;
			Settings.PersonalAccessToken = dlg.PersonalAccessToken;
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Failed to update connection settings.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}

	/// <summary>Connect to ADO</summary>
	public Command ToggleConnection { get; }
	private void ToggleConnectionInternal()
	{
		try
		{
			if (!IsConnected)
			{
				if (Settings.Organization.Length == 0 || Settings.PersonalAccessToken.Length == 0)
					ShowConnectionUI.Execute();
				if (Settings.Organization.Length == 0 || Settings.PersonalAccessToken.Length == 0)
					return;
				
				// Connect (if we have connection settings)
				Model.Ado = new AdoInterface(Settings.Organization, Settings.Project, Settings.PersonalAccessToken, Shutdown.Token);
			}
			else
			{
				Model.Ado = null;
			}
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Failed to connect.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}

	/// <summary>Refresh the ADO work items</summary>
	public Command Refresh { get; }
	private async void RefreshInternal()
	{
		try
		{
			using var refreshing = Scope.Create(() => m_refreshing = true, () => m_refreshing = false);
			Refresh.NotifyCanExecuteChanged();
			await Model.Refresh(Diagram.Chart);
			Refresh.NotifyCanExecuteChanged();
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Error while refreshing data.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}
	private bool RefreshAvailable() => !m_refreshing;
	private bool m_refreshing;

	/// <summary>Enable/Disable scattering</summary>
	public Command ToggleScattering { get; }
	private void ToggleScatteringInternal()
	{
		try
		{
			IsScattering = !IsScattering;
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Error while scattering.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}

	/// <summary>Show the about dialog</summary>
	public Command ShowAboutUI { get; }
	private void ShowAboutUIInternal()
	{
		// TODO
	}

	/// <summary>Shutdown the application</summary>
	public Command Exit { get; }
	private void ExitInternal()
	{
		Close();
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

}
