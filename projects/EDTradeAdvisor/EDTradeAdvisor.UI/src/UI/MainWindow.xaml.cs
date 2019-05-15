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
			StatusStack.ValueChanged += HandleStatusMsgChanged;
			Settings.Instance.SettingChange += HandleSettingChange;

			// Setup UI
			InitializeComponent();
			m_log.LogEntryPattern = LogEntryPatternRegex;
			m_log.LogFilepath = Log.Filepath;

			// Collections
			OriginStarSystemsShortList = CollectionViewSource.GetDefaultView(new List<StarSystem>());
			OriginStations = CollectionViewSource.GetDefaultView(new List<Station>());
			DestStarSystemsShortList = CollectionViewSource.GetDefaultView(new List<StarSystem>());
			DestStations = CollectionViewSource.GetDefaultView(new List<Station>());
			Advisor = new Advisor(x => Dispatcher.BeginInvoke(x));
			TradeRoutes = new ListCollectionView(Advisor.TradeRoutes);

			// Commands
			ShowSettings = Command.Create(this, ShowSettingsInternal);
			RebuildCache = Command.Create(this, RebuildCacheInternal);
			SetAsOrigin = Command.Create(this, SetAsOriginInternal);
			SetAsDestination = Command.Create(this, SetAsDestinationInternal);
			SwapOriDest = Command.Create(this, SwapOriDestInternal);
			Exit = Command.Create(this, Close);

			// UI Binding
			DataContext = this;

			// Start the advisor running
			Advisor.Run = true;
		}
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);

			// On first time startup, display the settings before creating the data directory
			if (!Path_.DirExists(Settings.Instance.DataPath))
			{
				ShowSettings.Execute();

				// Ensure the data directory exists
				Path_.CreateDirs(Settings.Instance.DataPath);
			}

			// Apply settings
			Settings.Instance.NotifySettingChanged(nameof(Settings.Origin));
			Settings.Instance.NotifySettingChanged(nameof(Settings.Destination));
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
					m_advisor.TradeRoutesChanged -= HandleTradeRoutesChanged;
					m_advisor.RunChanged -= HandleRunChanged;
					Util.Dispose(ref m_advisor);
				}
				m_advisor = value;
				if (m_advisor != null)
				{
					m_advisor.RunChanged += HandleRunChanged;
					m_advisor.TradeRoutesChanged += HandleTradeRoutesChanged;
				}

				// Handlers
				void HandleRunChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
				}
				void HandleTradeRoutesChanged(object sender, EventArgs e)
				{
					TradeRoutes.Refresh();
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
			Advisor.Run = true;
		}

		/// <summary>Set the current trade route as the new origin</summary>
		public Command SetAsOrigin { get; }
		private void SetAsOriginInternal()
		{
			if (TradeRoutes.CurrentItem == null) return;
			var route = (TradeRoute)TradeRoutes.CurrentItem;
			UseCurrentLocation = false;
			AnyDestination = true;
			Settings.Instance.Origin = route.Destination;
		}

		/// <summary>Set the current trade route as the destination</summary>
		public Command SetAsDestination { get; }
		private void SetAsDestinationInternal()
		{
			if (TradeRoutes.CurrentItem == null) return;
			var route = (TradeRoute)TradeRoutes.CurrentItem;
			AnyDestination = false;
			Settings.Instance.Destination = route.Destination;
		}

		/// <summary>Swap the current origin and destination values</summary>
		public Command SwapOriDest { get; }
		private void SwapOriDestInternal()
		{
			var tmp = Settings.Instance.Origin;
			Settings.Instance.Origin = Settings.Instance.Destination;
			Settings.Instance.Destination = tmp;
		}

		/// <summary>Exit the app</summary>
		public Command Exit { get; }

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
				}

				// Handlers
				void HandleCurrentSystemChanged(object sender, EventArgs e)
				{
					// Save the current origin system id
					var current = (StarSystem)OriginStarSystemsShortList.CurrentItem;
					Settings.Instance.Origin = new LocationID(current?.ID, Settings.Instance.Origin.StationID);
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
				}

				// Handlers
				void HandleCurrentSystemChanged(object sender, EventArgs e)
				{
					// Save the current destination system id
					var current = (StarSystem)DestStarSystemsShortList.CurrentItem;
					Settings.Instance.Destination = new LocationID(current?.ID, Settings.Instance.Destination.StationID);
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
					Settings.Instance.Origin = new LocationID(Settings.Instance.Origin.StarSystemID, current?.ID);
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
					// Save the current destination station id
					var current = (Station)DestStations.CurrentItem;
					Settings.Instance.Destination = new LocationID(Settings.Instance.Destination.StarSystemID, current?.ID);
				}
			}
		}
		private ICollectionView m_stations_dest;

		/// <summary>Read the current location from the journal file</summary>
		public bool UseCurrentLocation
		{
			get => Settings.Instance.UseCurrentLocation;
			set => Settings.Instance.UseCurrentLocation = value;
		}

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
		public double MaxJumpRange
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
		private void UpdateStarSystemAutoComplete(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.Text.Length > 0)
			{
				var view = (ICollectionView)cb.ItemsSource;
				var list = (List<StarSystem>)view.SourceCollection;
				var match = cb.Text.Replace('*', '%');
				list.Clear();
				list.AddRange(Advisor.Src.EnumStarSystems(match_name: match, max_count: 100));
				view.Refresh();
			}
		}

		/// <summary>Support sub-string matching for station names</summary>
		private void UpdateStationAutoComplete(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			var view = (ICollectionView)cb.ItemsSource;
			var system_id =
				view == OriginStations ? Settings.Instance.Origin.StarSystemID :
				view == DestStations ? Settings.Instance.Destination.StarSystemID :
				null;

			if (system_id != null)
			{
				var list = (List<Station>)view.SourceCollection;
				var match = cb.Text.Replace('*', '%');

				list.Clear();
				list.AddRange(Advisor.Src.EnumStations(system_id: system_id.Value, match_name: match));
				view.Refresh();
			}
			else if (cb.Text.Length > 0)
			{
				// If there is text in the station combo but no system yet, populate the
				// system short list with systems containing matching stations
				var systems_view =
					view == OriginStations ? OriginStarSystemsShortList :
					view == DestStations ? DestStarSystemsShortList :
					null;

				if (systems_view != null)
				{
					var list = (List<StarSystem>)systems_view.SourceCollection;
					var match = cb.Text.Replace('*', '%');

					list.Clear();
					list.AddRange(Advisor.Src.EnumStarSystemsContainingStation(
						match_station_name: match, max_count:100,
						ignore_permitted: Settings.Instance.IgnorePermitSystems));
					systems_view.Refresh();
				}

				// Populate the stations list with matching names
				{
					var list = (List<Station>)view.SourceCollection;
					var match = cb.Text.Replace('*', '%');
					list.Clear();
					list.AddRange(Advisor.Src.EnumStations(
						match_name: match, max_count: 100,
						max_station_distance:Settings.Instance.MaxStationDistance,
						required_pad_size:Settings.Instance.RequiredPadSize,
						facilities_incl:EFacilities.Market|EFacilities.Docking,
						ignore_planetary:Settings.Instance.IgnorePlanetBases));
					view.Refresh();
				}
			}
		}

		/// <summary>Handle setting changes</summary>
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			// Notes:
			//  - Don't change any Settings values in here. That should be in the model
			//    code (Advisor). Only update the collections used for binding.

			if (e.Before) return;
			switch (e.Key)
			{
			case nameof(Settings.Origin):
				{
					// Ensure the star systems short list reflects the currently selected system
					var systems = (List<StarSystem>)OriginStarSystemsShortList.SourceCollection;
					var idx = Settings.Instance.Origin.StarSystemID != null ? systems.IndexOf(x => x.ID == Settings.Instance.Origin.StarSystemID.Value) : -1;
					if (idx == -1 && Settings.Instance.Origin.StarSystemID != null)
					{
						// If the current system is not in the short list, repopulate the short list
						systems.Clear();
						systems.Add(Advisor.Src.GetStarSystem(Settings.Instance.Origin.StarSystemID.Value).Result);
						idx = 0;
					}
					OriginStarSystemsShortList.MoveCurrentToPosition(idx);
					OriginStarSystemsShortList.Refresh();

					// Populate the origin stations collection
					var stations = (List<Station>)OriginStations.SourceCollection;
					if (Settings.Instance.Origin.StarSystemID != null)
						stations.Assign(Advisor.Src.EnumStations(system_id: Settings.Instance.Origin.StarSystemID.Value));
					else
						stations.Clear();

					// Preserve the station if possible
					idx = Settings.Instance.Origin.StationID != null ? stations.IndexOf(x => x.ID == Settings.Instance.Origin.StationID) : -1;
					OriginStations.MoveCurrentToPosition(idx != -1 ? idx : 0);
					OriginStations.Refresh();
					break;
				}
			case nameof(Settings.Destination):
				{
					// Ensure the star systems short list reflects the currently selected system
					var systems = (List<StarSystem>)DestStarSystemsShortList.SourceCollection;
					var idx = Settings.Instance.Destination.StarSystemID != null ? systems.IndexOf(x => x.ID == Settings.Instance.Destination.StarSystemID.Value) : -1;
					if (idx == -1 && Settings.Instance.Destination.StarSystemID != null)
					{
						// If the current system is not in the short list, repopulate the short list
						systems.Clear();
						systems.Add(Advisor.Src.GetStarSystem(Settings.Instance.Destination.StarSystemID.Value).Result);
						idx = 0;
					}
					DestStarSystemsShortList.MoveCurrentToPosition(idx);
					DestStarSystemsShortList.Refresh();
				
					// Populate the destination stations collection
					var stations = (List<Station>)DestStations.SourceCollection;
					if (Settings.Instance.Destination.StarSystemID != null)
						stations.Assign(Advisor.Src.EnumStations(system_id: Settings.Instance.Destination.StarSystemID.Value));
					else
						stations.Clear();

					// Preserve the station if possible
					idx = Settings.Instance.Destination.StationID != null ? stations.IndexOf(x => x.ID == Settings.Instance.Destination.StationID) : -1;
					DestStations.MoveCurrentToPosition(idx != -1 ? idx : 0);
					DestStations.Refresh();
					break;
				}
			}

			// When settings change, just update everything
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.Key));
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
