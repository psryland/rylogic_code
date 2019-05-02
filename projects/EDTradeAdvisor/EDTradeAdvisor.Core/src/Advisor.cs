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
		//      - Monitor for changes in the player's location (when EDMC is available)
		//      - Finds the best (ish) trades around the current player location

		static Advisor()
		{
			Log = new Logger("EDTA", new LogToFile(Util.ResolveUserDocumentsPath("Rylogic", "EDTradeAdvisor", "Logs", "log.txt"), append: false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Info, "<<< Started >>>");
		}
		public Advisor(Action<Action> run_on_main_thread)
		{
			m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
			m_marshal_to_main_thread = run_on_main_thread;
			RebuildStaticData = !Path_.FileExists(EDDatabase.Filepath);
			Shutdown = new CancellationTokenSource();
			TradeRoutes = new ObservableCollection<TradeRoute>();
			m_find_trade_routes = new AutoResetEvent(false);
			Map = new Map();
			Web = new Web(Shutdown.Token);
			DB = new EDDatabase(Shutdown.Token);
			Settings.Instance.SettingChange += HandleSettingChange;
		}
		public void Dispose()
		{
			Settings.Instance.SettingChange -= HandleSettingChange;
			Run = false;
			Web = null;
			DB = null;
			Util.Dispose(Log);
		}

		/// <summary>Execute a delegate on the main thread</summary>
		private void RunOnMainThread(Action action)
		{
			if (Thread.CurrentThread.ManagedThreadId == m_main_thread_id)
				action();
			else
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
				}
				m_thread_run = value ? new Thread(new ParameterizedThreadStart(RunThreadEntryPoint)) : null;
				if (m_thread_run != null)
				{
					m_settings_snapshot = new Settings(Settings.Instance);
					m_thread_stop = CancellationTokenSource.CreateLinkedTokenSource(Shutdown.Token);
					m_thread_run.Start(m_thread_stop.Token);
				}
				RunChanged?.Invoke(this, EventArgs.Empty);

				// Thread entry point
				async void RunThreadEntryPoint(object x)
				{
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
			// One-time update the data we don't expect to change
			await UpdateStaticData(RebuildStaticData);

			// Rebuild the star map
			if (RebuildStaticData || Map.BuildNeeded)
				Map.BuildSystemMap(DB.EnumStarSystems(buffered:false));

			RebuildStaticData = false;
			Log.Write(ELogLevel.Info, "Advisor Activated");

			// Start a loop
			for (; !thread_stop.IsCancellationRequested;)
			{
				if (m_trade_routes_issue_current == m_trade_routes_issue)
				{
					// If up to date, go to sleep
					using (thread_stop.Register(() => m_find_trade_routes.Set()))
						m_find_trade_routes.WaitOne();

					continue;
				}

				// Find trade routes
				await FindTradeRoutes(m_settings_snapshot);
			}

			Log.Write(ELogLevel.Info, "Advisor Deactivated");
		}
		private Settings m_settings_snapshot;

		/// <summary>Logging</summary>
		public static Logger Log { get; }

		/// <summary>Monitor output from the ED Market Connector app</summary>
		public bool UseEDMC
		{
			get { return m_monitor_edmc != null; }
			set
			{
				if (UseEDMC == value) return;
				if (m_monitor_edmc != null)
				{
					m_monitor_edmc.Changed -= HandleChanged;
					Util.Dispose(ref m_monitor_edmc);
				}
				m_monitor_edmc = value ? new FileSystemWatcher(Settings.Instance.EDMCDataPath) : null;
				if (m_monitor_edmc != null)
				{
					m_monitor_edmc.Changed += HandleChanged;
				}

				// Handler
				void HandleChanged(object sender, FileSystemEventArgs e)
				{
					switch (e.ChangeType)
					{
					default:
						break;
					case WatcherChangeTypes.All:
						break;
					case WatcherChangeTypes.Changed:
						break;
					case WatcherChangeTypes.Created:
						break;
					case WatcherChangeTypes.Deleted:
						break;
					case WatcherChangeTypes.Renamed:
						break;
					}
				}
			}
		}
		private FileSystemWatcher m_monitor_edmc;

		/// <summary>The nearby trade possibilities, sorted from best to worst</summary>
		public ObservableCollection<TradeRoute> TradeRoutes { get; }

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

		/// <summary>The database of ED data</summary>
		public EDDatabase DB
		{
			get { return m_db; }
			private set
			{
				if (m_db == value) return;
				Util.Dispose(ref m_db);
				m_db = value;
			}
		}
		private EDDatabase m_db;

		/// <summary>Star map</summary>
		private Map Map
		{
			get; set;
		}

		/// <summary>Ensure the database is up to date</summary>
		private async Task UpdateStaticData(bool force_rebuild)
		{
			using (StatusStack.NewStatusMessage($"Updating static data..."))
			{
				// Ensure the EDDB data has been downloaded and the database is up to date
				var updates = (await Task.WhenAll(
					Web.DownloadFile(SourceFiles.SystemsPopulated), //0
					Web.DownloadFile(SourceFiles.Stations),         //1
					Web.DownloadFile(SourceFiles.Commodities),      //2
					Web.DownloadFile(SourceFiles.Modules),          //3
					Web.DownloadFile(SourceFiles.Listings),         //4
					Web.DownloadFile(SourceFiles.LiveListings)      //5
					// Web.DownloadFile(SourceFiles.Systems),
					)).ToDictionary(x => x.FileUrl, x => (Filepath: x.OutputFilepath, Downloaded: x.Downloaded));

				// If the systems data changed, rebuild the system table and the map
				if (force_rebuild || updates[SourceFiles.SystemsPopulated].Downloaded)
				{
					Log.Write(ELogLevel.Info, $"Building systems table");
					DB.BuildSystemsTable(updates[SourceFiles.SystemsPopulated].Filepath);
					Map.Invalidate();
				}

				// If the stations data changed, rebuild the stations table
				if (force_rebuild || updates[SourceFiles.Stations].Downloaded)
				{
					Log.Write(ELogLevel.Info, $"Building stations table");
					DB.BuildStationsTable(updates[SourceFiles.Stations].Filepath);
				}

				// If the commodities data changed, rebuild the commodities tables
				if (force_rebuild || updates[SourceFiles.Commodities].Downloaded)
				{
					Log.Write(ELogLevel.Info, $"Building commodities tables");
					DB.BuildCommoditiesTable(updates[SourceFiles.Commodities].Filepath);
				}

				// If the listings data changed, rebuild the listings table
				if (force_rebuild || updates[SourceFiles.Listings].Downloaded)
				{
					Log.Write(ELogLevel.Info, $"Building listings table");
					DB.BuildListingsTable(updates[SourceFiles.Listings].Filepath);
				}
			}
		}

		/// <summary>Check settings are valid before trying to find trade routes</summary>
		private bool CanFindTradeRoutes(Settings settings) // worker thread context
		{
			return
				settings.OriginSystemID != null &&
				settings.OriginStationID != null &&
				(
					(settings.AnyDestination && settings.MaxTradeRouteDistance != null) ||
					(settings.DestSystemID != null)
				) &&
				settings.MaxJumpRange > 0 &&
				settings.CargoCapacity > 0 &&
				settings.AvailableCredits > 0;
		}

		/// <summary>Search for trades</summary>
		private async Task FindTradeRoutes(Settings settings) // worker thread context
		{
			var issue = m_trade_routes_issue;

			// Settings not valid, ignore until they are.
			if (!CanFindTradeRoutes(settings))
			{
				RunOnMainThread(() =>
				{
					if (issue != m_trade_routes_issue) return;
					m_trade_routes_issue_current = m_trade_routes_issue;
					TradeRoutes.Clear();
				});
				return;
			}

			// Get the start point
			var origin_system = DB.GetStarSystem(settings.OriginSystemID.Value);
			var origin_station = DB.GetStation(settings.OriginStationID.Value);
			var origin = new Location(origin_system, origin_station);
			Log.Write(ELogLevel.Debug, $"Find trade routes started (issue={issue})");

			// If a specific destination is given, find trades between the origin and that destination
			if (!settings.AnyDestination)
			{
				Log.Write(ELogLevel.Debug, $"Find trade routes to destination ID {settings.DestSystemID.Value}");
				var destination_system = DB.GetStarSystem(settings.DestSystemID.Value);
				var stations = settings.DestStationID != null
					? new[] { DB.GetStation(settings.DestStationID.Value) }
					: DB.EnumStations(
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
					RunOnMainThread(() =>
					{
						if (issue != m_trade_routes_issue) return;
						MergeTradeRoutes(origin, routes);
					});
				}
			}
			else
			{
				Log.Write(ELogLevel.Debug, $"Find trade routes in volume (centre={origin_system.Position}, radius={settings.MaxTradeRouteDistance})");
				int considered_systems = 0, last_considered_systems = 0;

				// A queue of systems to consider
				var queue = new Queue<StarSystem>();
				using (var msg = StatusStack.NewStatusMessage("Finding Trade Routes..."))
				{
					var candidates = Map.Search(origin_system.Position, settings.MaxTradeRouteDistance.Value).ToList();
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
						var system_refs = KDTree_.Search(candidates, 3, (double[])hop.Position, settings.MaxJumpRange).ToHashSet();
						if (system_refs.Count == 0)
							continue;

						// Remove candidates and update the map
						candidates.RemoveAll(system_refs);
						KDTree_.Build(candidates, 3);

						// Check the stations in each system
						foreach (var system_ref in system_refs)
						{
							// Abort if the issue number changes
							if (m_trade_routes_issue != issue)
								return;

							// Get the system
							var destination_system = DB.GetStarSystem(system_ref.ID);

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
							var stations = DB.EnumStations(
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
			var origin_market = await DB.GetMarketData(origin.Station.ID);

			// Remove listings with zero supply
			origin_market.Listings.RemoveIf(x => x.Supply == 0);

			// Get the market for 'destination_station'
			var market = await DB.GetMarketData(destination.Station.ID);

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
			Log.Write(ELogLevel.Debug, $"Found {routes.Count} trade routes to merge");

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
		}

		/// <summary>Call to trigger a search for trade routes</summary>
		public void InvalidateTradeRoutes()
		{
			Log.Write(ELogLevel.Debug, "Trade routes invalidated");
			++m_trade_routes_issue;
			m_find_trade_routes.Set();
		}

		/// <summary>Watch for settings changes</summary>
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			if (e.Before) return;

			// Make a copy of the settings for thread safety
			Log.Write(ELogLevel.Debug, $"Setting {e.Key} changed to {e.Value}");
			m_settings_snapshot = new Settings(Settings.Instance);
			InvalidateTradeRoutes();
		}
	}
}
