using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace UFADO;

public partial class MainWindow : Window, INotifyPropertyChanged
{
	public MainWindow(Settings settings, Model model)
	{
		Shutdown = new CancellationTokenSource();
		Settings = settings;
		Model = model;
		Diagram = new Diagram { };
		Diagram.Chart.Options = new ChartControl.OptionsData
		{
			NavigationMode = ChartControl.ENavMode.Scene3D,
			Orthographic = false,
			BackgroundColour = 0xFF272822,
			AreaSelectRequiresShiftKey = true,
			AllowSelection = false,
			AllowElementDragging = false,
			ShowAxes = false,
			ShowGridLines = false,
			LockAspect = 1.0,
		};

		// Commands
		ShowAdoQueryUI = Command.Create(this, ShowAdoQueryUIInternal);
		ShowAdoWorkItemUI = Command.Create(this, ShowAdoWorkItemUIInternal);
		ShowSettingsUI = Command.Create(this, ShowSettingsUIInternal);
		ShowConnectionUI = Command.Create(this, ShowConnectionUIInternal);
		ShowSlidersUI = Command.Create(this, ShowSlidersUIInternal);
		ToggleConnection = Command.Create(this, ToggleConnectionInternal);
		Refresh = Command.Create(this, RefreshInternal, RefreshAvailable);
		ToggleScattering = Command.Create(this, ToggleScatteringInternal);
		ShowAboutUI = Command.Create(this, ShowAboutUIInternal);
		Exit = Command.Create(this, ExitInternal);

		InitializeComponent();
		InitDockContainer();
		DataContext = this;
		Loaded += (o, s) =>
		{
			ToggleConnectionInternal();
			RefreshInternal();
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
		get;
		set
		{
			if (field == value) return;
			if (field != null)
			{
				field.PropertyChanged -= HandlePropertyChanged;
			}
			field = value;
			if (field != null)
			{
				field.PropertyChanged += HandlePropertyChanged;
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
	} = null!;

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
			m_scatterer = value ? new Scatterer(Diagram.Chart, Settings.Sliders) : null;
			NotifyPropertyChanged(nameof(IsScattering));
			Diagram.Chart.Invalidate();
		}
	}
	private Scatterer? m_scatterer;

	/// <summary>The camera location</summary>
	public string CameraDescription => $"Camera: {Diagram.Chart.Camera.Description}";

	/// <summary>Add to the dock container</summary>
	private void InitDockContainer()
	{
		m_dc.Options.AlwaysShowTabs = true;
		m_dc.Options.ShowTitleBars = false;

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

	/// <summary>Show the query UI</summary>
	public Command ShowAdoQueryUI { get; }
	private void ShowAdoQueryUIInternal()
	{
		if (Model.Ado is not AdoInterface ado)
			return;

		if (m_ui_ado_query == null)
		{
			m_ui_ado_query = new AdoQueryUI(this, Settings, ado);
			m_ui_ado_query.Closed += delegate { m_ui_ado_query = null; };
			m_ui_ado_query.Show();
		}
		m_ui_ado_query.Focus();
	}
	private AdoQueryUI? m_ui_ado_query;

	/// <summary>Show the query UI</summary>
	public Command ShowAdoWorkItemUI { get; }
	private void ShowAdoWorkItemUIInternal()
	{
		if (Model.Ado is not AdoInterface ado)
			return;

		if (m_ui_ado_work_item == null)
		{
			m_ui_ado_work_item = new AdoWorkItemUI(this, Settings, ado);
			m_ui_ado_work_item.Closed += delegate { m_ui_ado_work_item = null; };
			m_ui_ado_work_item.Show();
		}
		m_ui_ado_work_item.Focus();
	}
	private AdoWorkItemUI? m_ui_ado_work_item;

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
				Project = Settings.Project,
				PersonalAccessToken = Settings.PersonalAccessToken,
			};
			dlg.ShowDialog();
			Settings.Organization = dlg.Organization;
			Settings.Project = dlg.Project;
			Settings.PersonalAccessToken = dlg.PersonalAccessToken;
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Failed to update connection settings.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
	}

	/// <summary></summary>
	public Command ShowSettingsUI { get; }
	private void ShowSettingsUIInternal()
	{
		// TODO: Show app settings
	}

	/// <summary>Display the sliders dialog</summary>
	public Command ShowSlidersUI { get; }
	private void ShowSlidersUIInternal()
	{
		if (m_ui_sliders == null)
		{
			m_ui_sliders = new SlidersUI(this, Settings.Sliders);
			m_ui_sliders.Closed += delegate { m_ui_sliders = null; };
			m_ui_sliders.Show();
		}
		m_ui_sliders.Focus();
	}
	private SlidersUI? m_ui_sliders;

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
				Model.Ado = new AdoInterface(Settings, Shutdown.Token);
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
		}
		catch (Exception ex)
		{
			MsgBox.Show(this, $"Error while refreshing data.\r\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
		}
		finally
		{
			Refresh.NotifyCanExecuteChanged();
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
