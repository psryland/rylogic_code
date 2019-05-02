using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EDTradeAdvisor.DomainObjects;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace EDTradeAdvisor.UI
{
	/// <summary>Interaction logic for MainWindow.xaml</summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			Advisor = new Advisor(x => Dispatcher.BeginInvoke(x));
			m_log.LogEntryPattern = LogEntryPatternRegex;
			m_log.LogFilepath = Advisor.Log.LogCB is LogToFile l2f ? l2f.Filepath : null;
			Settings.Instance.SettingChange += HandleSettingChange;
			StatusStack.ValueChanged += HandleStatusMsgChanged;

			// Collections
			TradeRoutes = new ListCollectionView(Advisor.TradeRoutes);
			OriginStarSystemsShortList = CollectionViewSource.GetDefaultView(new List<StarSystem>());
			OriginStations = CollectionViewSource.GetDefaultView(new List<Station>());
			DestStarSystemsShortList = CollectionViewSource.GetDefaultView(new List<StarSystem>());
			DestStations = CollectionViewSource.GetDefaultView(new List<Station>());

			// Commands
			ShowSettings = Command.Create(this, ShowSettingsInternal);
			RebuildCache = Command.Create(this, RebuildCacheInternal);
			Exit = Command.Create(this, Close);

			// UI Binding
			DataContext = this;

			// Start the advisor running
			Advisor.Run = true;
		}
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			// On first time startup, display the settings before creating the data directory
			if (!Path_.DirExists(Settings.Instance.DataPath))
			{
				ShowSettings.Execute();

				// Ensure the data directory exists
				Path_.CreateDirs(Settings.Instance.DataPath);
			}
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			if (!Advisor.Shutdown.IsCancellationRequested)
			{
				Advisor.Shutdown.Cancel();
				e.Cancel = true;
				Dispatcher.BeginInvoke(new Action(Close));
			}
			Settings.Instance.SettingChange -= HandleSettingChange;
			StatusStack.ValueChanged -= HandleStatusMsgChanged;
			base.OnClosing(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			OriginStarSystemsShortList = null;
			DestStarSystemsShortList = null;
			OriginStations = null;
			DestStations = null;
			Advisor.Dispose();
		}

		/// <summary>The app logic instance</summary>
		private Advisor Advisor
		{
			get { return m_advisor; }
			set
			{
				if (m_advisor == value) return;
				if (m_advisor != null)
				{
					m_advisor.RunChanged -= HandleRunChanged;
					Util.Dispose(ref m_advisor);
				}
				m_advisor = value;
				if (m_advisor != null)
				{
					m_advisor.RunChanged += HandleRunChanged;
				}

				// Handlers
				void HandleRunChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
				}
			}
		}
		private Advisor m_advisor;

		/// <summary>Show the settings dialog</summary>
		public Command ShowSettings { get; }
		private void ShowSettingsInternal()
		{
			new Dialogs.SettingsUI(this).ShowDialog();
		}

		/// <summary>Flatten and rebuild the cached data</summary>
		public Command RebuildCache { get; }
		private void RebuildCacheInternal()
		{
			Advisor.RebuildStaticData = true;
		}

		/// <summary>Exit the app</summary>
		public Command Exit { get; }

		/// <summary>Use output from the ED Market connector to monitor current position</summary>
		public bool UseEDMC
		{
			get => Advisor.UseEDMC;
			set => Advisor.UseEDMC = value;
		}

		/// <summary>True while the advisor is running</summary>
		public bool Active
		{
			get => Advisor.Run;
			set => Advisor.Run = value;
		}

		/// <summary>Current app status</summary>
		public string StatusMsg => StatusStack.Value;

		/// <summary>The found trade routes</summary>
		public ICollectionView TradeRoutes { get; }

		/// <summary>The short-list of star systems selected by the origin location combo box</summary>
		public ICollectionView OriginStarSystemsShortList
		{
			get { return m_star_systems_origin; }
			private set
			{
				if (m_star_systems_origin == value) return;
				if (m_star_systems_origin != null)
				{
					m_star_systems_origin.CurrentChanged -= HandleCurrentSystemChanged;
				}
				m_star_systems_origin = value;
				if (m_star_systems_origin != null)
				{
					m_star_systems_origin.CurrentChanged += HandleCurrentSystemChanged;

					// Initialise from the settings
					if (Settings.Instance.OriginSystemID != null)
						((List<StarSystem>)m_star_systems_origin.SourceCollection).Add(Advisor.DB.GetStarSystem(Settings.Instance.OriginSystemID.Value));
				}

				// Handlers
				void HandleCurrentSystemChanged(object sender, EventArgs e)
				{
					// Save the current origin system id
					var current = (StarSystem)OriginStarSystemsShortList.CurrentItem;
					Settings.Instance.OriginSystemID = current?.ID;
				}
			}
		}
		private ICollectionView m_star_systems_origin;

		/// <summary>The short-list of star systems selected by the destination location combo box</summary>
		public ICollectionView DestStarSystemsShortList
		{
			get { return m_star_systems_dest; }
			private set
			{
				if (m_star_systems_dest == value) return;
				if (m_star_systems_dest != null)
				{
					m_star_systems_dest.CurrentChanged -= HandleCurrentSystemChanged;
				}
				m_star_systems_dest = value;
				if (m_star_systems_dest != null)
				{
					m_star_systems_dest.CurrentChanged += HandleCurrentSystemChanged;

					// Initialise from the settings
					if (Settings.Instance.DestSystemID != null)
						((List<StarSystem>)m_star_systems_dest.SourceCollection).Add(Advisor.DB.GetStarSystem(Settings.Instance.DestSystemID.Value));
				}

				// Handlers
				void HandleCurrentSystemChanged(object sender, EventArgs e)
				{
					// Save the current destination system id
					var current = (StarSystem)DestStarSystemsShortList.CurrentItem;
					Settings.Instance.DestSystemID = current?.ID;
				}
			}
		}
		private ICollectionView m_star_systems_dest;

		/// <summary>The stations associated with the selection origin system</summary>
		public ICollectionView OriginStations
		{
			get { return m_stations_origin; }
			private set
			{
				if (m_stations_origin == value) return;
				if (m_stations_origin != null)
				{
					m_stations_origin.CurrentChanged -= HandleCurrentStationChanged;
				}
				m_stations_origin = value;
				if (m_stations_origin != null)
				{
					m_stations_origin.CurrentChanged += HandleCurrentStationChanged;
				}

				// Handlers
				void HandleCurrentStationChanged(object sender, EventArgs e)
				{
					// Save the current origin station id
					var current = (Station)OriginStations.CurrentItem;
					Settings.Instance.OriginStationID = current?.ID;
				}
			}
		}
		private ICollectionView m_stations_origin;

		/// <summary>The stations associated with the selection destination system</summary>
		public ICollectionView DestStations
		{
			get { return m_stations_dest; }
			private set
			{
				if (m_stations_dest == value) return;
				if (m_stations_dest != null)
				{
					m_stations_dest.CurrentChanged -= HandleCurrentStationChanged;
				}
				m_stations_dest = value;
				if (m_stations_dest != null)
				{
					m_stations_dest.CurrentChanged += HandleCurrentStationChanged;
				}

				// Handlers
				void HandleCurrentStationChanged(object sender, EventArgs e)
				{
					// Save the current origin station id
					var current = (Station)OriginStations.CurrentItem;
					Settings.Instance.OriginStationID = current?.ID;
				}
			}
		}
		private ICollectionView m_stations_dest;

		/// <summary>True if any destination will do</summary>
		public bool AnyDestination
		{
			get => Settings.Instance.AnyDestination;
			set => Settings.Instance.AnyDestination = value;
		}

		/// <summary>True if any destination will do</summary>
		public long? MaxTradeRouteDistance
		{
			get => Settings.Instance.MaxTradeRouteDistance;
			set => Settings.Instance.MaxTradeRouteDistance = value;
		}

		/// <summary>Cargo capacity</summary>
		public int CargoCapacity
		{
			get => Settings.Instance.CargoCapacity;
			set => Settings.Instance.CargoCapacity = value;
		}

		/// <summary>Credits available</summary>
		public long AvailableCredits
		{
			get => Settings.Instance.AvailableCredits;
			set => Settings.Instance.AvailableCredits = value;
		}

		/// <summary>Maximum distance per jump</summary>
		public long MaxJumpRange
		{
			get => Settings.Instance.MaxJumpRange;
			set => Settings.Instance.MaxJumpRange = value;
		}

		/// <summary>Maximum distance to a station</summary>
		public long? MaxStationDistance
		{
			get => Settings.Instance.MaxStationDistance;
			set => Settings.Instance.MaxStationDistance = value;
		}

		/// <summary>The pad size needed to land</summary>
		public ELandingPadSize RequiredPadSize
		{
			get => Settings.Instance.RequiredPadSize;
			set => Settings.Instance.RequiredPadSize = value;
		}

		/// <summary>Exclude planet bases</summary>
		public bool IgnorePlanetBases
		{
			get => Settings.Instance.IgnorePlanetBases;
			set => Settings.Instance.IgnorePlanetBases = value;
		}

		/// <summary>Exclude systems that require a permit</summary>
		public bool IgnorePermitSystems
		{
			get => Settings.Instance.IgnorePermitSystems;
			set => Settings.Instance.IgnorePermitSystems = value;
		}

		/// <summary>Support sub-string matching for system names</summary>
		private void HandleStarSystemTextChanged(object sender, TextChangedEventArgs e)
		{
			var cb = (ComboBox)sender;
			var view = (ICollectionView)cb.ItemsSource;
			var list = (List<StarSystem>)view.SourceCollection;
			using (cb.SelectionScope())
			{
				// Repopulate the items source
				list.Clear();
				if (cb.Text.Length > 0 && cb.EditableTextBox().IsFocused)
				{
					cb.IsDropDownOpen = true;
					list.AddRange(Advisor.DB.EnumStarSystems(match_name: cb.Text, max_count: 10));
					view.Refresh();
				}
			}
		}

		/// <summary>Handle setting changes</summary>
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			if (e.Before) return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.Key));
			switch (e.Key)
			{
			case nameof(Settings.OriginSystemID):
				{
					// Populate the origin stations collection
					var list = (List<Station>)OriginStations.SourceCollection;

					list.Clear();
					if (Settings.Instance.OriginSystemID != null)
						list.AddRange(Advisor.DB.EnumStations(system_id: Settings.Instance.OriginSystemID.Value));

					// Preserve the station if possible
					var idx = list.IndexOf(x => x.ID == Settings.Instance.OriginStationID);
					OriginStations.MoveCurrentToPosition(idx != -1 ? idx : 0);
					OriginStations.Refresh();
					break;
				}
			case nameof(Settings.DestSystemID):
				{
					// Populate the origin stations collection
					var list = (List<Station>)DestStations.SourceCollection;

					list.Clear();
					if (Settings.Instance.DestSystemID != null)
						list.AddRange(Advisor.DB.EnumStations(system_id: Settings.Instance.DestSystemID.Value));

					// Preserve the station if possible
					var idx = list.IndexOf(x => x.ID == Settings.Instance.DestStationID);
					DestStations.MoveCurrentToPosition(idx != -1 ? idx : 0);
					DestStations.Refresh();
					break;
				}
			}
		}

		/// <summary>Handle status changes</summary>
		private void HandleStatusMsgChanged(object sender, EventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusMsg)));
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Log line pattern</summary>
		private static readonly Regex LogEntryPatternRegex = new Regex(
			@"^(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)",
			RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
}
