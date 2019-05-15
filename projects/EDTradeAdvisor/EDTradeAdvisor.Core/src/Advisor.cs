using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EDTradeAdvisor.DataProviders;
using EDTradeAdvisor.DomainObjects;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class Advisor : IDisposable
	{
		// Notes:
		//  - This is the main logic object for the ED Trade Advisor.
		//  - Generally speaking, it does the following:
		//      - Maintain the database of systems, stations, commodities, price data, etc
		//      - Monitor for changes in the player's location (using the ED journal files)
		//      - Finds the best (ish) trades around the current player location

		public Advisor(Action<Action> run_on_main_thread)
		{
			Settings.Instance.SettingChange += HandleSettingChange;
			m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
			m_marshal_to_main_thread = run_on_main_thread;
			RebuildStaticData = !Path_.FileExists(EliteDataProvider.Filepath);
			Shutdown = new CancellationTokenSource();
			TradeRoutes = new List<TradeRoute>();
			m_find_trade_routes = new AutoResetEvent(false);
			Web = new Web(Shutdown.Token);
			Src = new EliteDataProvider(Web, Shutdown.Token);
			JournalMonitor = new EDJournalMonitor(Shutdown.Token);

			RunOnMainThread(new Action(() =>
			{
				Settings.Instance.NotifySettingChanged(nameof(Settings.UseCurrentLocation));
				Settings.Instance.NotifySettingChanged(nameof(Settings.ReadCargoCapacityFromLoadout));
				Settings.Instance.NotifySettingChanged(nameof(Settings.ReadMaxJumpRangeFromLoadout));
			}));
		}
		public void Dispose()
		{
			JournalMonitor = null;
			Run = false;
			Src = null;
			Web = null;
			Log.Dispose();
			Settings.Instance.SettingChange -= HandleSettingChange;
		}

		/// <summary>Execute a delegate on the main thread</summary>
		private void RunOnMainThread(Action action)
		{
			m_marshal_to_main_thread(action);
		}
		private Action<Action> m_marshal_to_main_thread;
		private int m_main_thread_id;

		/// <summary>App shutdown token</summary>
		public CancellationTokenSource Shutdown { get; }

		/// <summary>Set to true before Running the advisor to rebuild the cached data</summary>
		public bool RebuildStaticData
		{
			private get { return m_rebuild_static_data; }
			set
			{
				if (m_rebuild_static_data == value) return;
				m_rebuild_static_data = value;

				// If already running, restart
				if (RebuildStaticData && Run)
				{
					Run = false;
					Run = true;
				}
			}
		}
		private bool m_rebuild_static_data;

		/// <summary>Start/Stop the advisor</summary>
		public bool Run
		{
			get { return m_thread_run != null; }
			set
			{
				if (Run == value) return;
				if (m_thread_run != null)
				{
					m_thread_stop.Cancel();
					m_thread_run.Join();
					Util.Dispose(ref m_thread_stop);
					StatusStack.DefaultStatusMessage = "Advisor Inactive";
				}
				m_thread_run = value ? new Thread(new ParameterizedThreadStart(RunThreadEntryPoint)) : null;
				if (m_thread_run != null)
				{
					StatusStack.DefaultStatusMessage = "Idle";
					m_settings_snapshot = new Settings(Settings.Instance);
					m_thread_stop = CancellationTokenSource.CreateLinkedTokenSource(Shutdown.Token);
					m_thread_run.Start(m_thread_stop.Token);
				}
				RunChanged?.Invoke(this, EventArgs.Empty);

				// Thread entry point
				async void RunThreadEntryPoint(object x)
				{
					Thread.CurrentThread.Name = "Trade Advisor Thread";
					try { await AdvisorMain((CancellationToken)x); }
					catch (OperationCanceledException) { }
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, $"Advisor main thread has exited unexpectedly: {ex.Message}");
					}
					RunOnMainThread(() => Run = false);
				}
			}
		}
		private Thread m_thread_run;
		private CancellationTokenSource m_thread_stop;
		public event EventHandler RunChanged;

		/// <summary>Advisor main loop</summary>
		private async Task AdvisorMain(CancellationToken thread_stop)
		{
			Log.Write(ELogLevel.Info, "Advisor Activated");

			// One-time update the data we don't expect to change
			await Src.BuildCache(RebuildStaticData);
			RebuildStaticData = false;

			// Start a loop
			var issue = 0;
			for (; !thread_stop.IsCancellationRequested; )
			{
				// If a new search is needed and not yet started, search now
				if (m_trade_routes_issue_current != m_trade_routes_issue && issue != m_trade_routes_issue)
				{
					// Find trade routes
					issue = m_trade_routes_issue;
					await FindTradeRoutes(m_settings_snapshot, issue);
					continue;
				}

				// If up to date, go to sleep
				using (thread_stop.Register(() => m_find_trade_routes.Set()))
					m_find_trade_routes.WaitOne(TimeSpan.FromSeconds(0.5));
			}

			Log.Write(ELogLevel.Info, "Advisor Deactivated");
		}
		private Settings m_settings_snapshot;

		/// <summary>Monitor the ED commander journals</summary>
		public EDJournalMonitor JournalMonitor
		{
			get { return m_journal_monitor; }
			private set
			{
				if (m_journal_monitor == value) return;
				if (m_journal_monitor != null)
				{
					m_journal_monitor.MarketUpdate -= HandleMarketUpdate;
					m_journal_monitor.MaxJumpRangeChanged -= HandleMaxJumpRangeChanged;
					m_journal_monitor.CargoCapacityChanged -= HandleCargoSpaceChanged;
					m_journal_monitor.LocationChanged -= HandleLocationChanged;
					Util.Dispose(ref m_journal_monitor);
				}
				m_journal_monitor = value;
				if (m_journal_monitor != null)
				{
					m_journal_monitor.Run = Path_.DirExists(Settings.Instance.JournalFilesDir);
					m_journal_monitor.LocationChanged += HandleLocationChanged;
					m_journal_monitor.CargoCapacityChanged += HandleCargoSpaceChanged;
					m_journal_monitor.MaxJumpRangeChanged += HandleMaxJumpRangeChanged;
					m_journal_monitor.MarketUpdate += HandleMarketUpdate;
				}

				// Handlers
				void HandleLocationChanged(object sender, EventArgs e)
				{
					Settings.Instance.NotifySettingChanged(nameof(Settings.UseCurrentLocation));
				}
				void HandleCargoSpaceChanged(object sender, EventArgs e)
				{
					Settings.Instance.NotifySettingChanged(nameof(Settings.ReadCargoCapacityFromLoadout));
				}
				void HandleMaxJumpRangeChanged(object sender, EventArgs e)
				{
					Settings.Instance.NotifySettingChanged(nameof(Settings.ReadMaxJumpRangeFromLoadout));
				}
				async void HandleMarketUpdate(object sender, MarketUpdateEventArgs e)
				{
					if (Settings.Instance.UpdateMarketDataFromJournal)
						await Src.MergeMarketUpdate(e.MarketDataFilepath);
				}
			}
		}
		private EDJournalMonitor m_journal_monitor;

		/// <summary>The nearby trade possibilities, sorted from best to worst</summary>
		public List<TradeRoute> TradeRoutes { get; }
		public event EventHandler TradeRoutesChanged;

		/// <summary>Helper for accessing data from web sources</summary>
		private Web Web
		{
			get { return m_web; }
			set
			{
				if (m_web == value) return;
				Util.Dispose(ref m_web);
				m_web = value;
			}
		}
		private Web m_web;

		/// <summary>The source of systems, stations, and market data</summary>
		public IEliteDataProvider Src
		{
			get { return m_src; }
			private set
			{
				if (m_src == value) return;
				Util.Dispose(ref m_src);
				m_src = value;
			}
		}
		private IEliteDataProvider m_src;

		/// <summary>Check settings are valid before trying to find trade routes</summary>
		private bool CanFindTradeRoutes(Settings settings) // worker thread context
		{
			return
				settings.Origin.StarSystemID != null &&
				settings.Origin.StationID != null &&
				(
					(settings.AnyDestination && settings.MaxTradeRouteDistance != null) ||
					(settings.Destination.StarSystemID != null)
				) &&
				settings.MaxJumpRange > 0 &&
				settings.CargoCapacity > 0 &&
				settings.AvailableCredits > 0;
		}

		/// <summary>Search for trades</summary>
		private async Task FindTradeRoutes(Settings settings, int issue) // worker thread context
		{
			if (!CanFindTradeRoutes(settings))
			{
				// Settings not valid, ignore until they are.
				RunOnMainThread(() =>
				{
					if (issue != m_trade_routes_issue) return;
					m_trade_routes_issue_current = issue;
				});
				return;
			}
			else
			{
				// Settings are valid, reset the trade routes before starting
				RunOnMainThread(() =>
				{
					if (issue != m_trade_routes_issue || TradeRoutes.Count == 0)
						return;

					TradeRoutes.Clear();
					TradeRoutesChanged?.Invoke(this, EventArgs.Empty);
				});
			}

			// Get the start point
			var origin_system = await Src.GetStarSystem(settings.Origin.StarSystemID.Value);
			var origin_station = await Src.GetStation(settings.Origin.StationID.Value);
			var origin = new Location(origin_system, origin_station);

			// If a specific destination is given, find trades between the origin and that destination
			if (!settings.AnyDestination)
			{
				var destination_system = await Src.GetStarSystem(settings.Destination.StarSystemID.Value);
				var destination_station = settings.Destination.StationID != null ? await Src.GetStation(settings.Destination.StationID.Value) : null;

				Log.Write(ELogLevel.Info, $"Find trade routes {origin_system.Name}/{origin_station.Name} → {destination_system.Name}/{destination_station?.Name ?? "Any"}");

				var stations = destination_station != null
					? new[] { destination_station }
					: Src.EnumStations(
						system_id: destination_system.ID,
						max_station_distance: settings.MaxStationDistance,
						required_pad_size: settings.RequiredPadSize,
						facilities_incl: EFacilities.Market | EFacilities.Docking,
						ignore_planetary: settings.IgnorePlanetBases);

				// Look for trade routes between the given stations
				foreach (var station in stations)
				{
					// Abort if the issue number changes
					if (m_trade_routes_issue != issue)
						return;

					var routes = await FindTradeRoutes(settings, origin, new Location(destination_system, station), issue);
					if (routes.Count == 0)
						continue;

					RunOnMainThread(() =>
					{
						if (issue != m_trade_routes_issue) return;
						MergeTradeRoutes(origin, routes);
					});
				}
			}
			else
			{
				Log.Write(ELogLevel.Info, $"Find trade routes {origin_system.Name}/{origin_station.Name} within {settings.MaxTradeRouteDistance} LY");

				int considered_systems = 0, last_considered_systems = 0;
				var considered = new HashSet<StarSystemRef>();

				// A queue of systems to consider
				var queue = new Queue<StarSystem>();
				using (var msg = StatusStack.NewStatusMessage("Finding Trade Routes..."))
				{
					var candidates = Src.Search(origin_system.Position, settings.MaxTradeRouteDistance.Value).ToList();
					KDTree_.Build(candidates, 3);

					// Find all systems within the maximum trade route distance.
					for (queue.Enqueue(origin_system); queue.Count != 0 && !Shutdown.IsCancellationRequested;)
					{
						// Abort if the issue number changes
						if (m_trade_routes_issue != issue)
							return;

						// Get the next system to consider
						var hop = queue.Dequeue();

						// Find the systems within the maximum jump range that have not been considered already
						var system_refs = KDTree_.Search(candidates, 3, (double[])hop.Position, settings.MaxJumpRange).Where(x => !considered.Contains(x)).ToList();
						if (system_refs.Count == 0)
							continue;

						// When the considered number gets large enough, remove them from the map and regenerate it
						const int RebuildTreeThreshold = 256;
						considered.AddRange(system_refs);
						if (considered.Count > RebuildTreeThreshold)
						{
							candidates.RemoveAll(considered);
							KDTree_.Build(candidates, 3);
							considered.Clear();
						}

						// Check the stations in each system
						foreach (var system_ref in system_refs)
						{
							// Abort if the issue number changes
							if (m_trade_routes_issue != issue)
								return;

							// Get the system
							var destination_system = await Src.GetStarSystem(system_ref.ID);

							// Update status
							if (++considered_systems - last_considered_systems > 100)
							{
								var distance = (destination_system.Position - origin_system.Position).Length;
								msg.Message = $"Finding Trade Routes... (checked:{considered_systems} queued:{queue.Count} distance:{distance})";
								last_considered_systems = considered_systems;
							}

							// Needs a permit?
							if (destination_system.NeedPermit && settings.IgnorePermitSystems)
								continue;

							// Check the suitable stations
							var stations = Src.EnumStations(
								system_id:destination_system.ID,
								max_station_distance: settings.MaxStationDistance,
								required_pad_size: settings.RequiredPadSize,
								facilities_incl: EFacilities.Market | EFacilities.Docking,
								ignore_planetary: settings.IgnorePlanetBases);
							foreach (var station in stations)
							{
								// Abort if the issue number changes
								if (m_trade_routes_issue != issue)
									return;

								var routes = await FindTradeRoutes(settings, origin, new Location(destination_system, station), issue);
								if (routes.Count == 0)
									continue;

								RunOnMainThread(() =>
								{
									if (issue != m_trade_routes_issue) return;
									MergeTradeRoutes(origin, routes);
								});
							}

							// Add this system to be considered for the next hop
							queue.Enqueue(destination_system);
						}
					}
				}
			}
			m_trade_routes_issue_current = issue;
		}
		private AutoResetEvent m_find_trade_routes;
		private int m_trade_routes_issue_current;
		private int m_trade_routes_issue;

		/// <summary>Find the profitable trades between two stations and return a trade route for each one</summary>
		private async Task<IList<TradeRoute>> FindTradeRoutes(Settings settings, Location origin, Location destination, int issue) // worker thread context
		{
			var trade_routes = new List<TradeRoute>();

			// Get the market data for the origin station
			var origin_market = await Src.GetMarketData(origin.Station.ID);

			// Remove listings with zero supply
			origin_market.Listings.RemoveIf(x => x.Supply == 0);

			// Get the market for 'destination_station'
			var market = await Src.GetMarketData(destination.Station.ID);

			// For each commodity available in 'origin_market' see if
			// it's profitable to buy and then sell at 'market'.
			foreach (var origin_listing in origin_market.Listings)
			{
				// Find the same commodity on 'market', skip if not profitable
				var dest_listing = market.Listings.FirstOrDefault(x => x.CommodityID == origin_listing.CommodityID);
				if (dest_listing == null || dest_listing.SellPrice <= origin_listing.BuyPrice)
					continue;

				// The amount to buy
				var amount = Math.Min(settings.CargoCapacity, origin_listing.Supply);
				if (amount * origin_listing.BuyPrice > settings.AvailableCredits)
					amount = settings.AvailableCredits / origin_listing.BuyPrice;
				if (amount == 0)
					continue;

				// Create a trade route for this commodity
				trade_routes.Add(new TradeRoute(origin, destination, origin_listing.CommodityName,
					origin_listing.CommodityID, origin_listing.BuyPrice, dest_listing.SellPrice, amount));
			}

			return trade_routes;
		}

		/// <summary>Merge 'routes' into 'TradeRoutes' in order from most to least profitable</summary>
		private void MergeTradeRoutes(Location origin, IList<TradeRoute> routes)
		{
			// Remove any routes that don't start at 'origin_station'
			TradeRoutes.RemoveIf(x => x.Origin.Station.ID != origin.Station.ID);

			// Merge 'routes' into 'TradeRoutes' in order
			foreach (var route in routes)
			{
				// Remove duplicates
				TradeRoutes.RemoveIf(x =>
					x.Destination.System.ID == route.Destination.System.ID &&
					x.Destination.Station.ID == route.Destination.Station.ID &&
					x.CommodityID == route.CommodityID);

				// Merge routes
				var idx = TradeRoutes.BinarySearch(r => route.Profit.CompareTo(r.Profit), find_insert_position: true);
				if (idx < Settings.Instance.RouteCount)
					TradeRoutes.Insert(idx, route);
				if (TradeRoutes.Count > Settings.Instance.RouteCount)
					TradeRoutes.RemoveToEnd(Settings.Instance.RouteCount);
			}

			// Notify trade routes updated
			TradeRoutesChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Call to trigger a search for trade routes</summary>
		public void InvalidateTradeRoutes()
		{
			Log.Write(ELogLevel.Debug, "Trade routes invalidated");
			++m_trade_routes_issue;
			m_find_trade_routes.Set();
		}

		/// <summary>Set the current location from the journal data</summary>
		private async Task SetLocationFromJournal()
		{
			// Lookup the star system id
			var system = JournalMonitor.Location.StarSystem != null
				? await Src.GetStarSystem(JournalMonitor.Location.StarSystem)
				: null;

			// Lookup the station id
			var station = JournalMonitor.Location.Station != null && system != null
				? await Src.GetStation(system.ID, JournalMonitor.Location.Station)
				: null;

			Settings.Instance.Origin = new LocationID(system?.ID, station?.ID);
			InvalidateTradeRoutes();
		}

		/// <summary>Watch for settings changes</summary>
		private async void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			if (e.Before) return;
			Log.Write(ELogLevel.Debug, $"Setting {e.Key} changed to {e.Value}");

			switch (e.Key)
			{
			case nameof(Settings.JournalFilesDir):
				{
					// Enable the journal file monitor if the ED journal path is set
					m_journal_monitor.Run = Path_.DirExists(Settings.Instance.JournalFilesDir);
					break;
				}
			case nameof(Settings.Origin):
				{
					// If a origin station is selected but the current system id is
					// still null, set it to the system that owns the station.
					if (Settings.Instance.Origin.StationID != null &&
						Settings.Instance.Origin.StarSystemID == null)
					{
						var station = Src.GetStation(Settings.Instance.Origin.StationID.Value).Result;
						Settings.Instance.Origin = new LocationID(station.SystemID, station.ID);
					}
					break;
				}
			case nameof(Settings.Destination):
				{
					// If a destination station is selected but the current system id is
					// still null, set it to the system that owns the station.
					if (Settings.Instance.Destination.StationID != null &&
						Settings.Instance.Destination.StarSystemID == null)
					{
						var station = Src.GetStation(Settings.Instance.Destination.StationID.Value).Result;
						Settings.Instance.Destination = new LocationID(station.SystemID, station.ID);
					}
					break;
				}
			case nameof(Settings.UseCurrentLocation):
				{
					if (Settings.Instance.UseCurrentLocation)
						await SetLocationFromJournal();
					break;
				}
			case nameof(Settings.ReadCargoCapacityFromLoadout):
				{
					if (Settings.Instance.ReadCargoCapacityFromLoadout && JournalMonitor.CargoCapacity != null)
						Settings.Instance.CargoCapacity = JournalMonitor.CargoCapacity.Value;
					break;
				}
			case nameof(Settings.ReadMaxJumpRangeFromLoadout):
				{
					if (Settings.Instance.ReadMaxJumpRangeFromLoadout && JournalMonitor.MaxJumpRange != null)
						Settings.Instance.MaxJumpRange = JournalMonitor.MaxJumpRange.Value;
					break;
				}
			}

			// Make a copy of the settings for thread safety
			m_settings_snapshot = new Settings(Settings.Instance);
			InvalidateTradeRoutes();
		}
	}
}
